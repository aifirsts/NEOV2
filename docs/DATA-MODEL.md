# DATA-MODEL — MATRIX V2

## Сущности

Node, Edge, Project, Task, Control, Evidence, Finding, Runbook, AuditEvent, UserStatement.

## Node

| Поле | Тип | Обязательный |
|---|---|---|
| id | string | Да |
| type | enum | Да |
| name | string | Да |
| owner | string | Да |
| trustZone | string | Да |
| sensitivity | enum | Да |
| availability | enum | Да |

## Отношения

- Project.nodeId → Node (type=project)
- Task.projectId → Project
- Task.dependencies → Task[] (DAG, no cycles/dupes/self)
- Control.targetId → Node
- Evidence.controlId → Control
- Finding/Runbook/UserStatement.targetId → Node
- Evidence.supersedes → Evidence[] (no cycles/dupes)

## Runtime Limits

- Вход: до 16 MiB
- JSON depth: до 32
- UTC timestamps: max 7 fractional digits
- Строгая целочисленная запись
