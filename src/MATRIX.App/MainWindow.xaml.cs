using System.Windows;
using System.Windows.Controls;
using MATRIX.App.ViewModels;

namespace MATRIX.App
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            _viewModel.Initialize();
        }

        private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItemVM item)
                _viewModel.SelectItem(item);
        }

        private void OnExit(object sender, RoutedEventArgs e)
        {
            _viewModel.Shutdown();
            Close();
        }

        private void OnAbout(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "MATRIX V2\nЦентр управления инфраструктурой NEO\n\n" +
                "Версия: 2.0.0\n" +
                "Репозиторий: github.com/aifirsts/NEOV2\n\n" +
                "Продукт NEO. Все права защищены.",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
