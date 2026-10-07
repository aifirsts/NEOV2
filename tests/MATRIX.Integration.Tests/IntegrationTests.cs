using System;
using System.Collections.Generic;
using Xunit;
using MATRIX.Core;

namespace MATRIX.Integration.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public void Workspace_Create_ValidInput_ReturnsWorkspace()
        {
            var catalog = Catalog.Create(new List<Node>(), new List<Edge>(), new List<Project>());
            var state = State.Create(new List<Task>(), new List<Control>(), new List<Evidence>(),
                new List<Finding>(), new List<Runbook>(), new List<AuditEvent>(), new List<UserStatement>());
            var ws = Workspace.Create(DateTimeOffset.UtcNow, catalog, state);
            Assert.Equal(1, ws.SchemaVersion);
            Assert.Equal("MATRIX_WORKSPACE", Workspace.Format);
        }
    }
}
