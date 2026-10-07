// MATRIX V2 — Security policy and graph validation
// NEW_IMPLEMENTATION: ADR-V2-SEC-001 — conservative security computation
using System;
using System.Collections.Generic;
using System.Linq;

namespace MATRIX.Core
{
    /// <summary>
    /// ADR-V2-SEC-001: Security computation policy.
    /// UNKNOWN, STALE, MISSING, empty set and user statements never produce PASS.
    /// N/A requires rationale and is not a hidden PASS.
    /// Evidence result PASS is limited to criterion/scope/validity.
    /// </summary>
    public static class SecurityPolicy
    {
        /// <summary>Maximum allowed JSON depth for workspace files.</summary>
        public const int MaxJsonDepth = 32;

        /// <summary>Maximum input size in bytes (16 MiB).</summary>
        public const long MaxInputBytes = 16 * 1024 * 1024;

        /// <summary>Maximum allowed tokens (informational; enforced by deserializer limits).</summary>
        public const int MaxTokens = 1_000_000;

        /// <summary>Max fraction digits for UTC timestamps.</summary>
        public const int MaxFractionDigits = 7;

        /// <summary>
        /// Compute the Security status of a Node from its Controls and Evidence.
        /// </summary>
        public static Security Compute(
            string nodeId,
            IReadOnlyList<Control> controls,
            IReadOnlyList<Evidence> evidence,
            DateTimeOffset now)
        {
            var nodeControls = controls.Where(c => c.TargetId == nodeId).ToList();
            if (nodeControls.Count == 0)
                return Security.Unknown;

            var applicable = nodeControls.Where(c => c.Applicability == ControlApplicability.Applicable).ToList();
            if (applicable.Count == 0)
                return Security.Unknown;

            var required = applicable.Where(c => c.Required).ToList();
            if (required.Count == 0)
                return Security.Unknown;

            foreach (var control in required)
            {
                var controlEvidence = evidence
                    .Where(e => e.ControlId == control.Id && !e.Revoked)
                    .ToList();

                if (controlEvidence.Count == 0)
                    return Security.Unknown;

                var fresh = controlEvidence
                    .Where(e => e.ValidUntil > now)
                    .ToList();

                if (fresh.Count == 0)
                    return Security.Unknown;

                var passing = fresh.Any(e => e.Result == EvidenceResult.Pass);
                var failing = fresh.Any(e => e.Result == EvidenceResult.Fail);
                var blocked = fresh.Any(e => e.Result == EvidenceResult.Blocked);

                if (failing)
                    return Security.Fail;
                if (blocked && !passing)
                    return Security.Blocked;
                if (!passing)
                    return Security.Unknown;
            }

            return Security.Pass;
        }

        /// <summary>
        /// Check freshness of evidence: evidence is fresh if validUntil > now and not revoked.
        /// </summary>
        public static bool IsFresh(Evidence evidence, DateTimeOffset now) =>
            !evidence.Revoked && evidence.ValidUntil > now;

        /// <summary>
        /// Check if evidence is stale (expired but not revoked).
        /// </summary>
        public static bool IsStale(Evidence evidence, DateTimeOffset now) =>
            !evidence.Revoked && evidence.ValidUntil <= now;

        /// <summary>
        /// Validate that supersedes chains have no cycles or duplicates.
        /// </summary>
        public static bool ValidateSupersedes(
            IReadOnlyList<Evidence> evidence,
            out string? error)
        {
            error = null;
            var evidenceById = evidence.ToDictionary(e => e.Id, e => e);

            foreach (var ev in evidence)
            {
                var seen = new HashSet<string>();
                var current = ev.Id;
                while (true)
                {
                    if (!seen.Add(current))
                    {
                        error = $"Supersedes cycle detected involving evidence '{current}'";
                        return false;
                    }
                    var supersedes = evidenceById.TryGetValue(current, out var currentEv)
                        ? currentEv.Supersedes
                        : null;
                    if (supersedes == null || supersedes.Count == 0)
                        break;
                    // Follow first superseded link for cycle detection
                    var dupes = supersedes.GroupBy(s => s).Where(g => g.Count() > 1).ToList();
                    if (dupes.Count > 0)
                    {
                        error = $"Duplicate supersedes entries in evidence '{current}': {string.Join(", ", dupes.Select(d => d.Key))}";
                        return false;
                    }
                    current = supersedes[0];
                    if (!evidenceById.ContainsKey(current))
                    {
                        // Dangling supersede reference — allowed in historical audit
                        break;
                    }
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Graph validation: global ID uniqueness, dangling edges, duplicate edges,
    /// task dependency DAG (no cycles, no duplicates, no self-deps).
    /// </summary>
    public static class GraphValidator
    {
        /// <summary>Validate catalog: node ID uniqueness, edge references, edge uniqueness.</summary>
        public static void ValidateCatalog(Catalog catalog)
        {
            // Node ID uniqueness
            var nodeIds = new HashSet<string>();
            foreach (var node in catalog.Nodes)
            {
                if (!nodeIds.Add(node.Id))
                    throw new ArgumentException($"Duplicate node ID: {node.Id}");
            }

            // Edge references and uniqueness
            var edgeKeys = new HashSet<string>();
            foreach (var edge in catalog.Edges)
            {
                if (!nodeIds.Contains(edge.From))
                    throw new ArgumentException($"Dangling edge '{edge.Id}': from '{edge.From}' not found");
                if (!nodeIds.Contains(edge.To))
                    throw new ArgumentException($"Dangling edge '{edge.Id}': to '{edge.To}' not found");
                if (edge.From == edge.To)
                    throw new ArgumentException($"Self-referencing edge: {edge.Id}");

                var key = $"{edge.From}|{edge.To}|{edge.Kind}";
                if (!edgeKeys.Add(key))
                    throw new ArgumentException($"Duplicate edge relationship: {edge.Id} ({key})");
            }

            // Project nodeId references must point to project-type Nodes
            var projectNodeIds = new HashSet<string>(
                catalog.Nodes.Where(n => n.Type == NodeType.Project).Select(n => n.Id));
            foreach (var project in catalog.Projects)
            {
                if (!nodeIds.Contains(project.NodeId))
                    throw new ArgumentException($"Project '{project.Id}' references missing node '{project.NodeId}'");
                if (!projectNodeIds.Contains(project.NodeId))
                    throw new ArgumentException($"Project '{project.Id}' node '{project.NodeId}' is not type Project");
            }

            // Project ID uniqueness
            var projectIds = new HashSet<string>();
            foreach (var project in catalog.Projects)
            {
                if (!projectIds.Add(project.Id))
                    throw new ArgumentException($"Duplicate project ID: {project.Id}");
            }
        }

        /// <summary>Validate state: task dependencies DAG, control references, evidence references.</summary>
        public static void ValidateState(State state, Catalog catalog)
        {
            var nodeIds = new HashSet<string>(catalog.Nodes.Select(n => n.Id));
            var projectIds = new HashSet<string>(catalog.Projects.Select(p => p.Id));
            var controlIds = new HashSet<string>();

            // Control references
            foreach (var control in state.Controls)
            {
                if (!nodeIds.Contains(control.TargetId))
                    throw new ArgumentException($"Control '{control.Id}' references missing node '{control.TargetId}'");
                if (!controlIds.Add(control.Id))
                    throw new ArgumentException($"Duplicate control ID: {control.Id}");
            }

            // Evidence references
            var evidenceIds = new HashSet<string>();
            foreach (var evidence in state.Evidence)
            {
                if (!controlIds.Contains(evidence.ControlId))
                    throw new ArgumentException($"Evidence '{evidence.Id}' references missing control '{evidence.ControlId}'");
                if (!evidenceIds.Add(evidence.Id))
                    throw new ArgumentException($"Duplicate evidence ID: {evidence.Id}");
            }

            // Validate supersedes
            if (!SecurityPolicy.ValidateSupersedes(state.Evidence, out var supersedesError))
                throw new ArgumentException(supersedesError ?? "Supersedes validation failed");

            // Task references and dependencies
            var taskIds = new HashSet<string>();
            foreach (var task in state.Tasks)
            {
                if (!projectIds.Contains(task.ProjectId))
                    throw new ArgumentException($"Task '{task.Id}' references missing project '{task.ProjectId}'");
                if (!taskIds.Add(task.Id))
                    throw new ArgumentException($"Duplicate task ID: {task.Id}");

                // Check for duplicate dependencies
                var depSet = new HashSet<string>();
                foreach (var dep in task.Dependencies)
                {
                    if (!depSet.Add(dep))
                        throw new ArgumentException($"Duplicate dependency '{dep}' in task '{task.Id}'");
                    if (dep == task.Id)
                        throw new ArgumentException($"Self-dependency in task '{task.Id}'");
                }
            }

            // Task dependency DAG: no cycles
            var taskMap = state.Tasks.ToDictionary(t => t.Id, t => t.Dependencies);
            foreach (var taskId in taskIds)
            {
                ValidateTaskDfs(taskId, taskMap, new HashSet<string>(), new HashSet<string>(), out var cycleTask);
                if (cycleTask != null)
                    throw new ArgumentException($"Task dependency cycle detected involving '{cycleTask}'");
            }

            // Finding/Runbook/UserStatement target references
            foreach (var finding in state.Findings)
            {
                if (!nodeIds.Contains(finding.TargetId))
                    throw new ArgumentException($"Finding '{finding.Id}' references missing node '{finding.TargetId}'");
            }

            foreach (var runbook in state.Runbooks)
            {
                if (!nodeIds.Contains(runbook.TargetId))
                    throw new ArgumentException($"Runbook '{runbook.Id}' references missing node '{runbook.TargetId}'");
            }

            foreach (var statement in state.UserStatements)
            {
                if (!nodeIds.Contains(statement.TargetId))
                    throw new ArgumentException($"UserStatement '{statement.Id}' references missing node '{statement.TargetId}'");
            }
        }

        private static void ValidateTaskDfs(
            string taskId,
            IReadOnlyDictionary<string, IReadOnlyList<string>> taskMap,
            HashSet<string> visiting,
            HashSet<string> visited,
            out string? cycleTask)
        {
            cycleTask = null;
            if (visited.Contains(taskId)) return;
            if (!visiting.Add(taskId))
            {
                cycleTask = taskId;
                return;
            }

            if (taskMap.TryGetValue(taskId, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (taskMap.ContainsKey(dep))
                        ValidateTaskDfs(dep, taskMap, visiting, visited, out cycleTask);
                    if (cycleTask != null) return;
                }
            }

            visiting.Remove(taskId);
            visited.Add(taskId);
        }

        /// <summary>Validate the complete workspace: catalog + state.</summary>
        public static void ValidateWorkspace(Workspace workspace)
        {
            ValidateCatalog(workspace.Catalog);
            ValidateState(workspace.State, workspace.Catalog);
        }
    }
}
