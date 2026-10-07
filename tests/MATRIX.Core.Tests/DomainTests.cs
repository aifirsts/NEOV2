using System;
using System.Collections.Generic;
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
        public void Edge_Create_ValidInput_ReturnsEdge()
        {
            var edge = Edge.Create("edge-1", "from-1", "to-1", EdgeKind.DependsOn, Criticality.High);
            Assert.Equal("edge-1", edge.Id);
            Assert.Equal(EdgeKind.DependsOn, edge.Kind);
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
    }
}
