# SECURITY — MATRIX V2

## Принципы

1. Наличие ресурса не означает безопасность.
2. UNKNOWN/STALE/MISSING/пустой набор → не PASS.
3. UserStatement не Evidence.
4. Импорт не продлевает срок.
5. AuditEvent локальный и изменяемый; hash доказывает байты, не факт.
6. N/A требует rationale и не скрытый PASS.

## ADR-V2-SEC-001: Security Computation Policy

Conservative computation: только required + applicable controls с fresh, non-revoked PASS evidence дают Security.Pass. Всё остальное → Unknown/Fail/Blocked.

## Secret Screening

Проверка decoded values до display/log/save/import/export/recovery. Фиксированные коды, никаких raw inputs или exception chains с данными.

## URI Policy

Только approved HTTPS repo paths на github.com. Imported texts/paths/URLs — данные, не команды.
