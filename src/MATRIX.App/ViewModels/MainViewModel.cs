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
        private string _searchText = string.Empty;

        // Node editor fields
        private string? _selectedNodeType;
        private string? _selectedSensitivity;
        private string? _selectedAvailability;
        private string? _selectedTaskStatus;
        private string? _selectedPriority;

        // Recovery
        private Generation? _selectedGeneration;
        private ObservableCollection<Generation> _availableGenerations = new();

        public ObservableCollection<TreeViewItemVM> TreeRoot { get; } = new();
        public ObservableCollection<string> Events { get; } = new();

        public WorkspaceSession Session => _session;

        // Node editor bindings
        public ObservableCollection<string> NodeTypes { get; } = new() { "device", "service", "account", "vps", "project", "agent" };
        public ObservableCollection<string> SensitivityTypes { get; } = new() { "PUBLIC", "INTERNAL", "SENSITIVE" };
        public ObservableCollection<string> AvailabilityTypes { get; } = new() { "UNKNOWN", "UP", "DOWN" };
        public ObservableCollection<string> TaskStatusTypes { get; } = new() { "TODO", "IN_PROGRESS", "BLOCKED", "DONE" };
        public ObservableCollection<string> PriorityTypes { get; } = new() { "LOW", "MEDIUM", "HIGH", "CRITICAL" };

        // Properties
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

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); OnPropertyChanged(nameof(TreeCounts)); RefreshTree(); }
        }

        public string? SelectedNodeType { get => _selectedNodeType; set { _selectedNodeType = value; OnPropertyChanged(); } }
        public string? SelectedSensitivity { get => _selectedSensitivity; set { _selectedSensitivity = value; OnPropertyChanged(); } }
        public string? SelectedAvailability { get => _selectedAvailability; set { _selectedAvailability = value; OnPropertyChanged(); } }
        public string? SelectedTaskStatus { get => _selectedTaskStatus; set { _selectedTaskStatus = value; OnPropertyChanged(); } }
        public string? SelectedPriority { get => _selectedPriority; set { _selectedPriority = value; OnPropertyChanged(); } }

        public ObservableCollection<Generation> AvailableGenerations
        {
            get => _availableGenerations;
            set { _availableGenerations = value; OnPropertyChanged(); }
        }

        public Generation? SelectedGeneration
        {
            get => _selectedGeneration;
            set { _selectedGeneration = value; OnPropertyChanged(); }
        }

        public string TreeCounts
        {
            get
            {
                var ws = SafeGetCurrent();
                if (ws == null) return "0/0";
                var total = ws.Catalog.Nodes.Count;
                var visible = string.IsNullOrEmpty(_searchText)
                    ? total
                    : ws.Catalog.Nodes.Count(n => n.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
                return $"Видно: {visible} / Всего: {total}";
            }
        }

        public WorkspaceSession Session => _session;

        // Commands
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand AddNodeCommand { get; }
        public ICommand DeleteNodeCommand { get; }
        public ICommand AddProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand RefreshGenerationsCommand { get; }
        public ICommand RestoreGenerationCommand { get; }
        public ICommand PreviewGenerationCommand { get; }

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
            AddNodeCommand = new RelayCommand(_ => AddNodeFromForm());
            DeleteNodeCommand = new RelayCommand(_ => DeleteSelectedNode());
            AddProjectCommand = new RelayCommand(_ => AddProject());
            DeleteProjectCommand = new RelayCommand(_ => DeleteSelectedProject());
            AddTaskCommand = new RelayCommand(_ => AddTask());
            DeleteTaskCommand = new RelayCommand(_ => DeleteSelectedTask());
            ClearSearchCommand = new RelayCommand(_ => { SearchText = string.Empty; });
            RefreshGenerationsCommand = new RelayCommand(_ => RefreshGenerations());
            RestoreGenerationCommand = new RelayCommand(_ => RestoreGeneration());
            PreviewGenerationCommand = new RelayCommand(_ => PreviewGeneration());
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

        private Workspace? SafeGetCurrent()
        {
            try { return _session.IsLoaded ? _session.GetCurrent() : null; }
            catch { return null; }
        }

        private void LoadWorkspace()
        {
            var result = _session.Load();
            if (result.Success)
            {
                AddEvent("Workspace loaded successfully");
                RefreshTree();
                RefreshGenerations();
            }
            else
            {
                AddEvent($"Load failed: {result.Error}");
                var empty = Workspace.Create(DateTimeOffset.UtcNow,
                    Catalog.Create(Array.Empty<Node>(), Array.Empty<Edge>(), Array.Empty<Project>()),
                    State.Create(Array.Empty<Task>(), Array.Empty<Control>(), Array.Empty<Evidence>(),
                        Array.Empty<Finding>(), Array.Empty<Runbook>(), Array.Empty<AuditEvent>(),
                        Array.Empty<UserStatement>()));
                _session.SetCurrent(empty);
                RefreshTree();
            }
        }

        private void SaveWorkspace()
        {
            var result = _session.Save();
            AddEvent(result.Success ? "Saved successfully" : $"Save failed: {result.Error}");
            if (result.Success) RefreshGenerations();
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
                    AddEvent($"Import preview: {preview.Value!.NodeCount} nodes, {preview.Value.EdgeCount} edges");
                    var msg = $"Импорт заменит текущие данные.\n\n" +
                             $"SHA-256: {preview.Value.Sha256}\n" +
                             $"Узлов: {preview.Value.NodeCount}\n" +
                             $"Рёбер: {preview.Value.EdgeCount}\n\n" +
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
                catch (Exception ex) { AddEvent($"Export error: {ex.Message}"); }
            }
        }

        private void AddNodeFromForm()
        {
            try
            {
                var id = $"node-{Guid.NewGuid():N}"[..14];
                var name = "Новый ресурс";
                var nodeType = NodeType.Service;
                var sensitivity = Sensitivity.Internal;
                var availability = Availability.Unknown;

                if (_selectedNodeType != null)
                {
                    if (_selectedNodeType == "device") nodeType = NodeType.Device;
                    else if (_selectedNodeType == "service") nodeType = NodeType.Service;
                    else if (_selectedNodeType == "account") nodeType = NodeType.Account;
                    else if (_selectedNodeType == "vps") nodeType = NodeType.Vps;
                    else if (_selectedNodeType == "project") nodeType = NodeType.Project;
                    else if (_selectedNodeType == "agent") nodeType = NodeType.Agent;
                }

                if (_selectedSensitivity != null)
                {
                    sensitivity = _selectedSensitivity == "PUBLIC" ? Sensitivity.Public :
                                  _selectedSensitivity == "SENSITIVE" ? Sensitivity.Sensitive : Sensitivity.Internal;
                }

                if (_selectedAvailability != null)
                {
                    availability = _selectedAvailability == "UP" ? Availability.Up :
                                  _selectedAvailability == "DOWN" ? Availability.Down : Availability.Unknown;
                }

                var node = Node.Create(id, nodeType, name, "NEO", "LOCAL", sensitivity, availability);
                var result = _editor.AddNode(node);
                AddEvent(result.Success ? $"Added node {node.Id}" : $"AddNode: {result.Error}");
                RefreshTree();
            }
            catch (Exception ex) { AddEvent($"AddNode error: {ex.Message}"); }
        }

        private void DeleteSelectedNode()
        {
            if (_selectedItem?.Id != null && _selectedItem.Entity is Node node)
            {
                var result = _editor.DeleteNode(node.Id);
                AddEvent(result.Success ? $"Deleted node {node.Id}" : $"DeleteNode: {result.Error}");
                RefreshTree();
            }
            else
            {
                AddEvent("Select a node to delete");
            }
        }

        private void AddProject()
        {
            try
            {
                var ws = _session.GetCurrent();
                var projectNode = ws.Catalog.Nodes.FirstOrDefault(n => n.Type == NodeType.Project);
                if (projectNode == null)
                {
                    var node = Node.Create($"project-{Guid.NewGuid():N}"[..16], NodeType.Project, "Новый проект",
                        "NEO", "LOCAL", Sensitivity.Internal, Availability.Unknown);
                    _editor.AddNode(node);
                    ws = _session.GetCurrent();
                    projectNode = ws.Catalog.Nodes.FirstOrDefault(n => n.Type == NodeType.Project);
                    if (projectNode == null) { AddEvent("Failed to create project node"); return; }
                }

                var project = Project.Create($"proj-{Guid.NewGuid():N}"[..14], projectNode.Id,
                    "Описание цели", "https://github.com/aifirsts/NEOV2", "DESIGN", "Следующее действие",
                    new[] { "Milestone 1" });
                var result = _editor.AddProject(project);
                AddEvent(result.Success ? $"Added project {project.Id}" : $"AddProject: {result.Error}");
                RefreshTree();
            }
            catch (Exception ex) { AddEvent($"AddProject error: {ex.Message}"); }
        }

        private void DeleteSelectedProject()
        {
            if (_selectedItem?.Entity is Project project)
            {
                var result = _editor.DeleteProject(project.Id);
                AddEvent(result.Success ? $"Deleted project {project.Id}" : $"DeleteProject: {result.Error}");
                RefreshTree();
            }
        }

        private void AddTask()
        {
            try
            {
                var ws = _session.GetCurrent();
                var project = ws.Catalog.Projects.FirstOrDefault();
                if (project == null) { AddEvent("No project available"); return; }

                var status = TaskStatus.Todo;
                var priority = TaskPriority.Medium;
                if (_selectedTaskStatus != null)
                {
                    status = SecurityPolicy.ParseEnum<TaskStatus>(_selectedTaskStatus);
                }
                if (_selectedPriority != null)
                {
                    priority = SecurityPolicy.ParseEnum<TaskPriority>(_selectedPriority);
                }

                var task = Task.Create($"task-{Guid.NewGuid():N}"[..14], project.Id, "Новая задача",
                    status, priority, Array.Empty<string>());
                var result = _editor.AddTask(task);
                AddEvent(result.Success ? $"Added task {task.Id}" : $"AddTask: {result.Error}");
                RefreshTree();
            }
            catch (Exception ex) { AddEvent($"AddTask error: {ex.Message}"); }
        }

        private void DeleteSelectedTask()
        {
            if (_selectedItem?.Entity is Task task)
            {
                var result = _editor.DeleteTask(task.Id);
                AddEvent(result.Success ? $"Deleted task {task.Id}" : $"DeleteTask: {result.Error}");
                RefreshTree();
            }
        }

        // Recovery
        private void RefreshGenerations()
        {
            _availableGenerations.Clear();
            try
            {
                var config = StorageConfig.Default();
                if (System.IO.Directory.Exists(config.GenerationsDir))
                {
                    var dirs = System.IO.Directory.GetDirectories(config.GenerationsDir)
                        .OrderByDescending(d => d)
                        .Take(20);
                    foreach (var dir in dirs)
                    {
                        var metaPath = System.IO.Path.Combine(dir, "meta.json");
                        if (System.IO.File.Exists(metaPath))
                        {
                            var meta = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(metaPath));
                            var gen = new Generation
                            {
                                Id = meta.RootElement.GetProperty("id").GetString() ?? "",
                                Revision = meta.RootElement.GetProperty("revision").GetString() ?? "",
                                CatalogSha256 = meta.RootElement.GetProperty("catalogSha256").GetString() ?? "",
                                StateSha256 = meta.RootElement.GetProperty("stateSha256").GetString() ?? "",
                                CreatedAt = DateTimeOffset.Parse(meta.RootElement.GetProperty("createdAt").GetString()),
                                CatalogPath = meta.RootElement.GetProperty("catalogPath").GetString() ?? "",
                                StatePath = meta.RootElement.GetProperty("statePath").GetString() ?? ""
                            };
                            _availableGenerations.Add(gen);
                        }
                    }
                    AddEvent($"Found {_availableGenerations.Count} generations");
                }
            }
            catch (Exception ex) { AddEvent($"Refresh generations error: {ex.Message}"); }
        }

        private void RestoreGeneration()
        {
            if (_selectedGeneration == null)
            {
                AddEvent("Select a generation to restore");
                return;
            }
            var gen = _selectedGeneration;
            var msg = $"Восстановить generation {gen.Id}?\n" +
                     $"Каталог SHA-256: {gen.CatalogSha256[..16]}...\n" +
                     $"Состояние SHA-256: {gen.StateSha256[..16]}...\n" +
                     $"Создан: {gen.CreatedAt:yyyy-MM-dd HH:mm:ss}";
            if (System.Windows.MessageBox.Show(msg, "Восстановление",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    var config = StorageConfig.Default();
                    var catalog = System.IO.File.ReadAllText(gen.CatalogPath);
                    var state = System.IO.File.ReadAllText(gen.StatePath);

                    // Write to active files
                    System.IO.File.WriteAllText(config.CatalogFile, catalog);
                    System.IO.File.WriteAllText(config.StateFile, state);
                    System.IO.File.WriteAllText(config.CurrentFile, $"{gen.Revision}\n{gen.CatalogSha256}\n{gen.StateSha256}\n{DateTimeOffset.UtcNow:O}");

                    AddEvent($"Restored generation {gen.Id}");
                    LoadWorkspace();
                }
                catch (Exception ex) { AddEvent($"Restore error: {ex.Message}"); }
            }
        }

        private void PreviewGeneration()
        {
            if (_selectedGeneration == null)
            {
                AddEvent("Select a generation to preview");
                return;
            }
            var gen = _selectedGeneration;
            AddEvent($"Preview: {gen.Id} | Created: {gen.CreatedAt:yyyy-MM-dd HH:mm} | Catalog: {gen.CatalogSha256[..16]}...");
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
                var searchFilter = _searchText;

                bool NodeMatches(Node n) =>
                    string.IsNullOrEmpty(searchFilter) ||
                    n.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    n.Id.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    n.Owner.Contains(searchFilter, StringComparison.OrdinalIgnoreCase);

                var filteredNodes = ws.Catalog.Nodes.Where(NodeMatches).ToList();

                if (ModeInfrastructure || ModeProjects)
                {
                    var infraNode = new TreeViewItemVM("Инфраструктура", "Каталог", null);
                    foreach (var node in filteredNodes)
                        infraNode.Children.Add(new TreeViewItemVM(
                            $"{node.Name} [{SecurityPolicy.ToJsonString(node.Type)}]", node.Id, node));
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

                OnPropertyChanged(nameof(TreeCounts));
            }
            catch (Exception ex) { AddEvent($"Refresh error: {ex.Message}"); }
        }

        private void AddEvent(string message)
        {
            Events.Insert(0, $"[{DateTimeOffset.UtcNow:HH:mm:ss}] {message}");
            if (Events.Count > 200) Events.RemoveAt(Events.Count - 1);
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
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
