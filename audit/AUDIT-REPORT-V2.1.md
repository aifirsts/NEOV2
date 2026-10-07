# AUDIT-REPORT-V2.1 — Глубокий аудит и исправления

**Дата:** 7 октября 2026
**Commit:** a7e4f4c
**Статус:** COMPLETED_WITH_PASS_F

## Найденные проблемы и исправления

| ID | Severity | Область | Описание | Исправление | Статус |
|---|---|---|---|---|---|
| A-01 | CRITICAL | Application | Reflection hack `GetType().GetField("_current")?.SetValue()` в WorkspaceEditor и MainViewModel | Добавлен `internal void SetCurrent(Workspace ws)` + `InternalsVisibleTo("MATRIX")` | PASS |
| A-02 | HIGH | Application | Неполный CRUD: нет Update, нет Delete для Task/Control/Evidence/Finding/Runbook/UserStatement | Добавлены Update+Delete для всех 10 сущностей (60+ новых методов) | PASS |
| A-03 | HIGH | Core | Supersedes cycle detection проверял только первую ссылку | Переписан на DFS через Stack — проверяет ВСЕ supersedes ссылки | PASS |
| A-04 | HIGH | Storage | Stale lease: при краше процесса lease блокирует все будущие записи | Добавлен TTL (5 минут) — stale leases автоматически удаляются | PASS |
| A-05 | HIGH | Storage | Нет валидации schemaVersion в WorkspaceStore.Load | Добавлена проверка schemaVersion == 1 перед десериализацией | PASS |
| A-06 | MEDIUM | Core | Project.Create не валидировал RepoUrl | Добавлена проверка: только HTTPS github.com URL или null | PASS |
| A-07 | MEDIUM | Core | Нет проверки уникальности Finding/Runbook/UserStatement/AuditEvent IDs | Добавлены проверки в ValidateState | PASS |
| A-08 | MEDIUM | Application | Нет audit trail на CRUD операциях | Добавлено создание AuditEvent для каждой Add/Update/Delete операции | PASS |
| A-09 | MEDIUM | CI | Release Candidate загружал все файлы включая .resources.dll | Исправлен files list: только MATRIX.exe, VERSION.json, SHA256SUMS.txt | PASS |
| A-10 | MEDIUM | App | MainViewModel.AddProject использовал stale `ws` после AddNode | Добавлено `ws = _session.GetCurrent()` после AddNode | PASS |
| A-11 | LOW | App | MainViewModel.LoadWorkspace использовал reflection для empty workspace | Заменён на `_session.SetCurrent(empty)` | PASS |
| A-12 | LOW | Tests | Тестовое покрытие было минимальным (16 тестов) | Добавлены: Project_Create_InvalidRepoUrl, Project_Create_NullRepoUrl, Node_Create_IdTooLong, Node_Create_InvalidChars, SecurityPolicy_Compute_Revoked/Expired/Fail/Pass, GraphValidator DuplicateFinding/Runbook/AuditEvent, SecurityPolicy_ValidateSupersedes_MultiLinkCycle | PASS |

## CI Verification

- Build and Test: PASSED на windows-latest
- Все C# тесты: PASSED
- Python тесты: 50/50 PASSED
- Release: https://github.com/aifirsts/NEOV2/releases/tag/v2.0.0-preview.1

## Оставшиеся PASS-F

| Действие | Почему PASS-F | Инструкция |
|---|---|---|
| EXE на ПК NEO | Требует Windows с .NET 8 SDK | `dotnet publish` или скачать из Release |
| Приёмка UI | Требует интерактивного запуска | Запустить MATRIX.exe, проверить CRUD |
| V1→V2 миграция | Требует данные NEO | Экспорт V1 → импорт в V2 |

## Результат

Все найденные ошибки исправлены. CI green. Репозиторий содержит полный source tree с 50+ Python тестами и 16+ C# тестами. PROMPT_V3_PC.MD создан для следующей сессии.
