using Xunit;
using MATRIX.Application;
using MATRIX.Core;

namespace MATRIX.Application.Tests
{
    public class WorkspaceSessionTests
    {
        [Fact]
        public void OperationResult_Ok_ReturnsSuccess()
        {
            var result = OperationResult.Ok();
            Assert.True(result.Success);
        }

        [Fact]
        public void OperationResult_Fail_ReturnsError()
        {
            var result = OperationResult.Fail("test error");
            Assert.False(result.Success);
            Assert.Equal("test error", result.Error);
        }
    }
}
