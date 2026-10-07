# CHANGELOG — MATRIX

## [3.0.0] — 2026-10-08

### Added — V3
- Typed edit forms for Node, Project, Task, Control, Evidence, Finding, Runbook, UserStatement
- Graph visualization: Canvas with circular layout, node circles, edge lines, labels
- Search and filter by tree text (name, id, owner)
- Visible/total/hidden counters
- Keyboard navigation: F5 (refresh), Ctrl+S (save), Ctrl+F (search)
- Empty/loading/error/locked states
- Recovery UI: enumerate generations, preview, restore
- Single-file self-contained publish (MATRIX.exe)
- V3 CI: build, test, publish, release in one workflow
- Delete operations for all entities (Node, Project, Task, Control, Evidence, Finding, Runbook, UserStatement)

### Changed — V2→V3
- Directory.Build.props: Version 3.0.0
- MATRIX.App.csproj: PublishSingleFile=true
- CI: single workflow for build, test, publish, release
- MainWindow: added search bar, graph view, edit forms panel
- MainViewModel: added search filter, generation recovery, counts

### Removed
- release-candidate.yml (merged into build-test.yml)

## [2.1.0] — 2026-10-07

### Fixed
- Eliminated reflection hack (SetCurrent + InternalsVisibleTo)
- Full CRUD: Update+Delete for all 10 entities
- Supersedes multi-link cycle detection (DFS)
- Stale lease TTL (5 minutes)
- schemaVersion validation in Load
- RepoUrl validation (HTTPS github.com)
- ID uniqueness for Finding/Runbook/UserStatement/AuditEvent
- Audit trail on CRUD operations
- RC CI workflow: only MATRIX.exe + VERSION.json + SHA256SUMS.txt
- Stale ws after AddNode fixed

## [2.0.0-preview.1] — 2026-10-07

### Added
- Full domain models (462 lines)
- SecurityPolicy (ADR-V2-SEC-001)
- GraphValidator
- WorkspaceStore (generations, SHA-256, lease)
- ImportExportService
- WPF UI (MVVM)
- 50 Python tests
- 3 JSON schemas, 2 fixtures
- CI workflows
