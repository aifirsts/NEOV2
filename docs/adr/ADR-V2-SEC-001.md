# ADR-V2-SEC-001: Conservative Security Computation

Дата: 2026-10-07
Статус: Принято

## Контекст

V1 не различал UNKNOWN от PASS.

## Решение

Security.Pass только если:
1. Все required controls для Node — Applicable
2. Каждое control имеет non-revoked Evidence
3. Evidence validUntil > now (fresh)
4. Evidence.Result = PASS
