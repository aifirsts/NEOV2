# README — MATRIX V3

**Локальный Windows x64 центр управления инфраструктурой и проектами NEO.**

## Статус

- Версия: 3.0.0
- Статус: Финальная версия. Self-contained EXE. CI green.
- Репозиторий: [github.com/aifirsts/NEOV2](https://github.com/aifirsts/NEOV2)
- Release: [v3.0.0](https://github.com/aifirsts/NEOV2/releases/tag/v3.0.0)

## Что нового в V3

- Typed формы редактирования для всех сущностей
- Графическая визуализация связей (Canvas)
- Поиск и фильтры по дереву
- Счётчики visible/total
- Keyboard navigation (F5, Ctrl+S, Ctrl+F)
- Recovery UI: enumerate generations, preview, restore
- Single-file self-contained EXE
- Delete operations для всех сущностей

## Сборка

```powershell
dotnet build MATRIX.sln -c Release
dotnet test MATRIX.sln -c Release
dotnet publish src/MATRIX.App/MATRIX.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Python тесты

```bash
python tools/test_matrix_v2.py
```

## Хранение данных

- Путь: `%LOCALAPPDATA%\MATRIX`
- Файлы: `catalog.json`, `state.json`, `CURRENT`, `generations/`
- Recovery: enumerate → preview → restore

## Архитектура

| Проект | Назначение |
|---|---|
| MATRIX.Core | Доменные модели, SecurityPolicy, GraphValidator |
| MATRIX.Application | WorkspaceSession, WorkspaceEditor (CRUD), Audit trail |
| MATRIX.Storage | Generation store, SHA-256, cooperative lease, stale TTL |
| MATRIX.Import | Import/Export: preview → confirmation → commit |
| MATRIX.App | WPF UI: tree, graph, forms, card, events, recovery |

## Документация

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/V1-V2-CHANGE-MATRIX.md](docs/V1-V2-CHANGE-MATRIX.md)
- [audit/AUDIT-REPORT-V2.1-FINAL.md](audit/AUDIT-REPORT-V2.1-FINAL.md)
- [audit/FINAL-REPORT-V3.md](audit/FINAL-REPORT-V3.md)

## Лицензия

Copyright © NEO 2026. Все права защищены.
