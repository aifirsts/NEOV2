using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using MATRIX.Application;
using MATRIX.Storage;
using MATRIX.Import;
using MATRIX.Core;

namespace MATRIX.App.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly WorkspaceStore _store;
        private readonly WorkspaceSession _session;
        private readonly WorkspaceEditor _editor;
        private readonly ImportExportService _importExport;

        private TreeViewItemVM? _selectedItem;
        private string _selectedNodeSummary = string.Empty;
        private object? _selectedDetail;
        private bool _modeInfrastructure = true;
        private bool _modeProjects = true;
        private bool _modeSecurity = false;
        private bool _modeRecovery = false;

        public ObservableCollection<TreeViewItemVM> TreeRoot { get; } = new();
        public ObservableCollection<string> Events { get; } = new();

        public TreeViewItemVM? SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        public string SelectedNodeSummary
        {
            get => _selectedNodeSummary;
            set { _selectedNodeSummary = value; OnPropertyChanged(); }
        }

        public object? SelectedDetail
        {
            get => _selectedDetail;
            set { _selectedDetail = value; OnPropertyChanged(); }
        }

        public bool ModeInfrastructure { get => _modeInfrastructure; set { _modeInfrastructure = value; OnPropertyChanged(); RefreshTree(); } }
        public bool ModeProjects { get => _modeProjects; set { _modeProjects = value; OnPropertyChanged(); RefreshTree(); } }
        public bool ModeSecurity { get => _modeSecurity; set { _modeSecurity = value; OnPropertyChanged(); RefreshTree(); } }
        public bool ModeRecovery { get => _modeRecovery; set { _modeRecovery = value; OnPropertyChanged(); RefreshTree(); } }

        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand AddNodeCommand { get; }
        public ICommand AddProjectCommand { get; }
        public ICommand AddTaskCommand { get; }

        public MainViewModel()
        {
            _store = new WorkspaceStore();
            _session = new WorkspaceSession(_store);
            _editor = new WorkspaceEditor(_session);
            _importExport = new ImportExportService(_store);

            OpenCommand = new RelayCommand(_ => LoadWorkspace());
            SaveCommand = new RelayCommand(_ => SaveWorkspace());
            ImportCommand = new RelayCommand(_ => ImportWorkspace());
            ExportCommand = new RelayCommand(_ => ExportWorkspace());
            AddNodeCommand = new RelayCommand(_ => AddNode());
            AddProjectCommand = new RelayCommand(_ => AddProject());
            AddTaskCommand = new RelayCommand(_ => AddTask());
        }

        public void Initialize()
        {
            LoadWorkspace();
        }

        public void Shutdown()
        {
            if (_session.IsDirty)
                _session.Cancel();
        }

        private void LoadWorkspace()
        {
            var result = _session.Load();
            if (result.Success)
            {
                AddEvent("Workspace loaded successfully");
                RefreshTree();
            }
            else
            {
                AddEvent($"Load failed: {result.Error}");
                // Create empty workspace
                var empty = Workspace.Create(DateTimeOffset.UtcNow,
                    Catalog.Create(
                        Array.Empty<Node>(),
                        Array.Empty<Edge>(),
                        Array.Empty<Project>()),
                    State.Create(
                        Array.Empty<Task>(),
                        Array.Empty<Control>(),
                        Array.Empty<Evidence>(),
                        Array.Empty<Finding>(),
                        Array.Empty<Runbook>(),
                        Array.Empty<AuditEvent>(),
                        Array.Empty<UserStatement>()));
                // Set via internal method — no reflection
                _session.SetCurrent(empty);
                RefreshTree();
            }
        }

        private void SaveWorkspace()
        {
            var result = _session.Save();
            AddEvent(result.Success ? "Saved successfully" : $"Save failed: {result.Error}");
        }

        private void ImportWorkspace()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "MATRIX Workspace (*.json)|*.json",
                Title = "Импорт workspace"
            };
            if (dialog.ShowDialog() == true)
            {
                var preview = _importExport.PreviewImport(dialog.FileName);
                if (preview.Success)
                {
                    AddEvent($"Import preview: {preview.Value!.NodeCount} nodes, {preview.Value.EdgeCount} edges, {preview.Value.ProjectCount} projects");
                    var msg = $"Импорт заменит текущие данные.\n\n" +
                             $"SHA-256: {preview.Value.Sha256}\n" +
                             $"Узлов: {preview.Value.NodeCount}\n" +
                             $"Рёбер: {preview.Value.EdgeCount}\n" +
                             $"Проектов: {preview.Value.ProjectCount}\n\n" +
                             $"Подтверждаете импорт?";
                    if (System.Windows.MessageBox.Show(msg, "Подтверждение импорта",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
                    {
                        var commit = _importExport.CommitImport(dialog.FileName, preview.Value.Sha256);
                        AddEvent(commit.Success ? "Import committed" : $"Import failed: {commit.Error}");
                        LoadWorkspace();
                    }
                }
                else
                {
                    AddEvent($"Import preview failed: {preview.Error}");
                }
            }
        }

        private void ExportWorkspace()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "MATRIX Workspace (*.json)|*.json",
                Title = "Экспорт workspace",
                FileName = $"matrix-export-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var ws = _session.GetCurrent();
                    var result = _importExport.Export(dialog.FileName, ws);
                    AddEvent(result.Success ? $"Exported to {dialog.FileName}" : $"Export failed: {result.Error}");
                }
                catch (Exception ex)
                {
                    AddEvent($"Export error: {ex.Message}");
                }
            }
        }

        private void AddNode()
        {
            // Simplified: add a demo node
            try
            {
                var node = Node.Create(
                    $"node-{Guid.NewGuid().ToString("N")[..8]}",
                    NodeType.Service,
                    "Новый ресурс",
                    "NEO",
                    "LOCAL",
                    Sensitivity.Internal,
                    Availability.Unknown);
                var result = _editor.AddNode(node);
                AddEvent(result.Success ? $"Added node {node.Id}" : $"AddNode: {result.Error}");
                RefreshTree();
            }
            catch (Exception ex) { AddEvent($"AddNode error: {ex.Message}"); }
        }

        private void AddProject()
        {
            try
            {
                var ws = _session.GetCurrent();
                var projectNode = ws.Catalog.Nodes.FirstOrDefault(n => n.Type == NodeType.Project);
                if (projectNode == null)
                {
                    // Create project node first
                    var node = Node.Create(
                        $"project-{Guid.NewGuid().ToString("N")[..8]}",
                        NodeType.Project,
                        "Новый проект",
                        "NEO",
                        "LOCAL",
                        Sensitivity.Internal,
                        Availability.Unknown);
                    _editor.AddNode(node);
                    // Get fresh workspace after AddNode — ws is stale
                    ws = _session.GetCurrent();
                    projectNode = ws.Catalog.Nodes.FirstOrDefault(n => n.Type == NodeType.Project);
                    if (projectNode == null)
                    {
                        AddEvent("Failed to create project node");
                        return;
                    }
                }

                var project = Project.Create(
                    $"proj-{Guid.NewGuid().ToString("N")[..8]}",
                    projectNode.Id,
                    "Описание цели проекта",
                    "https://github.com/aifirsts/NEOV2",
                    "DESIGN",
                    "Следующее действие",
                    new[] { "Milestone 1", "Milestone 2" });
                var result = _editor.AddProject(project);
                AddEvent(result.Success ? $"Added project {project.Id}" : $"AddProject: {result.Error}");
                RefreshTree();
            }
            catch (Exception ex) { AddEvent($"AddProject error: {ex.Message}"); }
        }

        private void AddTask()
        {
            try
            {
                var ws = _session.GetCurrent();
                var project = ws.Catalog.Projects.FirstOrDefault();
                if (project == null)
                {
                    AddEvent("No project available to add task");
                    return;
                }
                var task = Task.Create(
                    $"task-{Guid.NewGuid().ToString("N")[..8]}",
                    project.Id,
                    "Новая задача",
                    TaskStatus.Todo,
                    TaskPriority.Medium,
                    Array.Empty<string>());
                var result = _editor.AddTask(task);
                AddEvent(result.Success ? $"Added task {task.Id}" : $"AddTask: {result.Error}");
                RefreshTree();
            }
            catch (Exception ex) { AddEvent($"AddTask error: {ex.Message}"); }
        }

        public void SelectItem(TreeViewItemVM item)
        {
            SelectedItem = item;
            SelectedNodeSummary = item.DisplayName;
            SelectedDetail = item.Entity;
        }

        private void RefreshTree()
        {
            TreeRoot.Clear();
            try
            {
                if (!_session.IsLoaded) return;
                var ws = _session.GetCurrent();

                if (ModeInfrastructure || ModeProjects)
                {
                    var infraNode = new TreeViewItemVM("Инфраструктура", "Каталог", null);
                    foreach (var node in ws.Catalog.Nodes)
                        infraNode.Children.Add(new TreeViewItemVM(
                            $"{node.Name} [{node.Type}]", node.Id, node));
                    TreeRoot.Add(infraNode);
                }

                if (ModeProjects)
                {
                    var projNode = new TreeViewItemVM("Проекты", "Проекты", null);
                    foreach (var proj in ws.Catalog.Projects)
                        projNode.Children.Add(new TreeViewItemVM(proj.Goal, proj.Id, proj));
                    TreeRoot.Add(projNode);
                }

                if (ModeSecurity)
                {
                    var secNode = new TreeViewItemVM("Безопасность", "Безопасность", null);
                    foreach (var ctrl in ws.State.Controls)
                        secNode.Children.Add(new TreeViewItemVM(ctrl.Criterion, ctrl.Id, ctrl));
                    TreeRoot.Add(secNode);
                }

                if (ModeRecovery)
                {
                    var recNode = new TreeViewItemVM("Восстановление", "Восстановление", null);
                    foreach (var rb in ws.State.Runbooks)
                        recNode.Children.Add(new TreeViewItemVM(rb.Name, rb.Id, rb));
                    TreeRoot.Add(recNode);
                }
            }
            catch (Exception ex)
            {
                AddEvent($"Refresh error: {ex.Message}");
            }
        }

        private void AddEvent(string message)
        {
            Events.Insert(0, $"[{DateTimeOffset.UtcNow:HH:mm:ss}] {message}");
            if (Events.Count > 200)
                Events.RemoveAt(Events.Count - 1);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class TreeViewItemVM
    {
        public string DisplayName { get; }
        public string Id { get; }
        public object? Entity { get; }
        public ObservableCollection<TreeViewItemVM> Children { get; } = new();

        public TreeViewItemVM(string displayName, string id, object? entity)
        {
            DisplayName = displayName;
            Id = id;
            Entity = entity;
        }
    }

    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
