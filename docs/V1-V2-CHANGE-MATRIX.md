# V1-V2-CHANGE-MATRIX

| # | V1 Базовый факт | V1 Дефект | V2 Изменение | Тест | Остаточная проверка |
|---|---|---|---|---|---|
| 1 | Domain.cs 16 lines | Нет моделей | Полная реализация 462 строки | T01-T06, T22 | Windows build |
| 2 | Нет SecurityPolicy | UNKNOWN=PASS | ADR-V2-SEC-001 | T07-T09 | Runtime verification |
| 3 | Нет GraphValidator | Dangling/cycles | Full validator | T01, T05, T06 | Integration tests |
| 4 | Нет storage | No generations/hash | Generation store + SHA-256 + lease | T14-T18 | Windows file system |
| 5 | Нет import/export | No preview/commit | Replace-only import + safe export | T10, T12, T13 | Manual import test |
| 6 | Нет UI | Scaffold only | WPF MVVM UI | T20-T21 | Runtime UI test |
| 7 | Нет CI | No automation | 2 GitHub Actions workflows | Workflow files | CI execution |
| 8 | Нет tests | No regression | 49 Python tests | test_matrix_v2.py | dotnet test on Windows |
| 9 | 1 schema | No validation | 3 JSON schemas | Schema validation | — |
| 10 | 0 fixtures | No test data | 2 workspace fixtures | Fixture tests | — |
