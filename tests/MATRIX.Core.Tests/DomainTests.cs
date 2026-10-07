using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MATRIX.Core;

namespace MATRIX.Core.Tests
{
    public class DomainTests
    {
        [Fact]
        public void Node_Create_ValidInput_ReturnsNode()
        {
            var node = Node.Create("test-1", NodeType.Service, "Test", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown);
            Assert.Equal("test-1", node.Id);
            Assert.Equal(NodeType.Service, node.Type);
            Assert.Equal("Test", node.Name);
        }

        [Fact]
        public void Node_Create_InvalidId_Throws()
        {
            Assert.Throws<ArgumentException>(() => Node.Create("", NodeType.Service, "Test", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown));
        }

        [Fact]
        public void Node_Create_IdTooLong_Throws()
        {
            Assert.Throws<ArgumentException>(() => Node.Create(new string('a', 81), NodeType.Service, "Test", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown));
        }

        [Fact]
        public void Node_Create_InvalidChars_Throws()
        {
            Assert.Throws<ArgumentException>(() => Node.Create("test/invalid", NodeType.Service, "Test", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown));
        }

        [Fact]
        public void Edge_Create_ValidInput_ReturnsEdge()
        {
            var edge = Edge.Create("edge-1", "from-1", "to-1", EdgeKind.DependsOn, Criticality.High);
            Assert.Equal("edge-1", edge.Id);
            Assert.Equal(EdgeKind.DependsOn, edge.Kind);
        }

        [Fact]
        public void Project_Create_ValidRepoUrl_ReturnsProject()
        {
            var project = Project.Create("p-1", "n-1", "Goal", "https://github.com/aifirsts/NEOV2", "DESIGN", "Next", new[] { "M1" });
            Assert.Equal("https://github.com/aifirsts/NEOV2", project.RepoUrl);
        }

        [Fact]
        public void Project_Create_NullRepoUrl_ReturnsProject()
        {
            var project = Project.Create("p-1", "n-1", "Goal", null, "DESIGN", "Next", new[] { "M1" });
            Assert.Null(project.RepoUrl);
        }

        [Fact]
        public void Project_Create_InvalidRepoUrl_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                Project.Create("p-1", "n-1", "Goal", "http://evil.com", "DESIGN", "Next", new[] { "M1" }));
        }

        [Fact]
        public void SecurityPolicy_ParseEnum_SnakeCase_Works()
        {
            Assert.Equal(EdgeKind.DependsOn, SecurityPolicy.ParseEnum<EdgeKind>("depends_on"));
            Assert.Equal(TaskStatus.InProgress, SecurityPolicy.ParseEnum<TaskStatus>("in_progress"));
            Assert.Equal(ControlApplicability.NotApplicable, SecurityPolicy.ParseEnum<ControlApplicability>("not_applicable"));
            Assert.Equal(EvidenceProvenance.VerifiedManual, SecurityPolicy.ParseEnum<EvidenceProvenance>("verified_manual"));
            Assert.Equal(FindingStatus.AcceptedRisk, SecurityPolicy.ParseEnum<FindingStatus>("accepted_risk"));
        }

        [Fact]
        public void SecurityPolicy_ToJsonString_SnakeCase_Works()
        {
            Assert.Equal("depends_on", SecurityPolicy.ToJsonString(EdgeKind.DependsOn));
            Assert.Equal("in_progress", SecurityPolicy.ToJsonString(TaskStatus.InProgress));
            Assert.Equal("not_applicable", SecurityPolicy.ToJsonString(ControlApplicability.NotApplicable));
        }

        [Fact]
        public void SecurityPolicy_Compute_NoControls_ReturnsUnknown()
        {
            var result = SecurityPolicy.Compute("node-1", new List<Control>(), new List<Evidence>(), DateTimeOffset.UtcNow);
            Assert.Equal(Security.Unknown, result);
        }

        [Fact]
        public void SecurityPolicy_Compute_RevokedEvidence_ReturnsUnknown()
        {
            var now = DateTimeOffset.UtcNow;
            var control = Control.Create("ctrl-1", "node-1", "Test", "scope", "method", true, Criticality.High, 86400, ControlApplicability.Applicable, "reason");
            var evidence = Evidence.Create("ev-1", "ctrl-1", "scope", "source", "method", "NEO", now.AddDays(-1), now.AddDays(1), EvidenceResult.Pass, EvidenceProvenance.VerifiedManual, true, new List<string>());
            var result = SecurityPolicy.Compute("node-1", new[] { control }, new[] { evidence }, now);
            Assert.Equal(Security.Unknown, result);
        }

        [Fact]
        public void SecurityPolicy_Compute_ExpiredEvidence_ReturnsUnknown()
        {
            var now = DateTimeOffset.UtcNow;
            var control = Control.Create("ctrl-1", "node-1", "Test", "scope", "method", true, Criticality.High, 86400, ControlApplicability.Applicable, "reason");
            var evidence = Evidence.Create("ev-1", "ctrl-1", "scope", "source", "method", "NEO", now.AddDays(-2), now.AddDays(-1), EvidenceResult.Pass, EvidenceProvenance.VerifiedManual, false, new List<string>());
            var result = SecurityPolicy.Compute("node-1", new[] { control }, new[] { evidence }, now);
            Assert.Equal(Security.Unknown, result);
        }

        [Fact]
        public void SecurityPolicy_Compute_FailEvidence_ReturnsFail()
        {
            var now = DateTimeOffset.UtcNow;
            var control = Control.Create("ctrl-1", "node-1", "Test", "scope", "method", true, Criticality.High, 86400, ControlApplicability.Applicable, "reason");
            var evidence = Evidence.Create("ev-1", "ctrl-1", "scope", "source", "method", "NEO", now, now.AddDays(1), EvidenceResult.Fail, EvidenceProvenance.VerifiedManual, false, new List<string>());
            var result = SecurityPolicy.Compute("node-1", new[] { control }, new[] { evidence }, now);
            Assert.Equal(Security.Fail, result);
        }

        [Fact]
        public void SecurityPolicy_Compute_PassEvidence_ReturnsPass()
        {
            var now = DateTimeOffset.UtcNow;
            var control = Control.Create("ctrl-1", "node-1", "Test", "scope", "method", true, Criticality.High, 86400, ControlApplicability.Applicable, "reason");
            var evidence = Evidence.Create("ev-1", "ctrl-1", "scope", "source", "method", "NEO", now, now.AddDays(1), EvidenceResult.Pass, EvidenceProvenance.VerifiedManual, false, new List<string>());
            var result = SecurityPolicy.Compute("node-1", new[] { control }, new[] { evidence }, now);
            Assert.Equal(Security.Pass, result);
        }

        [Fact]
        public void GraphValidator_ValidateCatalog_DuplicateNodeIds_Throws()
        {
            var node = Node.Create("dup-1", NodeType.Service, "Dup", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown);
            var catalog = Catalog.Create(new[] { node, node }, new List<Edge>(), new List<Project>());
            Assert.Throws<ArgumentException>(() => GraphValidator.ValidateCatalog(catalog));
        }

        [Fact]
        public void GraphValidator_ValidateCatalog_DanglingEdge_Throws()
        {
            var node = Node.Create("n-1", NodeType.Service, "N1", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown);
            var edge = Edge.Create("e-1", "n-1", "missing", EdgeKind.DependsOn, Criticality.High);
            var catalog = Catalog.Create(new[] { node }, new[] { edge }, new List<Project>());
            Assert.Throws<ArgumentException>(() => GraphValidator.ValidateCatalog(catalog));
        }

        [Fact]
        public void GraphValidator_ValidateCatalog_SelfEdge_Throws()
        {
            var node = Node.Create("n-1", NodeType.Service, "N1", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown);
            var edge = Edge.Create("e-1", "n-1", "n-1", EdgeKind.DependsOn, Criticality.High);
            var catalog = Catalog.Create(new[] { node }, new[] { edge }, new List<Project>());
            Assert.Throws<ArgumentException>(() => GraphValidator.ValidateCatalog(catalog));
        }

        [Fact]
        public void GraphValidator_ValidateState_DuplicateFindingId_Throws()
        {
            var node = Node.Create("n-1", NodeType.Service, "N1", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown);
            var finding = Finding.Create("f-1", "n-1", FindingSeverity.Low, "impact", "remediation", FindingStatus.Open);
            var catalog = Catalog.Create(new[] { node }, new List<Edge>(), new List<Project>());
            var state = State.Create(new List<Task>(), new List<Control>(), new List<Evidence>(),
                new[] { finding, finding }, new List<Runbook>(), new List<AuditEvent>(), new List<UserStatement>());
            Assert.Throws<ArgumentException>(() => GraphValidator.ValidateState(state, catalog));
        }

        [Fact]
        public void GraphValidator_ValidateState_DuplicateRunbookId_Throws()
        {
            var node = Node.Create("n-1", NodeType.Service, "N1", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown);
            var runbook = Runbook.Create("rb-1", "n-1", "RB", "preconditions", new[] { "step1" }, "risk", "verification");
            var catalog = Catalog.Create(new[] { node }, new List<Edge>(), new List<Project>());
            var state = State.Create(new List<Task>(), new List<Control>(), new List<Evidence>(),
                new List<Finding>(), new[] { runbook, runbook }, new List<AuditEvent>(), new List<UserStatement>());
            Assert.Throws<ArgumentException>(() => GraphValidator.ValidateState(state, catalog));
        }

        [Fact]
        public void GraphValidator_ValidateState_DuplicateAuditEventId_Throws()
        {
            var node = Node.Create("n-1", NodeType.Service, "N1", "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown);
            var audit = AuditEvent.Create("a-1", DateTimeOffset.UtcNow, "NEO", "TEST", "n-1", null, null);
            var catalog = Catalog.Create(new[] { node }, new List<Edge>(), new List<Project>());
            var state = State.Create(new List<Task>(), new List<Control>(), new List<Evidence>(),
                new List<Finding>(), new List<Runbook>(), new[] { audit, audit }, new List<UserStatement>());
            Assert.Throws<ArgumentException>(() => GraphValidator.ValidateState(state, catalog));
        }

        [Fact]
        public void SecurityPolicy_ValidateSupersedes_MultiLinkCycle_Detected()
        {
            // ev-1 -> [ev-2, ev-3], ev-3 -> ev-1 (cycle through second link)
            var ev1 = Evidence.Create("ev-1", "ctrl-1", "scope", "source", "method", "op", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), EvidenceResult.Pass, EvidenceProvenance.VerifiedManual, false, new[] { "ev-2", "ev-3" });
            var ev2 = Evidence.Create("ev-2", "ctrl-1", "scope", "source", "method", "op", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), EvidenceResult.Pass, EvidenceProvenance.VerifiedManual, false, new List<string>());
            var ev3 = Evidence.Create("ev-3", "ctrl-1", "scope", "source", "method", "op", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), EvidenceResult.Pass, EvidenceProvenance.VerifiedManual, false, new[] { "ev-1" });
            var evidence = new[] { ev1, ev2, ev3 };
            var result = SecurityPolicy.ValidateSupersedes(evidence, out var error);
            Assert.False(result);
            Assert.Contains("cycle", error?.ToLowerInvariant() ?? "");
        }
    }
}
