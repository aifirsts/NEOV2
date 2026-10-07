# CHANGELOG — MATRIX V2

## [2.0.0-preview.1] — 2026-10-07

### Added
- Полная реализация доменных моделей (Node, Edge, Project, Task, Control, Evidence, Finding, Runbook, AuditEvent, UserStatement)
- SecurityPolicy с conservative computation (ADR-V2-SEC-001)
- GraphValidator: ID uniqueness, dangling edges, duplicate edges, task DAG cycles
- WorkspaceStore с generations, CURRENT pointer, SHA-256, cooperative lease
- ImportExportService: preview → confirmation → exact bytes → commit
- WPF UI: дерево слева, граф в центре, карточка справа, события снизу
- 49 Python тестов (T01-T28 + schema/fixture validation)
- JSON schemas: matrix-workspace-v1, execution-status, release-evidence
- Фикстуры: empty-workspace, sample-workspace
- CI: build-test.yml, release-candidate.yml
- Полная документация: ARCHITECTURE, DATA-MODEL, STORAGE-PROTOCOL, etc.

### Changed
- V1 Domain.cs (16-line scaffold) → полная реализация 462 строк
- V1 workspace schema → сохранена совместимость (schema v1)
- V1 storage → generations + CURRENT pointer + SHA-256 + cooperative lease
- V1 import/export → replace-only с preview + confirmation

### PASS-F
- Windows build/EXE: требует Windows среду с .NET 8 SDK
- CI: требует push в GitHub для активации
- NEO acceptance: требует личной проверки на ПК NEO
