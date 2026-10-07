using Xunit;
using MATRIX.Import;
using MATRIX.Storage;
using MATRIX.Core;

namespace MATRIX.Import.Tests
{
    public class ImportTests
    {
        [Fact]
        public void ExportOptions_Default_IncludesAuditEvents()
        {
            var opts = new ExportOptions();
            Assert.True(opts.IncludeAuditEvents);
            Assert.True(opts.IncludeUserStatements);
        }
    }
}
