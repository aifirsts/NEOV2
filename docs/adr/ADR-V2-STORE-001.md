# ADR-V2-STORE-001: Generation-based Storage

Дата: 2026-10-07
Статус: Принято

## Решение

Каждая запись создаёт immutable generation snapshot. CURRENT pointer указывает на активную. SHA-256 для integrity.
