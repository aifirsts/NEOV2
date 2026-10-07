using Xunit;
using MATRIX.App.ViewModels;

namespace MATRIX.App.Tests
{
    public class AppTests
    {
        [Fact]
        public void RelayCommand_CanExecute_DefaultTrue()
        {
            var cmd = new RelayCommand(_ => { });
            Assert.True(cmd.CanExecute(null));
        }
    }
}
