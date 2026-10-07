// MATRIX V2 — Core domain models
// NEW_IMPLEMENTATION: Full domain models per V1 contract (matrix-workspace.schema.json v1)
// Core does NOT depend on WPF, network, filesystem, or command execution.
using System;
using System.Collections.Generic;
using System.Linq;

namespace MATRIX.Core
{
    /// <summary>Computed security status of a Node based on its Controls and Evidence.</summary>
    public enum Security
    {
        Unknown,
        Pass,
        Fail,
        Blocked,
        NotApplicable
    }

    /// <summary>Node types in the infrastructure catalog.</summary>
    public enum NodeType
    {
        Device,
        Service,
        Account,
        Vps,
        Project,
        Agent
    }

    /// <summary>Sensitivity classification for a Node.</summary>
    public enum Sensitivity
    {
        Public,
        Internal,
        Sensitive
    }

    /// <summary>Availability status of a Node.</summary>
    public enum Availability
    {
        Unknown,
        Up,
        Down
    }

    /// <summary>Edge kinds describing relationships between Nodes.</summary>
    public enum EdgeKind
    {
        DependsOn,
        ConnectsTo,
        AuthenticatesWith,
        HostedOn,
        DeployedFrom,
        BacksUp,
        Monitors,
        PaysThrough
    }

    /// <summary>Criticality level for Edges, Controls, and Findings.</summary>
    public enum Criticality
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>Task lifecycle status.</summary>
    public enum TaskStatus
    {
        Todo,
        InProgress,
        Blocked,
        Done
    }

    /// <summary>Task priority.</summary>
    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>Control applicability.</summary>
    public enum ControlApplicability
    {
        Applicable,
        NotApplicable
    }

    /// <summary>Evidence result.</summary>
    public enum EvidenceResult
    {
        Pass,
        Fail,
        Blocked
    }

    /// <summary>Evidence provenance.</summary>
    public enum EvidenceProvenance
    {
        VerifiedManual,
        Machine
    }

    /// <summary>Finding severity.</summary>
    public enum FindingSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>Finding lifecycle status.</summary>
    public enum FindingStatus
    {
        Open,
        InProgress,
        Resolved,
        AcceptedRisk
    }

    /// <summary>A resource in the infrastructure catalog.</summary>
    public sealed class Node
    {
        public required string Id { get; init; }
        public required NodeType Type { get; init; }
        public required string Name { get; init; }
        public required string Owner { get; init; }
        public required string TrustZone { get; init; }
        public required Sensitivity Sensitivity { get; init; }
        public required Availability Availability { get; init; }

        public static Node Create(string id, NodeType type, string name, string owner,
            string trustZone, Sensitivity sensitivity, Availability availability) =>
            new()
            {
                Id = ValidateId(id),
                Type = type,
                Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("name") : name.Trim(),
                Owner = string.IsNullOrWhiteSpace(owner) ? throw new ArgumentException("owner") : owner.Trim(),
                TrustZone = string.IsNullOrWhiteSpace(trustZone) ? throw new ArgumentException("trustZone") : trustZone.Trim(),
                Sensitivity = sensitivity,
                Availability = availability
            };

        internal static string ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Node id must not be empty");
            if (id.Length > 80)
                throw new ArgumentException("Node id exceeds 80 chars");
            if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-zA-Z0-9][a-zA-Z0-9._-]*$"))
                throw new ArgumentException("Node id has invalid characters");
            return id;
        }
    }

    /// <summary>A directed relationship between two Nodes.</summary>
    public sealed class Edge
    {
        public required string Id { get; init; }
        public required string From { get; init; }
        public required string To { get; init; }
        public required EdgeKind Kind { get; init; }
        public required Criticality Criticality { get; init; }

        public static Edge Create(string id, string from, string to, EdgeKind kind, Criticality criticality) =>
            new()
            {
                Id = Node.ValidateId(id),
                From = Node.ValidateId(from),
                To = Node.ValidateId(to),
                Kind = kind,
                Criticality = criticality
            };
    }

    /// <summary>A project tracked in MATRIX, attached to a project-type Node.</summary>
    public sealed class Project
    {
        public required string Id { get; init; }
        public required string NodeId { get; init; }
        public required string Goal { get; init; }
        public required string? RepoUrl { get; init; }
        public required string Stage { get; init; }
        public required string NextAction { get; init; }
        public required IReadOnlyList<string> Milestones { get; init; }

        public static Project Create(string id, string nodeId, string goal, string? repoUrl,
            string stage, string nextAction, IReadOnlyList<string> milestones) =>
            new()
            {
                Id = Node.ValidateId(id),
                NodeId = Node.ValidateId(nodeId),
                Goal = goal ?? throw new ArgumentException("goal"),
                RepoUrl = repoUrl == null || (repoUrl.StartsWith("https://") && repoUrl.Contains("github.com"))
                    ? repoUrl
                    : throw new ArgumentException("repoUrl must be HTTPS github.com URL or null"),
                Stage = string.IsNullOrWhiteSpace(stage) ? throw new ArgumentException("stage") : stage.Trim(),
                NextAction = nextAction ?? throw new ArgumentException("nextAction"),
                Milestones = milestones ?? throw new ArgumentException("milestones")
            };
    }

    /// <summary>A task within a Project, with dependency DAG.</summary>
    public sealed class Task
    {
        public required string Id { get; init; }
        public required string ProjectId { get; init; }
        public required string Title { get; init; }
        public required TaskStatus Status { get; init; }
        public required TaskPriority Priority { get; init; }
        public required IReadOnlyList<string> Dependencies { get; init; }

        public static Task Create(string id, string projectId, string title, TaskStatus status,
            TaskPriority priority, IReadOnlyList<string> dependencies) =>
            new()
            {
                Id = Node.ValidateId(id),
                ProjectId = Node.ValidateId(projectId),
                Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("title") : title.Trim(),
                Status = status,
                Priority = priority,
                Dependencies = dependencies ?? throw new ArgumentException("dependencies")
            };
    }

    /// <summary>A control/check applied to a Node.</summary>
    public sealed class Control
    {
        public required string Id { get; init; }
        public required string TargetId { get; init; }
        public required string Criterion { get; init; }
        public required string Scope { get; init; }
        public required string Method { get; init; }
        public required bool Required { get; init; }
        public required Criticality Criticality { get; init; }
        public required int TtlSeconds { get; init; }
        public required ControlApplicability Applicability { get; init; }
        public required string Rationale { get; init; }

        public static Control Create(string id, string targetId, string criterion, string scope,
            string method, bool required, Criticality criticality, int ttlSeconds,
            ControlApplicability applicability, string rationale) =>
            new()
            {
                Id = Node.ValidateId(id),
                TargetId = Node.ValidateId(targetId),
                Criterion = criterion ?? throw new ArgumentException("criterion"),
                Scope = scope ?? throw new ArgumentException("scope"),
                Method = method ?? throw new ArgumentException("method"),
                Required = required,
                Criticality = criticality,
                TtlSeconds = ttlSeconds < 1 || ttlSeconds > 31536000
                    ? throw new ArgumentException("ttlSeconds out of range")
                    : ttlSeconds,
                Applicability = applicability,
                Rationale = rationale ?? throw new ArgumentException("rationale")
            };
    }

    /// <summary>Evidence for a Control at a point in time.</summary>
    public sealed class Evidence
    {
        public required string Id { get; init; }
        public required string ControlId { get; init; }
        public required string Scope { get; init; }
        public required string Source { get; init; }
        public required string Method { get; init; }
        public required string Operator { get; init; }
        public required DateTimeOffset ObservedAt { get; init; }
        public required DateTimeOffset ValidUntil { get; init; }
        public required EvidenceResult Result { get; init; }
        public required EvidenceProvenance Provenance { get; init; }
        public required bool Revoked { get; init; }
        public required IReadOnlyList<string> Supersedes { get; init; }

        public static Evidence Create(string id, string controlId, string scope, string source,
            string method, string operatorName, DateTimeOffset observedAt, DateTimeOffset validUntil,
            EvidenceResult result, EvidenceProvenance provenance, bool revoked,
            IReadOnlyList<string> supersedes) =>
            new()
            {
                Id = Node.ValidateId(id),
                ControlId = Node.ValidateId(controlId),
                Scope = scope ?? throw new ArgumentException("scope"),
                Source = source ?? throw new ArgumentException("source"),
                Method = method ?? throw new ArgumentException("method"),
                Operator = operatorName ?? throw new ArgumentException("operator"),
                ObservedAt = observedAt,
                ValidUntil = validUntil,
                Result = result,
                Provenance = provenance,
                Revoked = revoked,
                Supersedes = supersedes ?? throw new ArgumentException("supersedes")
            };
    }

    /// <summary>A finding/vulnerability on a Node.</summary>
    public sealed class Finding
    {
        public required string Id { get; init; }
        public required string TargetId { get; init; }
        public required FindingSeverity Severity { get; init; }
        public required string Impact { get; init; }
        public required string Remediation { get; init; }
        public required FindingStatus Status { get; init; }

        public static Finding Create(string id, string targetId, FindingSeverity severity,
            string impact, string remediation, FindingStatus status) =>
            new()
            {
                Id = Node.ValidateId(id),
                TargetId = Node.ValidateId(targetId),
                Severity = severity,
                Impact = impact ?? throw new ArgumentException("impact"),
                Remediation = remediation ?? throw new ArgumentException("remediation"),
                Status = status
            };
    }

    /// <summary>A runbook for recovery or operations on a Node.</summary>
    public sealed class Runbook
    {
        public required string Id { get; init; }
        public required string TargetId { get; init; }
        public required string Name { get; init; }
        public required string Preconditions { get; init; }
        public required IReadOnlyList<string> Steps { get; init; }
        public required string Risk { get; init; }
        public required string Verification { get; init; }

        public static Runbook Create(string id, string targetId, string name, string preconditions,
            IReadOnlyList<string> steps, string risk, string verification) =>
            new()
            {
                Id = Node.ValidateId(id),
                TargetId = Node.ValidateId(targetId),
                Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("name") : name.Trim(),
                Preconditions = preconditions ?? throw new ArgumentException("preconditions"),
                Steps = steps ?? throw new ArgumentException("steps"),
                Risk = risk ?? throw new ArgumentException("risk"),
                Verification = verification ?? throw new ArgumentException("verification")
            };
    }

    /// <summary>An immutable audit event in the local audit log.</summary>
    public sealed class AuditEvent
    {
        public required string Id { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
        public required string Actor { get; init; }
        public required string Action { get; init; }
        public required string TargetId { get; init; }
        public required string? Before { get; init; }
        public required string? After { get; init; }

        public static AuditEvent Create(string id, DateTimeOffset timestamp, string actor,
            string action, string targetId, string? before, string? after) =>
            new()
            {
                Id = Node.ValidateId(id),
                Timestamp = timestamp,
                Actor = string.IsNullOrWhiteSpace(actor) ? throw new ArgumentException("actor") : actor.Trim(),
                Action = string.IsNullOrWhiteSpace(action) ? throw new ArgumentException("action") : action.Trim(),
                TargetId = Node.ValidateId(targetId),
                Before = before,
                After = after
            };
    }

    /// <summary>A user statement about a Node — not Evidence.</summary>
    public sealed class UserStatement
    {
        public required string Id { get; init; }
        public required string TargetId { get; init; }
        public required string Text { get; init; }
        public required DateTimeOffset DeclaredAt { get; init; }
        public required string Operator { get; init; }

        public static UserStatement Create(string id, string targetId, string text,
            DateTimeOffset declaredAt, string operatorName) =>
            new()
            {
                Id = Node.ValidateId(id),
                TargetId = Node.ValidateId(targetId),
                Text = text ?? throw new ArgumentException("text"),
                DeclaredAt = declaredAt,
                Operator = string.IsNullOrWhiteSpace(operatorName) ? throw new ArgumentException("operator") : operatorName.Trim()
            };
    }

    /// <summary>The catalog: Nodes, Edges, and Projects.</summary>
    public sealed class Catalog
    {
        public required int SchemaVersion { get; init; }
        public required IReadOnlyList<Node> Nodes { get; init; }
        public required IReadOnlyList<Edge> Edges { get; init; }
        public required IReadOnlyList<Project> Projects { get; init; }

        public static Catalog Create(IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges, IReadOnlyList<Project> projects) =>
            new()
            {
                SchemaVersion = 1,
                Nodes = nodes ?? throw new ArgumentException("nodes"),
                Edges = edges ?? throw new ArgumentException("edges"),
                Projects = projects ?? throw new ArgumentException("projects")
            };
    }

    /// <summary>The state: Tasks, Controls, Evidence, Findings, Runbooks, AuditEvents, UserStatements.</summary>
    public sealed class State
    {
        public required int SchemaVersion { get; init; }
        public required IReadOnlyList<Task> Tasks { get; init; }
        public required IReadOnlyList<Control> Controls { get; init; }
        public required IReadOnlyList<Evidence> Evidence { get; init; }
        public required IReadOnlyList<Finding> Findings { get; init; }
        public required IReadOnlyList<Runbook> Runbooks { get; init; }
        public required IReadOnlyList<AuditEvent> AuditEvents { get; init; }
        public required IReadOnlyList<UserStatement> UserStatements { get; init; }

        public static State Create(IReadOnlyList<Task> tasks, IReadOnlyList<Control> controls,
            IReadOnlyList<Evidence> evidence, IReadOnlyList<Finding> findings,
            IReadOnlyList<Runbook> runbooks, IReadOnlyList<AuditEvent> auditEvents,
            IReadOnlyList<UserStatement> userStatements) =>
            new()
            {
                SchemaVersion = 1,
                Tasks = tasks ?? throw new ArgumentException("tasks"),
                Controls = controls ?? throw new ArgumentException("controls"),
                Evidence = evidence ?? throw new ArgumentException("evidence"),
                Findings = findings ?? throw new ArgumentException("findings"),
                Runbooks = runbooks ?? throw new ArgumentException("runbooks"),
                AuditEvents = auditEvents ?? throw new ArgumentException("auditEvents"),
                UserStatements = userStatements ?? throw new ArgumentException("userStatements")
            };
    }

    /// <summary>The complete workspace: Catalog + State.</summary>
    public sealed class Workspace
    {
        public const string Format = "MATRIX_WORKSPACE";
        public required int SchemaVersion { get; init; }
        public required DateTimeOffset ExportedAt { get; init; }
        public required Catalog Catalog { get; init; }
        public required State State { get; init; }

        public static Workspace Create(DateTimeOffset exportedAt, Catalog catalog, State state) =>
            new()
            {
                SchemaVersion = 1,
                ExportedAt = exportedAt,
                Catalog = catalog ?? throw new ArgumentException("catalog"),
                State = state ?? throw new ArgumentException("state")
            };
    }
}
