using Xunit;
using MATRIX.Core;

namespace MATRIX.Release.Tools.Tests
{
    public class ReleaseToolsTests
    {
        [Fact]
        public void SecurityPolicy_MaxJsonDepth_Is32()
        {
            Assert.Equal(32, SecurityPolicy.MaxJsonDepth);
        }

        [Fact]
        public void SecurityPolicy_MaxInputBytes_Is16MiB()
        {
            Assert.Equal(16 * 1024 * 1024, SecurityPolicy.MaxInputBytes);
        }
    }
}
