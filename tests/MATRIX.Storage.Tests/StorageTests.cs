using Xunit;
using MATRIX.Storage;
using MATRIX.Core;

namespace MATRIX.Storage.Tests
{
    public class StorageTests
    {
        [Fact]
        public void StorageConfig_Default_SetsCorrectPaths()
        {
            var config = StorageConfig.Default("/tmp/test-matrix");
            Assert.Contains("catalog.json", config.CatalogFile);
            Assert.Contains("state.json", config.StateFile);
            Assert.Contains("CURRENT", config.CurrentFile);
            Assert.Contains("generations", config.GenerationsDir);
        }
    }
}
