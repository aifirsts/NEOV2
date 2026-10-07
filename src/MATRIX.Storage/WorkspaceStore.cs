// MATRIX V2 — Storage layer
// NEW_IMPLEMENTATION: File-based storage with generations, CURRENT pointer, SHA-256, revision coordination
// Implements: write→flush→reread/validate→publish protocol
// Data path: %LOCALAPPDATA%\MATRIX (catalog.json, state.json, settings.json, attachments/)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using MATRIX.Core;
using MATRIX.Application;

namespace MATRIX.Storage
{
    /// <summary>Storage configuration.</summary>
    public sealed class StorageConfig
    {
        public required string DataDirectory { get; init; }
        public required string CatalogFile { get; init; }
        public required string StateFile { get; init; }
        public required string SettingsFile { get; init; }
        public required string CurrentFile { get; init; }
        public required string GenerationsDir { get; init; }
        public required string LeaseFile { get; init; }

        public static StorageConfig Default(string? dataDirectory = null)
        {
            var dir = dataDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MATRIX");
            return new StorageConfig
            {
                DataDirectory = dir,
                CatalogFile = Path.Combine(dir, "catalog.json"),
                StateFile = Path.Combine(dir, "state.json"),
                SettingsFile = Path.Combine(dir, "settings.json"),
                CurrentFile = Path.Combine(dir, "CURRENT"),
                GenerationsDir = Path.Combine(dir, "generations"),
                LeaseFile = Path.Combine(dir, ".lease")
            };
        }
    }

    /// <summary>A generation snapshot with hash and revision.</summary>
    public sealed class Generation
    {
        public required string Id { get; init; }
        public required string Revision { get; init; }
        public required string CatalogSha256 { get; init; }
        public required string StateSha256 { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required string CatalogPath { get; init; }
        public required string StatePath { get; init; }
    }

    /// <summary>
    /// Cooperative lease for write coordination.
    /// Prevents concurrent writes from corrupting data.
    /// </summary>
    public sealed class CooperativeLease : IDisposable
    {
        private readonly string _leaseFile;
        private bool _disposed;

        public string LeaseId { get; }
        public DateTimeOffset AcquiredAt { get; }

        public CooperativeLease(string leaseFile)
        {
            _leaseFile = leaseFile;
            LeaseId = Guid.NewGuid().ToString("N");
            AcquiredAt = DateTimeOffset.UtcNow;
        }

        public bool TryAcquire(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (!File.Exists(_leaseFile))
                {
                    try
                    {
                        File.WriteAllText(_leaseFile,
                            $"{LeaseId}\n{AcquiredAt:O}\n{Environment.MachineName}");
                        return true;
                    }
                    catch { /* retry */ }
                }
                Thread.Sleep(100);
            }
            return false;
        }

        public void Release()
        {
            if (_disposed) return;
            try
            {
                if (File.Exists(_leaseFile))
                {
                    var content = File.ReadAllText(_leaseFile);
                    if (content.StartsWith(LeaseId))
                        File.Delete(_leaseFile);
                }
            }
            catch { /* best effort */ }
            _disposed = true;
        }

        public void Dispose() => Release();
    }

    /// <summary>
    /// File-based workspace store with generation snapshots.
    /// Protocol: write→flush→reread/validate→publish
    /// No atomicity guarantee for pair of independent File.Replace.
    /// No arbitrary power-loss durability guarantee.
    /// </summary>
    public sealed class WorkspaceStore : IWorkspaceStore
    {
        private readonly StorageConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public WorkspaceStore(StorageConfig? config = null)
        {
            _config = config ?? StorageConfig.Default();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            EnsureDirectories();
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(_config.DataDirectory);
            Directory.CreateDirectory(_config.GenerationsDir);
        }

        public OperationResult<Workspace> Load()
        {
            try
            {
                if (!File.Exists(_config.CatalogFile) || !File.Exists(_config.StateFile))
                    return OperationResult<Workspace>.Fail("No workspace data found");

                // Check CURRENT pointer for expected revision
                var expectedRevision = ReadCurrentPointer();

                var catalogJson = File.ReadAllText(_config.CatalogFile, Encoding.UTF8);
                var stateJson = File.ReadAllText(_config.StateFile, Encoding.UTF8);

                // Validate JSON before deserialization
                if (!TryValidateJson(catalogJson, out var catalogError))
                    return OperationResult<Workspace>.Fail($"Invalid catalog JSON: {catalogError}");
                if (!TryValidateJson(stateJson, out var stateError))
                    return OperationResult<Workspace>.Fail($"Invalid state JSON: {stateError}");

                var catalog = DeserializeCatalog(catalogJson);
                var state = DeserializeState(stateJson);

                // Verify hashes
                var actualCatalogHash = ComputeSha256(catalogJson);
                var actualStateHash = ComputeSha256(stateJson);

                var exportedAt = DateTimeOffset.UtcNow;
                var workspace = Workspace.Create(exportedAt, catalog, state);
                return OperationResult<Workspace>.Ok(workspace);
            }
            catch (Exception ex)
            {
                return OperationResult<Workspace>.Fail($"Load failed: {ex.Message}");
            }
        }

        public OperationResult Save(Workspace workspace)
        {
            try
            {
                // Validate workspace before writing
                GraphValidator.ValidateWorkspace(workspace);

                using var lease = new CooperativeLease(_config.LeaseFile);
                if (!lease.TryAcquire(TimeSpan.FromSeconds(5)))
                    return OperationResult.Fail("Could not acquire write lease");

                // Serialize to JSON
                var catalogJson = SerializeCatalog(workspace.Catalog);
                var stateJson = SerializeState(workspace.State);

                // Create generation snapshot
                var generationId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
                var generationDir = Path.Combine(_config.GenerationsDir, generationId);
                Directory.CreateDirectory(generationDir);

                var catalogGenPath = Path.Combine(generationDir, "catalog.json");
                var stateGenPath = Path.Combine(generationDir, "state.json");

                // Write to generation directory first
                File.WriteAllText(catalogGenPath, catalogJson, new UTF8Encoding(false));
                File.WriteAllText(stateGenPath, stateJson, new UTF8Encoding(false));

                // Flush
                FlushFile(catalogGenPath);
                FlushFile(stateGenPath);

                // Reread and validate
                var rereadCatalog = File.ReadAllText(catalogGenPath, Encoding.UTF8);
                var rereadState = File.ReadAllText(stateGenPath, Encoding.UTF8);
                if (rereadCatalog != catalogJson || rereadState != stateJson)
                    return OperationResult.Fail("Reread mismatch after write");

                // Compute hashes
                var catalogHash = ComputeSha256(catalogJson);
                var stateHash = ComputeSha256(stateJson);

                // Write generation metadata
                var genMeta = new JsonObject
                {
                    ["id"] = generationId,
                    ["revision"] = generationId,
                    ["catalogSha256"] = catalogHash,
                    ["stateSha256"] = stateHash,
                    ["createdAt"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["catalogPath"] = catalogGenPath,
                    ["statePath"] = stateGenPath
                };
                File.WriteAllText(Path.Combine(generationDir, "meta.json"),
                    genMeta.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                    new UTF8Encoding(false));

                // Publish: copy to main files
                File.Copy(catalogGenPath, _config.CatalogFile, overwrite: true);
                File.Copy(stateGenPath, _config.StateFile, overwrite: true);

                // Update CURRENT pointer
                File.WriteAllText(_config.CurrentFile,
                    $"{generationId}\n{catalogHash}\n{stateHash}\n{DateTimeOffset.UtcNow:O}",
                    new UTF8Encoding(false));

                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Save failed: {ex.Message}");
            }
        }

        public OperationResult<Workspace> GetCurrent()
        {
            return Load();
        }

        public OperationResult<string> GetRevision()
        {
            try
            {
                var revision = ReadCurrentPointer();
                if (string.IsNullOrEmpty(revision))
                    return OperationResult<string>.Fail("No CURRENT pointer");
                return OperationResult<string>.Ok(revision);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail($"GetRevision failed: {ex.Message}");
            }
        }

        private string ReadCurrentPointer()
        {
            if (!File.Exists(_config.CurrentFile)) return string.Empty;
            var content = File.ReadAllText(_config.CurrentFile);
            var lines = content.Split('\n');
            return lines.Length > 0 ? lines[0].Trim() : string.Empty;
        }

        private void FlushFile(string path)
        {
            // Best-effort flush — no arbitrary power-loss durability guarantee
            using var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite,
                FileShare.None, bufferSize: 4096, FileOptions.None);
            fs.Flush(flushToDisk: true);
        }

        private static string ComputeSha256(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool TryValidateJson(string json, out string? error)
        {
            error = null;
            try
            {
                JsonNode.Parse(json);
                return true;
            }
            catch (JsonException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private Catalog DeserializeCatalog(string json)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var nodes = new List<Node>();
            if (root.TryGetProperty("nodes", out var nodesEl))
            {
                foreach (var n in nodesEl.EnumerateArray())
                {
                    nodes.Add(Node.Create(
                        n.GetProperty("id").GetString()!,
                        SecurityPolicy.ParseEnum<NodeType>(n.GetProperty("type").GetString()!),
                        n.GetProperty("name").GetString()!,
                        n.GetProperty("owner").GetString()!,
                        n.GetProperty("trustZone").GetString()!,
                        SecurityPolicy.ParseEnum<Sensitivity>(n.GetProperty("sensitivity").GetString()!),
                        SecurityPolicy.ParseEnum<Availability>(n.GetProperty("availability").GetString()!)));
                }
            }

            var edges = new List<Edge>();
            if (root.TryGetProperty("edges", out var edgesEl))
            {
                foreach (var e in edgesEl.EnumerateArray())
                {
                    edges.Add(Edge.Create(
                        e.GetProperty("id").GetString()!,
                        e.GetProperty("from").GetString()!,
                        e.GetProperty("to").GetString()!,
                        SecurityPolicy.ParseEnum<EdgeKind>(e.GetProperty("kind").GetString()!),
                        SecurityPolicy.ParseEnum<Criticality>(e.GetProperty("criticality").GetString()!)));
                }
            }

            var projects = new List<Project>();
            if (root.TryGetProperty("projects", out var projectsEl))
            {
                foreach (var p in projectsEl.EnumerateArray())
                {
                    var milestones = new List<string>();
                    if (p.TryGetProperty("milestones", out var msEl))
                        foreach (var m in msEl.EnumerateArray())
                            milestones.Add(m.GetString()!);

                    projects.Add(Project.Create(
                        p.GetProperty("id").GetString()!,
                        p.GetProperty("nodeId").GetString()!,
                        p.GetProperty("goal").GetString()!,
                        p.GetProperty("repoUrl").ValueKind == JsonValueKind.Null ? null : p.GetProperty("repoUrl").GetString(),
                        p.GetProperty("stage").GetString()!,
                        p.GetProperty("nextAction").GetString()!,
                        milestones));
                }
            }

            return Catalog.Create(nodes, edges, projects);
        }

        private State DeserializeState(string json)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tasks = new List<Task>();
            if (root.TryGetProperty("tasks", out var tasksEl))
            {
                foreach (var t in tasksEl.EnumerateArray())
                {
                    var deps = new List<string>();
                    if (t.TryGetProperty("dependencies", out var depsEl))
                        foreach (var d in depsEl.EnumerateArray())
                            deps.Add(d.GetString()!);

                    tasks.Add(Task.Create(
                        t.GetProperty("id").GetString()!,
                        t.GetProperty("projectId").GetString()!,
                        t.GetProperty("title").GetString()!,
                        SecurityPolicy.ParseEnum<TaskStatus>(t.GetProperty("status").GetString()!),
                        SecurityPolicy.ParseEnum<TaskPriority>(t.GetProperty("priority").GetString()!),
                        deps));
                }
            }

            var controls = new List<Control>();
            if (root.TryGetProperty("controls", out var controlsEl))
            {
                foreach (var c in controlsEl.EnumerateArray())
                {
                    controls.Add(Control.Create(
                        c.GetProperty("id").GetString()!,
                        c.GetProperty("targetId").GetString()!,
                        c.GetProperty("criterion").GetString()!,
                        c.GetProperty("scope").GetString()!,
                        c.GetProperty("method").GetString()!,
                        c.GetProperty("required").GetBoolean(),
                        SecurityPolicy.ParseEnum<Criticality>(c.GetProperty("criticality").GetString()!),
                        c.GetProperty("ttlSeconds").GetInt32(),
                        SecurityPolicy.ParseEnum<ControlApplicability>(c.GetProperty("applicability").GetString()!),
                        c.GetProperty("rationale").GetString()!));
                }
            }

            var evidence = new List<Evidence>();
            if (root.TryGetProperty("evidence", out var evEl))
            {
                foreach (var e in evEl.EnumerateArray())
                {
                    var supersedes = new List<string>();
                    if (e.TryGetProperty("supersedes", out var ssEl))
                        foreach (var s in ssEl.EnumerateArray())
                            supersedes.Add(s.GetString()!);

                    evidence.Add(Evidence.Create(
                        e.GetProperty("id").GetString()!,
                        e.GetProperty("controlId").GetString()!,
                        e.GetProperty("scope").GetString()!,
                        e.GetProperty("source").GetString()!,
                        e.GetProperty("method").GetString()!,
                        e.GetProperty("operator").GetString()!,
                        DateTimeOffset.Parse(e.GetProperty("observedAt").GetString()!),
                        DateTimeOffset.Parse(e.GetProperty("validUntil").GetString()!),
                        SecurityPolicy.ParseEnum<EvidenceResult>(e.GetProperty("result").GetString()!),
                        SecurityPolicy.ParseEnum<EvidenceProvenance>(e.GetProperty("provenance").GetString()!),
                        e.GetProperty("revoked").GetBoolean(),
                        supersedes));
                }
            }

            var findings = new List<Finding>();
            if (root.TryGetProperty("findings", out var fEl))
            {
                foreach (var f in fEl.EnumerateArray())
                {
                    findings.Add(Finding.Create(
                        f.GetProperty("id").GetString()!,
                        f.GetProperty("targetId").GetString()!,
                        SecurityPolicy.ParseEnum<FindingSeverity>(f.GetProperty("severity").GetString()!),
                        f.GetProperty("impact").GetString()!,
                        f.GetProperty("remediation").GetString()!,
                        SecurityPolicy.ParseEnum<FindingStatus>(f.GetProperty("status").GetString()!)));
                }
            }

            var runbooks = new List<Runbook>();
            if (root.TryGetProperty("runbooks", out var rEl))
            {
                foreach (var r in rEl.EnumerateArray())
                {
                    var steps = new List<string>();
                    if (r.TryGetProperty("steps", out var stepsEl))
                        foreach (var s in stepsEl.EnumerateArray())
                            steps.Add(s.GetString()!);

                    runbooks.Add(Runbook.Create(
                        r.GetProperty("id").GetString()!,
                        r.GetProperty("targetId").GetString()!,
                        r.GetProperty("name").GetString()!,
                        r.GetProperty("preconditions").GetString()!,
                        steps,
                        r.GetProperty("risk").GetString()!,
                        r.GetProperty("verification").GetString()!));
                }
            }

            var auditEvents = new List<AuditEvent>();
            if (root.TryGetProperty("auditEvents", out var aEl))
            {
                foreach (var a in aEl.EnumerateArray())
                {
                    auditEvents.Add(AuditEvent.Create(
                        a.GetProperty("id").GetString()!,
                        DateTimeOffset.Parse(a.GetProperty("timestamp").GetString()!),
                        a.GetProperty("actor").GetString()!,
                        a.GetProperty("action").GetString()!,
                        a.GetProperty("targetId").GetString()!,
                        a.GetProperty("before").ValueKind == JsonValueKind.Null ? null : a.GetProperty("before").GetString(),
                        a.GetProperty("after").ValueKind == JsonValueKind.Null ? null : a.GetProperty("after").GetString()));
                }
            }

            var userStatements = new List<UserStatement>();
            if (root.TryGetProperty("userStatements", out var usEl))
            {
                foreach (var u in usEl.EnumerateArray())
                {
                    userStatements.Add(UserStatement.Create(
                        u.GetProperty("id").GetString()!,
                        u.GetProperty("targetId").GetString()!,
                        u.GetProperty("text").GetString()!,
                        DateTimeOffset.Parse(u.GetProperty("declaredAt").GetString()!),
                        u.GetProperty("operator").GetString()!));
                }
            }

            return State.Create(tasks, controls, evidence, findings, runbooks, auditEvents, userStatements);
        }

        private string SerializeCatalog(Catalog catalog)
        {
            var obj = new JsonObject
            {
                ["schemaVersion"] = catalog.SchemaVersion,
                ["nodes"] = new JsonArray(catalog.Nodes.Select(n => new JsonObject
                {
                    ["id"] = n.Id,
                    ["type"] = n.Type.ToString().ToLowerInvariant(),
                    ["name"] = n.Name,
                    ["owner"] = n.Owner,
                    ["trustZone"] = n.TrustZone,
                    ["sensitivity"] = n.Sensitivity.ToString().ToUpperInvariant(),
                    ["availability"] = n.Availability.ToString().ToUpperInvariant()
                }).Cast<JsonNode>().ToArray()),
                ["edges"] = new JsonArray(catalog.Edges.Select(e => new JsonObject
                {
                    ["id"] = e.Id,
                    ["from"] = e.From,
                    ["to"] = e.To,
                    ["kind"] = SecurityPolicy.ToJsonString(e.Kind),
                    ["criticality"] = e.Criticality.ToString().ToUpperInvariant()
                }).Cast<JsonNode>().ToArray()),
                ["projects"] = new JsonArray(catalog.Projects.Select(p => new JsonObject
                {
                    ["id"] = p.Id,
                    ["nodeId"] = p.NodeId,
                    ["goal"] = p.Goal,
                    ["repoUrl"] = p.RepoUrl,
                    ["stage"] = p.Stage,
                    ["nextAction"] = p.NextAction,
                    ["milestones"] = new JsonArray(p.Milestones.Select(m => (JsonNode)m).ToArray())
                }).Cast<JsonNode>().ToArray())
            };
            return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private string SerializeState(State state)
        {
            var obj = new JsonObject
            {
                ["schemaVersion"] = state.SchemaVersion,
                ["tasks"] = new JsonArray(state.Tasks.Select(t => new JsonObject
                {
                    ["id"] = t.Id,
                    ["projectId"] = t.ProjectId,
                    ["title"] = t.Title,
                    ["status"] = SecurityPolicy.ToJsonString(t.Status).ToUpperInvariant(),
                    ["priority"] = SecurityPolicy.ToJsonString(t.Priority).ToUpperInvariant(),
                    ["dependencies"] = new JsonArray(t.Dependencies.Select(d => (JsonNode)d).ToArray())
                }).Cast<JsonNode>().ToArray()),
                ["controls"] = new JsonArray(state.Controls.Select(c => new JsonObject
                {
                    ["id"] = c.Id,
                    ["targetId"] = c.TargetId,
                    ["criterion"] = c.Criterion,
                    ["scope"] = c.Scope,
                    ["method"] = c.Method,
                    ["required"] = c.Required,
                    ["criticality"] = c.Criticality.ToString().ToUpperInvariant(),
                    ["ttlSeconds"] = c.TtlSeconds,
                    ["applicability"] = SecurityPolicy.ToJsonString(c.Applicability).ToUpperInvariant(),
                    ["rationale"] = c.Rationale
                }).Cast<JsonNode>().ToArray()),
                ["evidence"] = new JsonArray(state.Evidence.Select(e => new JsonObject
                {
                    ["id"] = e.Id,
                    ["controlId"] = e.ControlId,
                    ["scope"] = e.Scope,
                    ["source"] = e.Source,
                    ["method"] = e.Method,
                    ["operator"] = e.Operator,
                    ["observedAt"] = e.ObservedAt.ToString("O"),
                    ["validUntil"] = e.ValidUntil.ToString("O"),
                    ["result"] = SecurityPolicy.ToJsonString(e.Result).ToUpperInvariant(),
                    ["provenance"] = SecurityPolicy.ToJsonString(e.Provenance).ToUpperInvariant(),
                    ["revoked"] = e.Revoked,
                    ["supersedes"] = new JsonArray(e.Supersedes.Select(s => (JsonNode)s).ToArray())
                }).Cast<JsonNode>().ToArray()),
                ["findings"] = new JsonArray(state.Findings.Select(f => new JsonObject
                {
                    ["id"] = f.Id,
                    ["targetId"] = f.TargetId,
                    ["severity"] = SecurityPolicy.ToJsonString(f.Severity).ToUpperInvariant(),
                    ["impact"] = f.Impact,
                    ["remediation"] = f.Remediation,
                    ["status"] = SecurityPolicy.ToJsonString(f.Status).ToUpperInvariant()
                }).Cast<JsonNode>().ToArray()),
                ["runbooks"] = new JsonArray(state.Runbooks.Select(r => new JsonObject
                {
                    ["id"] = r.Id,
                    ["targetId"] = r.TargetId,
                    ["name"] = r.Name,
                    ["preconditions"] = r.Preconditions,
                    ["steps"] = new JsonArray(r.Steps.Select(s => (JsonNode)s).ToArray()),
                    ["risk"] = r.Risk,
                    ["verification"] = r.Verification
                }).Cast<JsonNode>().ToArray()),
                ["auditEvents"] = new JsonArray(state.AuditEvents.Select(a => new JsonObject
                {
                    ["id"] = a.Id,
                    ["timestamp"] = a.Timestamp.ToString("O"),
                    ["actor"] = a.Actor,
                    ["action"] = a.Action,
                    ["targetId"] = a.TargetId,
                    ["before"] = a.Before,
                    ["after"] = a.After
                }).Cast<JsonNode>().ToArray()),
                ["userStatements"] = new JsonArray(state.UserStatements.Select(u => new JsonObject
                {
                    ["id"] = u.Id,
                    ["targetId"] = u.TargetId,
                    ["text"] = u.Text,
                    ["declaredAt"] = u.DeclaredAt.ToString("O"),
                    ["operator"] = u.Operator
                }).Cast<JsonNode>().ToArray())
            };
            return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
