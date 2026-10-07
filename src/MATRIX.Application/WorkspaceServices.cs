// MATRIX V2 — Application services
// NEW_IMPLEMENTATION: Workspace operations, CRUD, session management
using System;
using System.Collections.Generic;
using System.Linq;
using MATRIX.Core;

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
    /// Delete RESTRICT with reasons. No implicit cascade. Audit is read-only.
    /// </summary>
    public sealed class WorkspaceEditor
    {
        private readonly WorkspaceSession _session;

        public WorkspaceSession Session => _session;

        public WorkspaceEditor(WorkspaceSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        // --- Node CRUD ---
        public OperationResult AddNode(Node node)
        {
            try
            {
                var ws = _session.GetCurrent();
                var nodes = ws.Catalog.Nodes.ToList();
                if (nodes.Any(n => n.Id == node.Id))
                    return OperationResult.Fail("Duplicate node ID", node.Id);
                nodes.Add(node);
                var catalog = Catalog.Create(nodes, ws.Catalog.Edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddNode failed", ex.Message); }
        }

        public OperationResult DeleteNode(string nodeId)
        {
            try
            {
                var ws = _session.GetCurrent();
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
                var edges = ws.Catalog.Edges.ToList();
                if (edges.Any(e => e.Id == edge.Id))
                    return OperationResult.Fail("Duplicate edge ID", edge.Id);
                edges.Add(edge);
                var catalog = Catalog.Create(ws.Catalog.Nodes, edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddEdge failed", ex.Message); }
        }

        public OperationResult DeleteEdge(string edgeId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var edges = ws.Catalog.Edges.Where(e => e.Id != edgeId).ToList();
                var catalog = Catalog.Create(ws.Catalog.Nodes, edges, ws.Catalog.Projects);
                UpdateCatalog(catalog);
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
                var projects = ws.Catalog.Projects.ToList();
                if (projects.Any(p => p.Id == project.Id))
                    return OperationResult.Fail("Duplicate project ID", project.Id);
                projects.Add(project);
                var catalog = Catalog.Create(ws.Catalog.Nodes, ws.Catalog.Edges, projects);
                UpdateCatalog(catalog);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddProject failed", ex.Message); }
        }

        public OperationResult DeleteProject(string projectId)
        {
            try
            {
                var ws = _session.GetCurrent();
                var tasks = ws.State.Tasks.Where(t => t.ProjectId == projectId).ToList();
                if (tasks.Count > 0)
                    return OperationResult.Fail("RESTRICT: Project has tasks",
                        $"References: {string.Join(", ", tasks.Select(t => t.Id))}");

                var projects = ws.Catalog.Projects.Where(p => p.Id != projectId).ToList();
                var catalog = Catalog.Create(ws.Catalog.Nodes, ws.Catalog.Edges, projects);
                UpdateCatalog(catalog);
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
                var tasks = ws.State.Tasks.ToList();
                if (tasks.Any(t => t.Id == task.Id))
                    return OperationResult.Fail("Duplicate task ID", task.Id);
                tasks.Add(task);
                var state = State.Create(tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddTask failed", ex.Message); }
        }

        // --- Control CRUD ---
        public OperationResult AddControl(Control control)
        {
            try
            {
                var ws = _session.GetCurrent();
                var controls = ws.State.Controls.ToList();
                if (controls.Any(c => c.Id == control.Id))
                    return OperationResult.Fail("Duplicate control ID", control.Id);
                controls.Add(control);
                var state = State.Create(ws.State.Tasks, controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddControl failed", ex.Message); }
        }

        // --- Evidence CRUD ---
        public OperationResult AddEvidence(Evidence evidence)
        {
            try
            {
                var ws = _session.GetCurrent();
                var evList = ws.State.Evidence.ToList();
                if (evList.Any(e => e.Id == evidence.Id))
                    return OperationResult.Fail("Duplicate evidence ID", evidence.Id);
                evList.Add(evidence);
                var state = State.Create(ws.State.Tasks, ws.State.Controls, evList,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddEvidence failed", ex.Message); }
        }

        // --- Finding CRUD ---
        public OperationResult AddFinding(Finding finding)
        {
            try
            {
                var ws = _session.GetCurrent();
                var findings = ws.State.Findings.ToList();
                if (findings.Any(f => f.Id == finding.Id))
                    return OperationResult.Fail("Duplicate finding ID", finding.Id);
                findings.Add(finding);
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    findings, ws.State.Runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddFinding failed", ex.Message); }
        }

        // --- Runbook CRUD ---
        public OperationResult AddRunbook(Runbook runbook)
        {
            try
            {
                var ws = _session.GetCurrent();
                var runbooks = ws.State.Runbooks.ToList();
                if (runbooks.Any(r => r.Id == runbook.Id))
                    return OperationResult.Fail("Duplicate runbook ID", runbook.Id);
                runbooks.Add(runbook);
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, runbooks, ws.State.AuditEvents,
                    ws.State.UserStatements);
                UpdateState(state);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddRunbook failed", ex.Message); }
        }

        // --- UserStatement CRUD ---
        public OperationResult AddUserStatement(UserStatement statement)
        {
            try
            {
                var ws = _session.GetCurrent();
                var statements = ws.State.UserStatements.ToList();
                if (statements.Any(s => s.Id == statement.Id))
                    return OperationResult.Fail("Duplicate statement ID", statement.Id);
                statements.Add(statement);
                var state = State.Create(ws.State.Tasks, ws.State.Controls, ws.State.Evidence,
                    ws.State.Findings, ws.State.Runbooks, ws.State.AuditEvents,
                    statements);
                UpdateState(state);
                return OperationResult.Ok();
            }
            catch (Exception ex) { return OperationResult.Fail("AddUserStatement failed", ex.Message); }
        }

        private void UpdateCatalog(Catalog catalog)
        {
            var ws = _session.GetCurrent();
            var newWs = Workspace.Create(ws.ExportedAt, catalog, ws.State);
            _session.GetType().GetField("_current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_session, newWs);
            _session.MarkDirty();
        }

        private void UpdateState(State state)
        {
            var ws = _session.GetCurrent();
            var newWs = Workspace.Create(ws.ExportedAt, ws.Catalog, state);
            _session.GetType().GetField("_current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_session, newWs);
            _session.MarkDirty();
        }
    }
}
