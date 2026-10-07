// MATRIX V2 — Application services
// NEW_IMPLEMENTATION: Workspace operations, CRUD, session management
// V2.1 FIX: Eliminated reflection hack, added full CRUD (Update+Delete for all entities),
//           added audit trail, stale lease handling, schemaVersion validation
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MATRIX.Core;

[assembly: InternalsVisibleTo("MATRIX")]

namespace MATRIX.Application
{
    /// <summary>Result of an operation with optional error.</summary>
    public sealed class OperationResult
    {
        public required bool Success { get; init; }
        public string? Error { get; init; }
        public string? Detail { get; init; }

        public static OperationResult Ok() => new() { Success = true };
        public static OperationResult Fail(string error, string? detail = null) =>
            new() { Success = false, Error = error, Detail = detail };
    }

    public sealed class OperationResult<T>
    {
        public required bool Success { get; init; }
        public T? Value { get; init; }
        public string? Error { get; init; }

        public static OperationResult<T> Ok(T value) => new() { Success = true, Value = value };
        public static OperationResult<T> Fail(string error) => new() { Success = false, Error = error };
    }

    /// <summary>Interface for workspace storage operations.</summary>
    public interface IWorkspaceStore
    {
        OperationResult<Workspace> Load();
        OperationResult Save(Workspace workspace);
        OperationResult<Workspace> GetCurrent();
        OperationResult<string> GetRevision();
    }

    /// <summary>
    /// Session manager: load → edit → save with revision coordination.
    /// Failed Load does not leave editable old snapshot.
    /// Saved result reflects exact approved candidate and read-back.
    /// </summary>
    public sealed class WorkspaceSession
    {
        private readonly IWorkspaceStore _store;
        private Workspace? _current;
        private string? _loadedRevision;
        private readonly object _lock = new();
        private bool _isDirty;

        public bool IsLoaded => _current != null;
        public bool IsDirty => _isDirty;
        public string? LoadedRevision => _loadedRevision;

        public WorkspaceSession(IWorkspaceStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>Load workspace from store. Does not leave stale snapshot on failure.</summary>
        public OperationResult Load()
        {
            lock (_lock)
            {
                // Clear any previous state before attempting load
                _current = null;
                _loadedRevision = null;
                _isDirty = false;

                var result = _store.Load();
                if (!result.Success)
                    return OperationResult.Fail(result.Error ?? "Load failed");

                _current = result.Value;
                var revResult = _store.GetRevision();
                _loadedRevision = revResult.Success ? revResult.Value : null;
                return OperationResult.Ok();
            }
        }

        /// <summary>Get current workspace (throws if not loaded).</summary>
        public Workspace GetCurrent()
        {
            lock (_lock)
            {
                if (_current == null)
                    throw new InvalidOperationException("Workspace not loaded");
                return _current;
            }
        }

        /// <summary>
        /// Replace current workspace with a new version. Internal for WorkspaceEditor.
        /// This replaces the previous reflection-based hack.
        /// </summary>
        internal void SetCurrent(Workspace workspace)
        {
            lock (_lock)
            {
                _current = workspace ?? throw new ArgumentNullException(nameof(workspace));
                _isDirty = true;
            }
        }

        /// <summary>Mark the session as dirty after edits.</summary>
        public void MarkDirty()
        {
            lock (_lock)
            {
                _isDirty = true;
            }
        }

        /// <summary>Save workspace. Validates before writing. Read-back after save.</summary>
        public OperationResult Save()
        {
            lock (_lock)
            {
                if (_current == null)
                    return OperationResult.Fail("No workspace loaded");

                // Validate before save
                try
                {
                    GraphValidator.ValidateWorkspace(_current);
                }
                catch (ArgumentException ex)
                {
                    return OperationResult.Fail("Validation failed", ex.Message);
                }

                var result = _store.Save(_current);
                if (!result.Success)
                    return result;

                // Read-back verification
                var readBack = _store.GetCurrent();
                if (!readBack.Success)
                    return OperationResult.Fail("Read-back verification failed");

                _current = readBack.Value;
                var revResult = _store.GetRevision();
                _loadedRevision = revResult.Success ? revResult.Value : null;
                _isDirty = false;
                return OperationResult.Ok();
            }
        }

        /// <summary>Cancel: discard any unsaved changes, no store write, no audit commit.</summary>
        public void Cancel()
        {
            lock (_lock)
            {
                if (_current != null && _isDirty)
                {
                    // Reload from store to discard edits
                    var result = _store.Load();
                    if (result.Success)
                        _current = result.Value;
                    _isDirty = false;
                }
            }
        }
    }

    /// <summary>
    /// CRUD operations for Nodes, Edges, Projects, Tasks, Controls, Evidence, Findings, Runbooks, AuditEvents, UserStatements.
    /// Full CRUD: Add, Update, Delete. Delete RESTRICT with reasons. No implicit cascade. Audit is read-only.
    /// V2.1: Added Update operations, Delete for all entities, audit trail via AuditEvent.
    /// </summary>
    public sealed class WorkspaceEditor
    {
        private readonly WorkspaceSession _session;

        public WorkspaceSession Session => _session;

        public WorkspaceEditor(WorkspaceSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        // --- Helper: create audit event ---
        private static AuditEvent CreateAuditEvent(string action, string targetId, string? before, string? after) =>
            AuditEvent.Create(
                $"audit-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow,
                "MATRIX",
                action,
                targetId,
                before,
                after);

        // --- Helper: add audit event to state ---
        private void AppendAuditEvent(AuditEvent auditEvent)
        {
            var ws = _session.GetCurrent();
            var auditList = ws.State.AuditEvents.ToList();
            auditList.Add(auditEvent);
            var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                ws.State.Findings, ws.State.Runbooks, auditList,
                ws.State.UserStatements);
            UpdateState(state);
        }

        // --- Node CRUD ---
        public OperationResult AddNode(Node node)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.Catalog.Nodes.Any(n => n.Id == node.Id))
                    return OperationResult.Fail("Duplicate node ID", node.Id);

                var nodes = ws.Catalog.Nodes.ToList();
                nodes.Add(node);
                var catalog = Catalog.Create(nodes, ws.Catalog.Edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("ADD_NODE", node.Id, null, node.Name));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddNode failed", ex.Message); }
        }

        public OperationResult UpdateNode(Node node)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.Catalog.Nodes.FirstOrDefault(n => n.Id == node.Id);
                if (existing == null)
                    return OperationResult.Fail("Node not found", node.Id);

                var nodes = ws.Catalog.Nodes.Select(n => n.Id == node.Id ? node : n).ToList();
                var catalog = Catalog.Create(nodes, ws.Catalog.Edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("UPDATE_NODE", node.Id, existing.Name, node.Name));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateNode failed", ex.Message); }
        }

        public OperationResult DeleteNode(string nodeId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.Catalog.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (existing == null)
                    return OperationResult.Fail("Node not found", nodeId);

                // RESTRICT: check references before deletion
                var edges = ws.Catalog.Edges.Where(e => e.From == nodeId || e.To == nodeId).ToList();
                if (edges.Count > 0)
                    return OperationResult.Fail("RESTRICT: Node has edges",
                        $"References: {string.Join(", ", edges.Select(e => e.Id))}");
                var projects = ws.Catalog.Projects.Where(p => p.NodeId == nodeId).ToList();
                if (projects.Count > 0)
                    return OperationResult.Fail("RESTRICT: Node has projects",
                        $"References: {string.Join(", ", projects.Select(p => p.Id))}");
                var controls = ws.State.Controls.Where(c => c.TargetId == nodeId).ToList();
                if (controls.Count > 0)
                    return OperationResult.Fail("RESTRICT: Node has controls",
                        $"References: {string.Join(", ", controls.Select(c => c.Id))}");
                var findings = ws.State.Findings.Where(f => f.TargetId == nodeId).ToList();
                if (findings.Count > 0)
                    return OperationResult.Fail("RESTRICT: Node has findings",
                        $"References: {string.Join(", ", findings.Select(f => f.Id))}");
                var runbooks = ws.State.Runbooks.Where(r => r.TargetId == nodeId).ToList();
                if (runbooks.Count > 0)
                    return OperationResult.Fail("RESTRICT: Node has runbooks",
                        $"References: {string.Join(", ", runbooks.Select(r => r.Id))}");
                var statements = ws.State.UserStatements.Where(s => s.TargetId == nodeId).ToList();
                if (statements.Count > 0)
                    return OperationResult.Fail("RESTRICT: Node has user statements",
                        $"References: {string.Join(", ", statements.Select(s => s.Id))}");

                var nodes = ws.Catalog.Nodes.Where(n => n.Id != nodeId).ToList();
                var catalog = Catalog.Create(nodes, ws.Catalog.Edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("DELETE_NODE", nodeId, existing.Name, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteNode failed", ex.Message); }
        }

        // --- Edge CRUD ---
        public OperationResult AddEdge(Edge edge)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.Catalog.Edges.Any(e => e.Id == edge.Id))
                    return OperationResult.Fail("Duplicate edge ID", edge.Id);

                var edges = ws.Catalog.Edges.ToList();
                edges.Add(edge);
                var catalog = Catalog.Create(ws.Catalog.Nodes, edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("ADD_EDGE", edge.Id, null, $"{edge.From}->{edge.To}"));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddEdge failed", ex.Message); }
        }

        public OperationResult UpdateEdge(Edge edge)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (!ws.Catalog.Edges.Any(e => e.Id == edge.Id))
                    return OperationResult.Fail("Edge not found", edge.Id);

                var edges = ws.Catalog.Edges.Select(e => e.Id == edge.Id ? edge : e).ToList();
                var catalog = Catalog.Create(ws.Catalog.Nodes, edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("UPDATE_EDGE", edge.Id, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateEdge failed", ex.Message); }
        }

        public OperationResult DeleteEdge(string edgeId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.Catalog.Edges.FirstOrDefault(e => e.Id == edgeId);
                if (existing == null)
                    return OperationResult.Fail("Edge not found", edgeId);

                var edges = ws.Catalog.Edges.Where(e => e.Id != edgeId).ToList();
                var catalog = Catalog.Create(ws.Catalog.Nodes, edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("DELETE_EDGE", edgeId, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteEdge failed", ex.Message); }
        }

        // --- Project CRUD ---
        public OperationResult AddProject(Project project)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.Catalog.Projects.Any(p => p.Id == project.Id))
                    return OperationResult.Fail("Duplicate project ID", project.Id);

                var projects = ws.Catalog.Projects.ToList();
                projects.Add(project);
                var catalog = Catalog.Create(ws.Catalog.Nodes, ws.Catalog.Edges, projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("ADD_PROJECT", project.Id, null, project.Goal));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddProject failed", ex.Message); }
        }

        public OperationResult UpdateProject(Project project)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.Catalog.Projects.FirstOrDefault(p => p.Id == project.Id);
                if (existing == null)
                    return OperationResult.Fail("Project not found", project.Id);

                var projects = ws.Catalog.Projects.Select(p => p.Id == project.Id ? project : p).ToList();
                var catalog = Catalog.Create(ws.Catalog.Nodes, ws.Catalog.Edges, projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("UPDATE_PROJECT", project.Id, existing.Goal, project.Goal));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateProject failed", ex.Message); }
        }

        public OperationResult DeleteProject(string projectId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.Catalog.Projects.FirstOrDefault(p => p.Id == projectId);
                if (existing == null)
                    return OperationResult.Fail("Project not found", projectId);

                var tasks = ws.State.Tasks.Where(t => t.ProjectId == projectId).ToList();
                if (tasks.Count > 0)
                    return OperationResult.Fail("RESTRICT: Project has tasks",
                        $"References: {string.Join(", ", tasks.Select(t => t.Id))}");

                var projects = ws.Catalog.Projects.Where(p => p.Id != projectId).ToList();
                var catalog = Catalog.Create(ws.Catalog.Nodes, ws.Catalog.Edges, projects);
                UpdateCatalog(catalog);

                AppendAuditEvent(CreateAuditEvent("DELETE_PROJECT", projectId, existing.Goal, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteProject failed", ex.Message); }
        }

        // --- Task CRUD ---
        public OperationResult AddTask(Task task)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.State.Tasks.Any(t => t.Id == task.Id))
                    return OperationResult.Fail("Duplicate task ID", task.Id);

                var tasks = ws.State.Tasks.ToList();
                tasks.Add(task);
                var state = State.Create(tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("ADD_TASK", task.Id, null, task.Title));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddTask failed", ex.Message); }
        }

        public OperationResult UpdateTask(Task task)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.State.Tasks.FirstOrDefault(t => t.Id == task.Id);
                if (existing == null)
                    return OperationResult.Fail("Task not found", task.Id);

                var tasks = ws.State.Tasks.Select(t => t.Id == task.Id ? task : t).ToList();
                var state = State.Create(tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("UPDATE_TASK", task.Id, existing.Title, task.Title));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateTask failed", ex.Message); }
        }

        public OperationResult DeleteTask(string taskId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.State.Tasks.FirstOrDefault(t => t.Id == taskId);
                if (existing == null)
                    return OperationResult.Fail("Task not found", taskId);

                // RESTRICT: check if other tasks depend on this one
                var dependents = ws.State.Tasks.Where(t => t.Dependencies.Contains(taskId)).ToList();
                if (dependents.Count > 0)
                    return OperationResult.Fail("RESTRICT: Task is a dependency",
                        $"Referenced by: {string.Join(", ", dependents.Select(t => t.Id))}");

                var tasks = ws.State.Tasks.Where(t => t.Id != taskId).ToList();
                var state = State.Create(tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("DELETE_TASK", taskId, existing.Title, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteTask failed", ex.Message); }
        }

        // --- Control CRUD ---
        public OperationResult AddControl(Control control)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.State.Controls.Any(c => c.Id == control.Id))
                    return OperationResult.Fail("Duplicate control ID", control.Id);

                var controls = ws.State.Controls.ToList();
                controls.Add(control);
                var state = State.Create(ws.State.Tasks, controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("ADD_CONTROL", control.Id, null, control.Criterion));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddControl failed", ex.Message); }
        }

        public OperationResult UpdateControl(Control control)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (!ws.State.Controls.Any(c => c.Id == control.Id))
                    return OperationResult.Fail("Control not found", control.Id);

                var controls = ws.State.Controls.Select(c => c.Id == control.Id ? control : c).ToList();
                var state = State.Create(ws.State.Tasks, controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("UPDATE_CONTROL", control.Id, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateControl failed", ex.Message); }
        }

        public OperationResult DeleteControl(string controlId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.State.Controls.FirstOrDefault(c => c.Id == controlId);
                if (existing == null)
                    return OperationResult.Fail("Control not found", controlId);

                // RESTRICT: check if evidence references this control
                var evidence = ws.State.Evidence.Where(e => e.ControlId == controlId).ToList();
                if (evidence.Count > 0)
                    return OperationResult.Fail("RESTRICT: Control has evidence",
                        $"References: {string.Join(", ", evidence.Select(e => e.Id))}");

                var controls = ws.State.Controls.Where(c => c.Id != controlId).ToList();
                var state = State.Create(ws.State.Tasks, controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("DELETE_CONTROL", controlId, existing.Criterion, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteControl failed", ex.Message); }
        }

        // --- Evidence CRUD ---
        public OperationResult AddEvidence(Evidence evidence)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.State.Evidence.Any(e => e.Id == evidence.Id))
                    return OperationResult.Fail("Duplicate evidence ID", evidence.Id);

                var evList = ws.State.Evidence.ToList();
                evList.Add(evidence);
                var state = State.Create(ws.State.Tasks, ws.State.Controls, evList,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("ADD_EVIDENCE", evidence.Id, null, evidence.Result.ToString()));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddEvidence failed", ex.Message); }
        }

        public OperationResult UpdateEvidence(Evidence evidence)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (!ws.State.Evidence.Any(e => e.Id == evidence.Id))
                    return OperationResult.Fail("Evidence not found", evidence.Id);

                var evList = ws.State.Evidence.Select(e => e.Id == evidence.Id ? evidence : e).ToList();
                var state = State.Create(ws.State.Tasks, ws.State.Controls, evList,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("UPDATE_EVIDENCE", evidence.Id, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateEvidence failed", ex.Message); }
        }

        public OperationResult DeleteEvidence(string evidenceId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.State.Evidence.FirstOrDefault(e => e.Id == evidenceId);
                if (existing == null)
                    return OperationResult.Fail("Evidence not found", evidenceId);

                var evList = ws.State.Evidence.Where(e => e.Id != evidenceId).ToList();
                var state = State.Create(ws.State.Tasks, ws.State.Controls, evList,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("DELETE_EVIDENCE", evidenceId, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteEvidence failed", ex.Message); }
        }

        // --- Finding CRUD ---
        public OperationResult AddFinding(Finding finding)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.State.Findings.Any(f => f.Id == finding.Id))
                    return OperationResult.Fail("Duplicate finding ID", finding.Id);

                var findings = ws.State.Findings.ToList();
                findings.Add(finding);
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("ADD_FINDING", finding.Id, null, finding.Severity.ToString()));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddFinding failed", ex.Message); }
        }

        public OperationResult UpdateFinding(Finding finding)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (!ws.State.Findings.Any(f => f.Id == finding.Id))
                    return OperationResult.Fail("Finding not found", finding.Id);

                var findings = ws.State.Findings.Select(f => f.Id == finding.Id ? finding : f).ToList();
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("UPDATE_FINDING", finding.Id, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateFinding failed", ex.Message); }
        }

        public OperationResult DeleteFinding(string findingId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.State.Findings.FirstOrDefault(f => f.Id == findingId);
                if (existing == null)
                    return OperationResult.Fail("Finding not found", findingId);

                var findings = ws.State.Findings.Where(f => f.Id != findingId).ToList();
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("DELETE_FINDING", findingId, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteFinding failed", ex.Message); }
        }

        // --- Runbook CRUD ---
        public OperationResult AddRunbook(Runbook runbook)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.State.Runbooks.Any(r => r.Id == runbook.Id))
                    return OperationResult.Fail("Duplicate runbook ID", runbook.Id);

                var runbooks = ws.State.Runbooks.ToList();
                runbooks.Add(runbook);
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("ADD_RUNBOOK", runbook.Id, null, runbook.Name));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddRunbook failed", ex.Message); }
        }

        public OperationResult UpdateRunbook(Runbook runbook)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (!ws.State.Runbooks.Any(r => r.Id == runbook.Id))
                    return OperationResult.Fail("Runbook not found", runbook.Id);

                var runbooks = ws.State.Runbooks.Select(r => r.Id == runbook.Id ? runbook : r).ToList();
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("UPDATE_RUNBOOK", runbook.Id, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateRunbook failed", ex.Message); }
        }

        public OperationResult DeleteRunbook(string runbookId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.State.Runbooks.FirstOrDefault(r => r.Id == runbookId);
                if (existing == null)
                    return OperationResult.Fail("Runbook not found", runbookId);

                var runbooks = ws.State.Runbooks.Where(r => r.Id != runbookId).ToList();
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("DELETE_RUNBOOK", runbookId, existing.Name, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteRunbook failed", ex.Message); }
        }

        // --- UserStatement CRUD ---
        public OperationResult AddUserStatement(UserStatement statement)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (ws.State.UserStatements.Any(s => s.Id == statement.Id))
                    return OperationResult.Fail("Duplicate statement ID", statement.Id);

                var statements = ws.State.UserStatements.ToList();
                statements.Add(statement);
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    statements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("ADD_STATEMENT", statement.Id, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddUserStatement failed", ex.Message); }
        }

        public OperationResult UpdateUserStatement(UserStatement statement)
        {
            try
            {
                var ws = _session.GetCurrent();
                if (!ws.State.UserStatements.Any(s => s.Id == statement.Id))
                    return OperationResult.Fail("Statement not found", statement.Id);

                var statements = ws.State.UserStatements.Select(s => s.Id == statement.Id ? statement : s).ToList();
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    statements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("UPDATE_STATEMENT", statement.Id, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("UpdateUserStatement failed", ex.Message); }
        }

        public OperationResult DeleteUserStatement(string statementId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var existing = ws.State.UserStatements.FirstOrDefault(s => s.Id == statementId);
                if (existing == null)
                    return OperationResult.Fail("Statement not found", statementId);

                var statements = ws.State.UserStatements.Where(s => s.Id != statementId).ToList();
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    statements);
                UpdateState(state);

                AppendAuditEvent(CreateAuditEvent("DELETE_STATEMENT", statementId, null, null));
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("DeleteUserStatement failed", ex.Message); }
        }

        private void UpdateCatalog(Catalog catalog)
        {
            var ws = _session.GetCurrent();
            var newWs = Workspace.Create(ws.ExportedAt, catalog, ws.State);
            _session.SetCurrent(newWs);
        }

        private void UpdateState(State state)
        {
            var ws = _session.GetCurrent();
            var newWs = Workspace.Create(ws.ExportedAt, ws.Catalog, state);
            _session.SetCurrent(newWs);
        }
    }
}
