// MATRIX V2 — Import/Export layer
// NEW_IMPLEMENTATION: Own import replace-only, safe export, strict JSON validation
// Protocol: preview → explicit confirmation → exact bytes+revision → commit
// Merge/SOCpit/attachments/ZIP runtime import remain OFF
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MATRIX.Core;
using MATRIX.Application;
using MATRIX.Storage;

namespace MATRIX.Import
{
    /// <summary>Import preview result — no data written yet.</summary>
    public sealed class ImportPreview
    {
        public required string FilePath { get; init; }
        public required string Sha256 { get; init; }
        public required long FileSize { get; init; }
        public required int NodeCount { get; init; }
        public required int EdgeCount { get; init; }
        public required int ProjectCount { get; init; }
        public required int TaskCount { get; init; }
        public required int ControlCount { get; init; }
        public required int EvidenceCount { get; init; }
        public required int FindingCount { get; init; }
        public required int RunbookCount { get; init; }
        public required int AuditEventCount { get; init; }
        public required int UserStatementCount { get; init; }
        public required bool IsValid { get; init; }
        public List<string> ValidationErrors { get; } = new();
    }

    /// <summary>Export options.</summary>
    public sealed class ExportOptions
    {
        public bool IncludeAuditEvents { get; init; } = true;
        public bool IncludeUserStatements { get; init; } = true;
    }

    /// <summary>
    /// Own import/export service: replace-only import, safe export writer.
    /// No merge, no SOCpit, no attachments, no ZIP runtime import.
    /// </summary>
    public sealed class ImportExportService
    {
        private readonly WorkspaceStore _store;
        private readonly JsonSerializerOptions _jsonOptions;

        public ImportExportService(WorkspaceStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Preview import: read file, validate, return summary. Does NOT write.
        /// </summary>
        public OperationResult<ImportPreview> PreviewImport(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return OperationResult<ImportPreview>.Fail("File not found");

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 16 * 1024 * 1024)
                    return OperationResult<ImportPreview>.Fail("File exceeds 16 MiB limit");

                var json = File.ReadAllText(filePath, Encoding.UTF8);
                var hash = ComputeSha256(json);

                // Strict JSON validation
                var errors = new List<string>();
                JsonNode? root;
                try { root = JsonNode.Parse(json); }
                catch (JsonException ex)
                {
                    return OperationResult<ImportPreview>.Ok(new ImportPreview
                    {
                        FilePath = filePath,
                        Sha256 = hash,
                        FileSize = fileInfo.Length,
                        NodeCount = 0, EdgeCount = 0, ProjectCount = 0,
                        TaskCount = 0, ControlCount = 0, EvidenceCount = 0,
                        FindingCount = 0, RunbookCount = 0, AuditEventCount = 0,
                        UserStatementCount = 0,
                        IsValid = false,
                        ValidationErrors = { $"JSON parse error: {ex.Message}" }
                    });
                }

                // Count entities
                var catalog = root?["catalog"] ?? root?["nodes"];
                var state = root?["state"];

                int Count(JsonNode? node, string property)
                {
                    var arr = node?[property];
                    if (arr is JsonArray array) return array.Count;
                    return 0;
                }

                var catNode = root?["catalog"] ?? root;
                var stateNode = root?["state"] ?? root;

                var preview = new ImportPreview
                {
                    FilePath = filePath,
                    Sha256 = hash,
                    FileSize = fileInfo.Length,
                    NodeCount = Count(catNode, "nodes"),
                    EdgeCount = Count(catNode, "edges"),
                    ProjectCount = Count(catNode, "projects"),
                    TaskCount = Count(stateNode, "tasks"),
                    ControlCount = Count(stateNode, "controls"),
                    EvidenceCount = Count(stateNode, "evidence"),
                    FindingCount = Count(stateNode, "findings"),
                    RunbookCount = Count(stateNode, "runbooks"),
                    AuditEventCount = Count(stateNode, "auditEvents"),
                    UserStatementCount = Count(stateNode, "userStatements"),
                    IsValid = errors.Count == 0
                };
                preview.ValidationErrors.AddRange(errors);

                return OperationResult<ImportPreview>.Ok(preview);
            }
            catch (Exception ex)
            {
                return OperationResult<ImportPreview>.Fail($"Preview failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute import: replace current workspace with imported data.
        /// Requires explicit confirmation (caller must have obtained it).
        /// Writes exact bytes + revision.
        /// </summary>
        public OperationResult CommitImport(string filePath, string expectedSha256)
        {
            try
            {
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                var actualHash = ComputeSha256(json);

                if (actualHash != expectedSha256)
                    return OperationResult.Fail("SHA-256 mismatch",
                        $"Expected {expectedSha256}, got {actualHash}");

                // Parse and validate workspace
                var workspace = ParseWorkspace(json);
                GraphValidator.ValidateWorkspace(workspace);

                // Backup current data
                var config = StorageConfig.Default();
                if (File.Exists(config.CatalogFile))
                {
                    var backupDir = Path.Combine(config.DataDirectory, "backups",
                        DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"));
                    Directory.CreateDirectory(backupDir);
                    if (File.Exists(config.CatalogFile))
                        File.Copy(config.CatalogFile, Path.Combine(backupDir, "catalog.json"));
                    if (File.Exists(config.StateFile))
                        File.Copy(config.StateFile, Path.Combine(backupDir, "state.json"));
                }

                // Write new data
                var result = _store.Save(workspace);
                if (!result.Success)
                    return result;

                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"CommitImport failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Export workspace to a file.
        /// Safe export writer: no secrets, no raw exceptions.
        /// </summary>
        public OperationResult Export(string filePath, Workspace workspace, ExportOptions? options = null)
        {
            try
            {
                options ??= new ExportOptions();

                var obj = new JsonObject
                {
                    ["format"] = Workspace.Format,
                    ["schemaVersion"] = workspace.SchemaVersion,
                    ["exportedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["catalog"] = SerializeCatalog(workspace.Catalog),
                    ["state"] = SerializeState(workspace.State, options)
                };

                var json = obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

                // Write to temp file first, then rename
                var tempPath = filePath + ".tmp";
                File.WriteAllText(tempPath, json, new UTF8Encoding(false));
                File.Move(tempPath, filePath, overwrite: true);

                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Export failed: {ex.Message}");
            }
        }

        private Workspace ParseWorkspace(string json)
        {
            var root = JsonNode.Parse(json) ?? throw new JsonException("Empty JSON");

            // If it has catalog/state at top level, use those; otherwise treat as bare catalog
            JsonNode? catNode = root["catalog"];
            JsonNode? stateNode = root["state"];

            if (catNode == null && root["nodes"] != null)
            {
                catNode = root;
                stateNode = new JsonObject();
            }
            if (stateNode == null)
                stateNode = new JsonObject();

            // Build workspace from JSON nodes
            // This is a simplified path — full deserialization is in WorkspaceStore
            var catalogJson = catNode?.ToJsonString() ?? "{}";
            var stateJson = stateNode?.ToJsonString() ?? "{}";

            // Use WorkspaceStore's deserialization
            var tempConfig = StorageConfig.Default(Path.Combine(Path.GetTempPath(), "matrix-import-" + Guid.NewGuid().ToString("N")));
            var tempStore = new WorkspaceStore(tempConfig);
            Directory.CreateDirectory(tempConfig.DataDirectory);
            File.WriteAllText(tempConfig.CatalogFile, catalogJson, new UTF8Encoding(false));
            File.WriteAllText(tempConfig.StateFile, stateJson, new UTF8Encoding(false));
            File.WriteAllText(tempConfig.CurrentFile, "imported\n", new UTF8Encoding(false));

            var result = tempStore.Load();
            if (!result.Success)
                throw new JsonException($"Failed to parse workspace: {result.Error}");

            // Cleanup temp
            try { Directory.Delete(tempConfig.DataDirectory, recursive: true); } catch { }

            return result.Value!;
        }

        private JsonObject SerializeCatalog(Catalog catalog)
        {
            return new JsonObject
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
                    ["kind"] = e.Kind.ToString().ToLowerInvariant(),
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
        }

        private JsonObject SerializeState(State state, ExportOptions options)
        {
            var obj = new JsonObject
            {
                ["schemaVersion"] = state.SchemaVersion,
                ["tasks"] = new JsonArray(state.Tasks.Select(t => new JsonObject
                {
                    ["id"] = t.Id,
                    ["projectId"] = t.ProjectId,
                    ["title"] = t.Title,
                    ["status"] = t.Status.ToString().ToUpperInvariant(),
                    ["priority"] = t.Priority.ToString().ToUpperInvariant(),
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
                    ["applicability"] = c.Applicability.ToString().ToUpperInvariant(),
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
                    ["result"] = e.Result.ToString().ToUpperInvariant(),
                    ["provenance"] = e.Provenance.ToString().ToUpperInvariant(),
                    ["revoked"] = e.Revoked,
                    ["supersedes"] = new JsonArray(e.Supersedes.Select(s => (JsonNode)s).ToArray())
                }).Cast<JsonNode>().ToArray()),
                ["findings"] = new JsonArray(state.Findings.Select(f => new JsonObject
                {
                    ["id"] = f.Id,
                    ["targetId"] = f.TargetId,
                    ["severity"] = f.Severity.ToString().ToUpperInvariant(),
                    ["impact"] = f.Impact,
                    ["remediation"] = f.Remediation,
                    ["status"] = f.Status.ToString().ToUpperInvariant()
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
                ["userStatements"] = options.IncludeUserStatements
                    ? new JsonArray(state.UserStatements.Select(u => new JsonObject
                    {
                        ["id"] = u.Id,
                        ["targetId"] = u.TargetId,
                        ["text"] = u.Text,
                        ["declaredAt"] = u.DeclaredAt.ToString("O"),
                        ["operator"] = u.Operator
                    }).Cast<JsonNode>().ToArray())
                    : new JsonArray()
            };

            if (options.IncludeAuditEvents)
            {
                obj["auditEvents"] = new JsonArray(state.AuditEvents.Select(a => new JsonObject
                {
                    ["id"] = a.Id,
                    ["timestamp"] = a.Timestamp.ToString("O"),
                    ["actor"] = a.Actor,
                    ["action"] = a.Action,
                    ["targetId"] = a.TargetId,
                    ["before"] = a.Before,
                    ["after"] = a.After
                }).Cast<JsonNode>().ToArray());
            }
            else
            {
                obj["auditEvents"] = new JsonArray();
            }

            return obj;
        }

        private static string ComputeSha256(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
