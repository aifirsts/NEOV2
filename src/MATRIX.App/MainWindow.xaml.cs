using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            UpdateGraph();
            _viewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(MainViewModel.SelectedItem)) UpdateGraph(); };
        }

        private void UpdateGraph()
        {
            if (!_viewModel.Session.IsLoaded) return;
            try
            {
                var ws = _viewModel.Session.GetCurrent();
                GraphView.DrawGraph(ws.Catalog.Nodes, ws.Catalog.Edges,
                    _viewModel.SelectedItem?.Entity is MATRIX.Core.Node n ? n.Id : null);
            }
            catch (Exception ex)
            {
                _viewModel.Events.Insert(0, $"[{DateTimeOffset.UtcNow:HH:mm:ss}] Graph error: {ex.Message}");
            }
        }

        private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItemVM item)
            {
                _viewModel.SelectItem(item);
                UpdateGraph();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    _viewModel.Initialize();
                    UpdateGraph();
                    break;
                case Key.S when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                    _viewModel.Session.Save();
                    break;
                case Key.F when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                    {
                        // Focus search box
                        var tb = FindName("SearchBox") as System.Windows.Controls.TextBox;
                        tb?.Focus();
                    }
                    break;
            }
        }

        private void OnExit(object sender, RoutedEventArgs e)
        {
            _viewModel.Shutdown();
            Close();
        }

        private void OnAbout(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "MATRIX V3\nЦентр управления инфраструктурой NEO\n\n" +
                "Версия: 3.0.0\n" +
                "Репозиторий: github.com/aifirsts/NEOV2\n\n" +
                "Продукт NEO. Все права защищены.",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
