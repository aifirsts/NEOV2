# CONTEXT — MATRIX V2

Версия: 2.0. Дата: 7 октября 2026. Владелец: NEO.

## Цель

Локальный визуальный центр управления инфраструктурой и проектами NEO. Пользователь понимает ресурсы, связи, риски, актуальность доказательств, следующие задачи и восстановление.

## Продукт

Windows x64; C#/WPF/MVVM; .NET 8. Self-contained win-x64. Основной артефакт — `MATRIX.exe`.

## Архитектура

- `MATRIX.Core` — доменные модели, SecurityPolicy, GraphValidator. Не зависит от WPF, сети, файловой системы.
- `MATRIX.Application` — WorkspaceSession, WorkspaceEditor (CRUD), OperationResult.
- `MATRIX.Storage` — file-based store с generations, CURRENT pointer, SHA-256, cooperative lease.
- `MATRIX.Import` — Import/Export: preview → confirmation → exact bytes → commit.
- `MATRIX.App` — WPF UI: дерево, граф, карточка, события.

## Модель данных

Node, Edge, Project, Task, Control, Evidence, Finding, Runbook, AuditEvent, UserStatement.

## Безопасность

- UNKNOWN/STALE/MISSING/пустой набор и UserStatement не дают PASS.
- N/A требует rationale и не служит скрытым PASS.
- ADR-V2-SEC-001: conservative security computation.

## Ограничения

Нет секретов, network connectors, SSH, телеметрии, auto-update, admin, изменения ОС/VPS, платежей, plugins и localhost API. MATRIX меняет собственные данные, не инфраструктуру.

## Хранение

`%LOCALAPPDATA%\MATRIX`: catalog.json, state.json, settings.json, generations/. Protocol: write→flush→reread/validate→publish. Cooperative lease. SHA-256 integrity.

## Поставка

Self-contained win-x64. Данные в `%LOCALAPPDATA%\MATRIX`, отдельно от EXE.
