# ARCHITECTURE — MATRIX V2

## Обзор

MATRIX V2 — настольное приложение Windows x64 (C#/WPF/MVVM) для управления инфраструктурой NEO.

## Слои

- MATRIX.Core — доменные модели, SecurityPolicy, GraphValidator. Без WPF/сети/файлов.
- MATRIX.Application — WorkspaceSession, WorkspaceEditor (CRUD), OperationResult.
- MATRIX.Storage — file-based store с generations, CURRENT pointer, SHA-256, cooperative lease.
- MATRIX.Import — Import/Export: preview → confirmation → exact bytes → commit.
- MATRIX.App — WPF UI: дерево, граф, карточка, события.

## Зависимости

- Core → (нет внешних зависимостей)
- Application → Core
- Storage → Core, System.Text.Json
- Import → Core, Storage
- App → Core, Application, Storage, Import, WPF

## ADR

- ADR-V2-SEC-001: Conservative security computation
- ADR-V2-STORE-001: Generation-based storage
- ADR-V2-SDK-001: .NET 8 LTS
- ADR-V2-ARCH-001: Layered architecture
