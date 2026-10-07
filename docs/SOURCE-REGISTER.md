# SOURCE-REGISTER — MATRIX V2

## Происхождение источников

| Источник | Тип | Использование |
|---|---|---|
| FINAL.zip (475 files) | ZIP архив | Аудит истории V1 (шаги 1-8) |
| FINAL.MD | Markdown | Техническое задание |
| matrix-workspace.schema.json | JSON Schema | Контракт V1 |
| Domain.cs (V1) | C# scaffold | Базовый каркас (16 строк) |

## NEW_IMPLEMENTATION

- src/MATRIX.Core/Domain.cs — полная реализация доменных моделей
- src/MATRIX.Core/SecurityPolicy.cs — ADR-V2-SEC-001
- src/MATRIX.Application/WorkspaceServices.cs — Session, Editor, CRUD
- src/MATRIX.Storage/WorkspaceStore.cs — Generation store
- src/MATRIX.Import/ImportExportService.cs — Import/Export
- src/MATRIX.App/ — WPF UI
- tools/test_matrix_v2.py — Python тесты
