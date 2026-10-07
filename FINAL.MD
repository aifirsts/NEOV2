# FINAL.MD
# Perplexity Computer — «АРХИТЕКТУРА NEO»: глубокий аудит и MATRIX V1 → V2

Версия задания: 1.0 · Дата: 7 октября 2026 · Владелец: NEO.
Исполнитель: Perplexity Computer, руководитель и оркестратор разработки.
Целевой репозиторий результата: [aifirsts/NEOV2](https://github.com/aifirsts/NEOV2).
Исходный репозиторий MATRIX: [aifirsts/MATRIX](https://github.com/aifirsts/MATRIX), источник для чтения и преемственности, не место новых записей по умолчанию.
Исторический `aifirsts/01` — не целевой репозиторий.

Назначение документа: самостоятельное исполнимое техническое задание, контекст, правила решений, аудит, разработка, проверка, упаковка и загрузка улучшенного V2 в NEOV2. Это не отчёт о выполненной разработке V2 и не готовый бинарный релиз.

## 1. Главная команда Computer

Прочитай этот документ целиком. В рамках одной задачи выполни максимально полный цикл: обнаружение всех источников → глубокий аудит проекта и чата → согласование текущего контракта → восстановление целостного рабочего дерева → исправление дефектов → улучшение V1 до V2 → реальные доступные тесты → Windows build/publish при доступной среде → документация → проверенные пакеты → загрузка в `aifirsts/NEOV2` через доступные разрешённые инструменты → финальный отчёт.

Не заканчивай ответом «вот план». Создавай и исправляй реальные исходники, тесты, scripts, schemas, документацию и артефакты. Не начинай MATRIX заново и не подменяй его демонстрацией. Если полного приложения в источниках нет, реализуй отсутствующее по прочитанному контракту и прямо обозначь `NEW_IMPLEMENTATION`.

Работай как автономный руководитель: самостоятельно выбирай технические решения, порядок, детали UX, способ устранения конфликтов и распределение задач. Не запрашивай косметических согласований и не жди отдельной команды на каждый этап. Специалистов/подагентов запускай, если такая возможность реально доступна; результаты проверяй и интегрируй сам.

Один проход означает одну инициированную задачу с внутренними итерациями, подзадачами и исправлениями до выдачи максимального результата. Это не запрет на повтор теста после исправления и не обещание бесконечного времени, credits или недоступных инструментов. При ограничении среды не останавливай весь проект: используй PASS-F, продолжай независимые и безопасные работы, сохраняй checkpoint и завершай полезной поставкой.

## 2. Новые указания NEO

Следующие изменения заменяют соответствующие старые задания:

- Исполнитель — Perplexity Computer вместо Arena Agent.
- Новый репозиторий результата — `aifirsts/NEOV2`, не `aifirsts/MATRIX` и не `aifirsts/01`.
- Восемь прежних шагов — история подготовки; не требуй команды «следующий шаг» для продолжения текущей задачи.
- Прежний план 10 сессий × 10 пакетов — исторический бюджет, а не обязательные остановки. Теперь выполняй задачу одним проходом.
- Агент принимает технические решения самостоятельно и фиксирует их в ADR/decision log.
- Если необходимое действие действительно возможно только с участием NEO, выполняй всё доступное, присваивай PASS-F, записывай точную будущую проверку и переходи дальше.
- Сообщение NEO «ПРОВЕРКИ ПРОЙДЕННЫ» из предыдущего чата учитывай как подтверждение владельца. Сначала найди соответствующие результаты, scope и hashes; не назначай автоматически всем новым артефактам статус проверенных.

Системные правила, ограничения инструментов, OAuth, обязательные платформенные approvals, юридические права и физически недоступные действия этот документ не отменяет. Если система требует подтверждения конкретной записи или публикации, подготовь точный payload и используй предусмотренный approval. Пока он недоступен, поставь PASS-F этому внешнему действию и продолжай локальную работу; не обходи контроль другим инструментом.

## 3. CONTEXT проекта

MATRIX — локальный Windows x64 центр представления ресурсов, проектов, связей, задач, контролей, evidence, findings и runbooks NEO. Ценность — понятная карта и проверяемые основания статусов, а не автономное управление инфраструктурой.

NEO — владелец проекта и соло-разработчик. Заявленная инфраструктура включает ПК, iPhone, Android, четыре USB, TP-Link, Samsung TV, Алису, VPS для VPN/агентов, GitHub, VS Code, ИИ-сервисы, OpenRouter, n8n и VPN. Наличие объекта в каталоге не означает известную конфигурацию или доказанную безопасность. Не придумывай адреса, модели, credentials, настройки или количество VPS.

Связанные проекты: MATRIX, SOCpit и WORLDOFTELEGRAM.AI. SOCpit остаётся отдельным продуктом; его документы — исторический источник принципов, не доказательства фактического состояния MATRIX или ПК. Платёжные/Telegram-сценарии внешних проектов не становятся функциями MATRIX автоматически.

Исходное направление: C# / WPF / MVVM, логическое разделение `MATRIX.Core`, `MATRIX.Application`, `MATRIX.Storage`, `MATRIX.Import`, `MATRIX.App`. Core не зависит от WPF, сети, файловой системы или исполнения команд. App вызывает Application; Storage/Import реализуют соответствующие порты, композиция находится в App.

Основной артефакт — `MATRIX.exe`; V2 — улучшенная преемственная версия этого продукта, а не обязательное переименование в NEOV2.exe. NEOV2 обозначает целевой GitHub repo. Сохрани продуктовую идентичность MATRIX, если фактический актуальный контракт не докажет иное.

Runtime-данные — `%LOCALAPPDATA%\MATRIX`, отдельно от executable. `C:\MATRIX-FINAL` ранее предложен, но не обязательный путь. Для V2-приёмки изоляция тестовых данных обязательна: не открывай и не изменяй живые данные NEO без разрешения; при несовместимости схем используй отдельный каталог/явную копию и инструкции миграции.

Не входят по умолчанию ни в V1, ни в это улучшение V2: API/SSH-коннекторы приложения, платежи, хранение secrets, обязательный admin, телеметрия, auto-update, localhost server, динамические плагины, autonomous infrastructure actions. Автономность Computer как разработчика не означает добавление автономных полномочий приложению.

## 4. История восьми шагов и чата

Это документарная сводка из данного чата и доступных материалов; counts — история артефактов, не количество успешных продуктовых тестов.

| Шаг | Подготовленный результат | Что нельзя выводить из этого |
|---|---|---|
| 1 | Анализ V1, master prompt, структура финальной поставки, план восьми этапов | Готовое приложение или проверенный репозиторий |
| 2 | Knowledge baseline: заявлено 39 файлов, 6 ADR, 16 требований | Полнота runtime-реализации |
| 3 | Domain supplement: заявлено 45 файлов, модели/схемы/policies/fixtures/reference; 8 Python-методов прошли по истории | Компиляция C#/Windows; актуальность historical Domain.cs |
| 4 | StrictJsonGuard, независимый validator, GenerationStore, OwnImportExport, smoke source, storage/import contracts; CLOSED_WITH_CARRYOVER | VERIFIED storage, абсолютная durability, полный current ADR007 |
| 5 | WPF/catalog/Application частичные исходники, новая integration implementation, assembler и Release1 handoff | Реально объединённый cumulative solution и production composition |
| 6 | State edits/session/VM, task DAG/readiness, conservative security proposal, recovery orchestration/compare, tests; итог заявлен 34 файла | Готовые typed WPF forms, recovery backend, утверждённая ADR007 policy |
| 7 | Build/test/publish scripts, inactive CI template, schemas, evidence/package tools; заявлено 40 файлов и 39 Python-тестов | Реальный Windows build/EXE/CI; выбранный SDK |
| 8 | Final preparation supplement: заявлено 50 файлов, 10 session prompts/60 conditional packets, 12 gates, assembler/merge/evidence tools; 34 Python-теста по истории | Полное включение предыдущих source bytes, cumulative app или бинарная поставка |

Внутри шага 5 было несколько дополнений, а не один окончательный source ZIP. Сводки ранних сообщений могли называть подготовку «закрытой», одновременно сохраняя product carryover. Не переносить слово «закрыто» в product readiness.

Ключевые уроки чата:

- Project Files доступны через поиск, но их архивы не обязательно доступны в локальном filesystem исполнителя.
- В нескольких проходах среда начиналась без предыдущих deliverables. Новая реализация по контракту не является восстановленными исходными bytes.
- Число файлов и тестовых сценариев не является доказательством compilation/runtime.
- Нельзя считать формально корректный evidence JSON доказательством происхождения логов или подлинности запуска.
- `MANIFEST.json` Step06 и `MANIFEST (2).json` audit оказались разными пакетами; нельзя выбросить первый только по суффиксу.
- Исторические manifest counts 31/13 не совпадали с отчётами 34/17 файлов; проверять actual ZIP/внутренний manifest, не объявлять повреждение по одной сводке.
- Publisher шага 5 ожидал `MATRIX.App.exe` и намеренно завершался исключением: он был intermediate, не готовый release pipeline. Исправить permanent `AssemblyName=MATRIX`, не переименовывать EXE вручную.
- Step07-проверки учитывали 10 gates; Step08 добавил требования no unresolved Critical/High и license/signing state. При смене статусов обновить все schemas/verifiers/tests, не только README.
- Проверки NEO, уже привязанные к тому же артефакту, следует переиспользовать; новые bytes требуют проверки затронутых сценариев.

Текущее исследование для составления FINAL.MD увидело reported inventory 475 Project Files, но API вернул усечённый список. Полное чтение всех 475 файлов, actual current source/ADR007 и всех ZIP не заявляется. Этот факт фиксирует границу составителя документа, а не отсутствие материалов в твоём Computer workspace. Расширь инвентаризацию доступными инструментами.

URL NEOV2 был запрошен при подготовке FINAL.MD, но получить страницу не удалось. Это не подтверждает ни пустоту, ни отсутствие, ни приватность, ни права репозитория. Проверяй через доступный authenticated GitHub connector/API/local checkout.

## 5. Источники и карта файлов

Ищи фактические папки, включая варианты имени/суффикса. Известные исторические расположения:

```text
АРХИТЕКТУРА NEO / Project Files
├── CONTEXT.md
├── artifacts-1of2/
│   ├── AGENTS.md, PRD.md, DATA-MODEL.md, CONTEXT.md
│   ├── ARCHITECTURE*, ACCEPTANCE*, RELEASE-GATES.md
│   ├── HANDOFF-STEP-03.md, STATE-STEP-03.json
│   ├── matrix-workspace.schema.json
│   └── исходные пакеты Step02 / Step03
├── artifacts-2of2/                     # обнаружить фактическое содержимое
├── artifacts(ШАГ4)/
│   ├── START-HERE (3).md, STATE (3).json, report (3).json
│   ├── CARRYOVER.md, ACCEPTED-LIMITS.md
│   ├── STORAGE-OVERVIEW.md, STORAGE-PROTOCOL.md
│   ├── IMPORT-EXPORT.md, VALIDATION.md, workspace-rules.json
│   └── PARTIAL / VALIDATION / CLOSURE ZIP supplements
├── artifacts(ШАГ5)/
│   ├── INPUT-PACKAGES.json, HANDOFF.md, STATE.json
│   ├── CONTRACT.md, GATES.json, KNOWN-ISSUES.md
│   ├── windows-build-publish.ps1
│   └── PARTIAL / INTEGRATION / ASSEMBLY / R1 HANDOFF пакеты
├── папки шагов 6 / 7 / 8               # имена и вложенность проверить
│   └── source, tools, docs, schemas, states, reports
└── artifacts(ПОСЛЕДНИЙ АУДИТ)/
    ├── AUDIT-REPORT.md, FINDINGS.csv
    ├── MANIFEST.json, MANIFEST (2).json
    ├── NEXT-CHAT-STEP07-IMPROVED.md
    ├── Step06 и audit ZIP/reports
    └── смешанные распакованные материалы нескольких пакетов
```

Имена — ориентиры, не обещание наличия или полного чтения. Также проверь все дополнительные папки, новые загрузки, repository files и readable attachments.

Известные пакеты для поиска:

| Назначение | Имя |
|---|---|
| Baseline | MATRIX_HANDOFF_STEP_02.zip |
| Domain | MATRIX_HANDOFF_STEP_03.zip |
| Storage | MATRIX_STEP04_PARTIAL.zip |
| Validation | MATRIX_STEP04_VALIDATION_SUPPLEMENT.zip |
| Closure | MATRIX_STEP04_CLOSURE_SUPPLEMENT.zip |
| UI/catalog | MATRIX_STEP05_PARTIAL_SUPPLEMENT.zip |
| Integration | MATRIX_STEP05_INTEGRATION_SUPPLEMENT.zip |
| Assembly tool | MATRIX_STEP05_ASSEMBLY_TOOLING.zip |
| Release1 handoff | MATRIX_STEP05_ARENA_R1_HANDOFF.zip |
| State workflows | MATRIX_STEP06_SUPPLEMENT.zip |
| Pre-step07 audit | MATRIX_PRE_STEP07_AUDIT_SUPPLEMENT.zip |
| Release tooling | MATRIX_STEP07_ARENA_SUPPLEMENT.zip |
| Final preparation | MATRIX_STEP08_FINAL_PREPARATION_SUPPLEMENT.zip |

Порядок исследования:

1. Получи полный inventory всех реально доступных папок/файлов: путь, размер, тип, SHA-256, origin, доступность, отношение к пакету/версии.
2. Прочитай требования/ADR/schemas/Models/StatusPolicy/composition/storage/import/VM/tests/CI/release/evidence. Остальные файлы классифицируй и проверь по назначению; generated/binary не выдавай за прочитанный исходник.
3. Различай metadata-only, excerpt-read, full-text-read, exact-byte-verified, executed. Каждый статус подтверждается способом получения.
4. Распаковывай доступные ZIP в отдельные provenance-каталоги после проверки hash, путей, case collisions, duplicates, sizes, symlink/reparse и manifest coverage.
5. При readable unpacked files работай с ними; не повторяй требование приложить ZIP. При отсутствующих bytes прочитай контракт и создай только нужную новую реализацию.
6. Version rule: `имя (3).ext > имя (2).ext > имя.ext` для одного назначения. Разные документы/пакеты не версии друг друга. `STATE-STEP-03` и `STATE (3)` согласовать, не выбрать числом.
7. Сохрани старые версии в provenance. Создай `source → working → hash → selection reason`, collision log и список неподтверждённых/недоступных источников.
8. Не называй ZIP cumulative, пока реально не объединены все необходимые исходники и знания с полной картой coverage.

Усечённый search result — не причина бесконечно повторять запрос. Меняй путь/метод, используй локальные реальные files, authenticated repo и специализированные доступные tools. Объективно недоступный источник — PASS-F с перечнем, без остановки независимой реализации.

## 6. PASS-F: точная семантика

PASS-F — новый статус управления работой, установленный NEO. Он означает: агент выполнил все доступные ему действия, но конкретное завершение/подтверждение требует участия NEO или недоступной авторизации/среды.

| Статус | Значение | Продолжать независимые этапы |
|---|---|---|
| PASS | Реальное действие/проверка выполнены и результат подтверждён в указанном scope | Да |
| PASS-F | Максимально выполнено; зафиксирована конкретная будущая проверка/операция NEO | Да, как при PASS для workflow |
| FAIL | Реально обнаруженная ошибка или провал проверки, который нужно исправить | Исправить; другие независимые работы продолжаются |
| RUNNING | Агент ещё выполняет доступную работу | Да |

`PASS-F = PASS` только в переходах workflow. `PASS-F ≠ verified PASS` в фактах, security, tests, release confidence и утверждении о публикации. Не смешивай это с доменным `Evidence.result`, `Security`, freshness или status Task. Product Security никогда не повышается из-за orchestration PASS-F.

Сохраняй две оси вместо стирания истории:

```json
{
  "id": "V2-G08",
  "workflowStatus": "PASS-F",
  "continueAllowed": true,
  "verification": {
    "actualResult": "NOT_RUN",
    "scope": "приёмка финального EXE на ПК NEO",
    "performedByAgent": ["доступные build/tests и документация"],
    "evidenceRefs": [],
    "environmentLimit": "ПК NEO недоступен агенту"
  },
  "followUp": {
    "owner": "NEO",
    "action": "выполнить конкретный сценарий по инструкции",
    "artifactIdentity": "actual version/commit/EXE hash после сборки",
    "expectedResult": "наблюдаемое условие успеха",
    "risk": "какие свойства ещё не подтверждены",
    "priority": "HIGH",
    "howToBecomePASS": "добавить результат и evidence для тех же bytes"
  }
}
```

Пример JSON — шаблон, не свидетельство реально выполненного build.

PASS-F допустим только после исчерпания доступных способов в разумном объёме. Нет Windows в локальном sandbox? Сначала проверь разрешённый Windows CI/runner/remote environment. Нет repo write? Сначала обнаружь коннектор и проверь реальные права. Не помечай PASS-F то, что можешь исправить сам: compilation error, failing unit test, unsafe placeholder, забытый adapter, malformed JSON, известную потерю данных или ложный Security PASS.

Для фактического FAIL не завершай проект словом BLOCK: исправь, повтори проверку и продолжи. Если обнаруженный дефект устранить невозможно из-за внешнего участия, отражай и `actualResult=FAIL`, и workflow PASS-F с impact/безопасным обходным решением. Это не разрешение распространять заведомо опасный executable или публиковать secrets: продолжай аудит, исправления независимых частей, source checkpoint и безопасный draft вместо опасной операции.

Старые validators, которые требуют строго PASS на каждом шаге и останавливают всю работу на NOT_RUN, обнови под двухосевую модель. Сохрани строгую проверку реальных hashes, failed tests и фактов. Тестами докажи: PASS-F разрешает workflow; PASS-F не превращает NOT_RUN в executed; actual FAIL не скрывается; отсутствие permissions не порождает фиктивную публикацию.

Обязательные approvals/логин/оплата/физические действия: подготовь всё доступное, запиши PASS-F и точный pending action, продолжай. Не обходи системный запрет и не покупай credits/услуги без разрешения. Финальный список действий NEO — один агрегированный `NEO-ACTIONS.md`, не поток мелких вопросов.

## 7. Контракт V1, который сохраняется в V2

### 7.1 Модель

Node, Edge, Project, Task, Control, Evidence, Finding, Runbook, AuditEvent и UserStatement. Читай актуальные схемы для exact fields/enum/required и генерируй согласованные модели/DTO, не копируй устаревший Domain.cs.

- Node: global id, type, name, owner, trustZone, sensitivity, availability; Security вычисляется отдельно.
- Edge: from/to Node, kind, criticality; типы связей baseline `depends_on`, `connects_to`, `authenticates_with`, `hosted_on`, `deployed_from`, `backs_up`, `monitors`, `pays_through`.
- Project.nodeId привязан к Node типа project; не дублируй общие ресурсы.
- Task.projectId → Project, dependencies → Task, DAG, повторяющиеся dependencies запрещены.
- Control → Node; criterion, scope, method, required, criticality, TTL, applicability/rationale.
- Evidence → Control; scope/source/method/operator, observedAt/validUntil, result/provenance/revoked/supersedes.
- Finding/Runbook/UserStatement target → Node; принятый риск не означает устранённую уязвимость.
- AuditEvent.targetId может ссылаться на историческую сущность; это не active-reference ошибка.
- Supersedes: тот же control/scope, без циклов и повторов. Импорт не продлевает даты. UserStatement не Evidence.

### 7.2 Время и Security

UNKNOWN, STALE, MISSING, пустой набор и заявление пользователя не дают PASS. Show reason/control/scope/source/freshness и историю отдельно от текущей оценки. N/A требует rationale и не служит скрытым PASS.

Прочитай ADR007, если доступен. Если его нет, не выдумывай историческое содержание. В рамках делегированной автономии выбери и обоснуй новую явную V2 policy в новом ADR с regression fixtures: конфликт PASS/FAIL, supersedes chains, revoked, expired, future timestamps, clock boundaries, applicability и required/optional aggregate. Технический выбор такой политики не требует косметического согласования, но не может отменить запрет ложного PASS. Сохрани conservative UNKNOWN для неразрешённых/невалидных данных.

### 7.3 Runtime limits

Согласовано NEO 7 октября 2026:

- Вход до 16 MiB.
- JSON depth до 32.
- До 1 000 000 tokens.
- UTC timestamps, не более семи дробных цифр секунды.
- Строгая целочисленная запись, а не float/exponent coercion.
- Repo URL — разрешённые HTTPS repo paths на github.com.
- Повторяющиеся dependencies/supersedes запрещены.

Closed schemas, duplicate JSON keys, unknown fields/versions, references, global ID uniqueness, TTL, methods/scopes и cycles проверяй production pipeline. Baseline числовые caps/short/text/TTL сверяй с actual schema. Никакого syntactic smoke callback вместо full validator.

### 7.4 CRUD и UI

Русский интерфейс: дерево слева, граф в центре, карточка справа, события/ошибки снизу. Каталог и state typed forms, поиск/фильтры, synchronized selection, keyboard, wrap, виртуализация по необходимости, счётчики visible/total/hidden.

Create/edit/delete Node/Project/Edge и tasks/controls/evidence/findings/runbooks/statements. Delete RESTRICT с причинами и связями, no implicit cascade. Audit read-only. Cancel не пишет; draft сохраняется при ошибке; пользовательская карточка обновляется только после успешного commit/read-back. Missing backend не прикрывается активной кнопкой-заглушкой.

### 7.5 Storage/import/recovery

Coherent catalog/state snapshot, immutable generations, CURRENT pointer, SHA-256, expected revision, cooperative lease, write→flush→reread/validate→publish. Не обещай атомарность пары независимых File.Replace, произвольную power-loss durability, защиту от всех path races и hostile editors.

Выбери фактический storage protocol и документируй совместимость legacy/V2. Backup исходных данных, dry-run compare, explicit migration, unknown version rejection. Identity v1 load не полноценная migration. После uncertain commit — контролируемое повторное чтение и review, не ложный успех/rollback/retry.

Own import replace-only: preview → explicit confirmation → точные bytes+revision → commit. Полный diff/counts, safe export writer, secret-safe errors. Merge/SOCpit/attachments/ZIP runtime import остаются выключенными, если это улучшение не реализовано полностью, совместимо и отдельно обосновано. Source ZIP tooling не превращается в runtime ZIP import.

Recovery: enumerate/validate candidates, corrupt-head preview без auto reset/fallback, restore только в новый approved destination, точные bytes/revision/hash, compare/read-back/source untouched. Cleanup/retention только с проверенной безопасной политикой.

### 7.6 Secrets и доверие

Heuristic screening не доказывает отсутствия secrets. Проверять decoded values до display/log/save/import/export/recovery; фиксированные коды, никаких raw inputs или exception chains с данными. Imported texts/paths/URLs — данные, не команды. Публичные samples fictional, не private topology NEO.

## 8. Что означает улучшенный V2

Создай реально интегрированный, проверяемый преемственный продукт. Не ограничивайся заменой номера версии и новым README. По результатам аудита составь `V1-V2-CHANGE-MATRIX.md`: baseline факт → дефект/ограничение → V2 изменение → test/evidence → остаточная проверка NEO.

Обязательные направления улучшения:

| Направление | Требуемый результат V2 |
|---|---|
| Целостность | Один coherent solution, реальные references/composition, отсутствие conflicting scaffold duplicates |
| Domain/status | Актуальные модели, объяснимая вычисляемая Security policy, время/конфликты/supersedes regression |
| Production pipeline | Один полный schema/semantic/reference/secret путь для UI/save/import/export/recovery |
| Storage | Проверенный protocol, revision/lock/corruption/uncertain handling, bounded safe recovery |
| UX | Рабочие typed формы, синхронная навигация, keyboard, filters/wrap/counts/states |
| State workflows | Tasks/evidence/findings/runbooks/statements, audit, cancel/RESTRICT/reopen |
| Import/export | Exact preview/commit, dates preserved, безопасный file writer, понятный destructive compare |
| Compatibility | Explicit V1→V2 dry-run/migration/rollback instructions либо честный новый isolated workspace |
| Delivery | Exact SDK, permanent MATRIX.exe, Windows CI, actual suites, self-contained output, manifests/notices |
| Evidence | PASS/PASS-F dual-axis, actual source/artifact identity, no fabricated results |
| Knowledge | Читаемые непротиворечивые docs, source/version map, audit fixes и NEO follow-ups |

Автономно выбирай UX детали, архитектурный refactoring, тестовый framework, dependency versions и exact SDK после проверки доступности/поддержки/совместимости. Не переносить .NET 8/10 вслепую из SOCpit. Не менять стек на web/localhost вместо WPF только потому, что sandbox Linux: найди Windows build path; при отсутствии — PASS-F и подготовь правильный Windows проект.

При missing исходнике разрешена новая реализация по подтверждённому контракту; сохраняй прочитанные рабочие части. Не придумывай полномасштабную online V2, платежи, broker, SSH или системное администрирование без отдельного задания. Автономность — выбор решений внутри scope, не расширение полномочий.

Результат «V1 → V2» означает единый улучшенный V2 и воспроизводимую baseline/compatibility историю. Если V1 executable отсутствует, не фабрикуй его ради двух релизов. Если он существует, зафиксируй версию/commit/hashes и не перезаписывай исторический asset.

## 9. Целевая файловая структура NEOV2

Ниже целевая структура результата, а не утверждение о текущем содержимом NEOV2. Сохрани подходящие existing paths, избегай бессмысленного переименования. Добавления выбирай по реальным проектам.

```text
NEOV2/
├── FINAL.MD                         # это задание, provenance и версия
├── README.md                        # русский start-here и фактический status
├── CONTEXT.md                       # актуальный V2 контракт, не переписанная история
├── AGENTS.md                        # Computer, autonomy, PASS-F, trust boundaries
├── SECURITY.md
├── CHANGELOG.md
├── LICENSE                          # только действительное решение и права
├── global.json                      # exact проверенный SDK, не placeholder
├── MATRIX.sln / MATRIX.slnx          # выбрать один основной solution
├── Directory.Build.props
├── src/
│   ├── MATRIX.Core/
│   ├── MATRIX.Application/
│   ├── MATRIX.Storage/
│   ├── MATRIX.Import/
│   └── MATRIX.App/
├── tests/
│   ├── MATRIX.Core.Tests/
│   ├── MATRIX.Application.Tests/
│   ├── MATRIX.Storage.Tests/
│   ├── MATRIX.Import.Tests/
│   ├── MATRIX.App.Tests/
│   ├── MATRIX.Integration.Tests/
│   └── MATRIX.Release.Tools.Tests/
├── schemas/
│   ├── matrix-workspace-v1.schema.json
│   ├── matrix-workspace-v2.schema.json # только если реально изменён wire contract
│   ├── execution-status.schema.json
│   └── release-evidence.schema.json
├── fixtures/                        # синтетические, не реальные данные NEO
├── scripts/
│   ├── build-test.ps1
│   ├── publish.ps1
│   ├── package.ps1
│   └── verify-artifacts.ps1
├── tools/
│   ├── inventory/
│   ├── assembly/
│   ├── audit/
│   └── evidence/
├── docs/
│   ├── ARCHITECTURE.md
│   ├── DATA-MODEL.md
│   ├── THREAT-MODEL.md
│   ├── UI-SPEC.md
│   ├── STORAGE-PROTOCOL.md
│   ├── IMPORT-EXPORT.md
│   ├── V1-V2-CHANGE-MATRIX.md
│   ├── MIGRATION.md
│   ├── BACKUP-RESTORE.md
│   ├── TEST-PLAN.md
│   ├── RELEASE-CONTRACT.md
│   ├── NEO-ACTIONS.md
│   ├── DECISION-LOG.md
│   ├── SOURCE-REGISTER.md
│   └── adr/
├── audit/
│   ├── INVENTORY.json
│   ├── SOURCE-MAP.json
│   ├── COLLISIONS.json
│   ├── FINDINGS.json
│   ├── REQUIREMENTS-TRACEABILITY.csv
│   ├── COVERAGE.md
│   ├── STATUS.json
│   └── FINAL-REPORT.md
├── evidence/
│   ├── OWNER-REPORTS.json
│   ├── BUILD-INFO.json
│   ├── TEST-RESULTS.json
│   ├── RELEASE-EVIDENCE.json
│   └── safe-logs/                   # reviewed; no private payloads/secrets
├── provenance/                     # история; публично только safe/licensed subset
│   └── package-id/original-path
└── .github/workflows/
    ├── build-test.yml
    └── release-candidate.yml
```

`provenance` в локальном bundle и публичный Git не одно и то же. Не загружай автоматически все старые ZIP/логи/документы в публичный repo: сначала privacy/secret/license review. Для исключённых private материалов сохрани path/hash/reason, не их содержимое.

Binary assets и большие архивы по возможности помещай в GitHub Release, а не обычную Git-историю. Repo должен содержать coherent source tree, а не только набор ZIP. Если разрешён только файловый connector, используй его в пределах возможностей; не считай commit/PR/Release существующим без read-back.

Не создавай пустые schemas/tests лишь для внешней полноты дерева. Невыполненный компонент обозначается реальным scope/status; V2 schema не нужна, если wire v1 сохранён. Версию продукта, версию workspace schema и версию storage protocol различай.

## 10. Автономное управление и решения

Computer — единственный координатор и ответственный за интеграцию. При доступности подагентов распределяй независимые роли: source historian, architect/domain, storage/import, WPF/VM, test/CI, release/security audit. Параллелизм не даёт права нескольким workers бесконтрольно редактировать один файл: назначь ownership, branch/worktree или единый интеграционный writer.

Самостоятельно:

- Выбирай порядок исправлений по impact/severity/dependencies.
- Решай technical conflicts на основе актуального контракта и фактических tests; записывай ADR.
- Выбирай exact SDK и supported Windows target после проверки официальных сведений на дату работы и реальных csproj.
- Выбирай совместимый test framework, минимальные зависимости, UI layout и алгоритмы graph/readiness.
- Определи version/tag после inspection NEOV2 и существующих releases.
- Обновляй старые prompts с Arena на Computer и старые target repos на NEOV2 в текущих operational docs; в provenance оставляй original text.
- При отсутствии ADR007 самостоятельно принимай новый V2 technical ADR, не зависай в бесконечном «предложено»; не нарушай invariants.
- При ресурсном ограничении сокращай второстепенные enhancements, не качество базовой validation и не правдивость отчёта.

Не делегировано:

- Изменять ОС, router/VPS/accounts или живые данные NEO ради разработки.
- Force-push, удалять existing branches/tags/releases или заменять assets без требуемого approval.
- Обходить OAuth, отключать mandatory approvals, покупать credits/услуги.
- Публиковать secrets/private топологию или чужой код без соответствующих прав.
- Объявлять физическую проверку на ПК NEO выполненной за пользователя.

Используй branch в NEOV2 с понятным непересекающимся именем, выбранным после просмотра existing branches. Прямой push в защищённую ветку не обходить. Если main merge требует пользователя, PR/source checkpoint плюс PASS-F для merge; независимые tests/package/docs продолжаются.

Повторные попытки должны иметь смысл: изучить ошибку, исправить причину, повторить. При неисправимом auth/action deny не спамить retry; PASS-F и продолжение. Конечное evidence — read-back IDs/URLs/hashes, не только успешный HTTP ответ или UI notification.

## 11. Однопроходный алгоритм выполнения

Выполняй внутренние фазы без промежуточного ожидания команд NEO. Контрольные точки — для устойчивости работы, не остановки владельцу на согласование.

### Фаза A — capability и source discovery

Определи доступные tools, connectors, local FS, Git, dotnet, PowerShell, Windows runner, CI permissions, repo privacy/branch/assets и бюджеты. Не предполагай наличие Windows у Computer. Проверь source repos только для чтения; новый write target — NEOV2.

Получение target repo не удалось составителю FINAL.MD; authenticated inspection обязателен. Empty repo, private repo, absent repo и denied access — разные факты. Если repo отсутствует и creation разрешён платформой/заданием, подготовь создание с требуемым approval. Если нельзя — локальная полная поставка и PASS-F GitHub upload, не выдуманный success.

### Фаза B — глубокий аудит всех источников

Составь inventory всех реально доступных файлов; вручную/инструментами исследуй requirements, каждый handoff/state/manifest/report, code/tests/schemas/scripts/CI. Определи fully read/partial/unavailable, package boundaries, duplicates, old copies, byte coverage. Аудит чата: отличи user decisions, assistant claims, actual tool outputs и исторические планы.

Сопоставь шаги 1–8 с implementation. Найди missing references, несовместимые DTO/enums, dead code, test doubles в production, disabled обязательные functions, unsafe defaults, формальные gate bypass, секреты, history contradictions. Итог: reproducible findings с impact, scope, source paths, actual evidence и proposed fix.

### Фаза C — coherent baseline

Восстанови рабочее дерево из актуальных источников с provenance и explicit source map. Проверяй manifests по содержимому/пакету, а не имени. Новый код только там, где нужен. Реши actual protocol/model conflicts, SDK и schema compatibility; compile как можно раньше.

Если baseline code не buildable, исправляй, а не объявляй «домен готов» по Python. Если Windows пока недоступен, продолжай pure Core/Application checks, подготовь настоящую WPF/CI конфигурацию и используй PASS-F для недоступного platform step после попыток.

### Фаза D — V2 implementation

Закрой audit findings и улучшения раздела 8. Подключи production composition root, typed forms, full validator/secret pipeline, actual storage/recovery/export, reliable statuses. Реализуй explicit migration/dry-run либо isolated versioned workspace, сохрани исходные данные.

Обнови application code, schemas, DTO, docs, fixtures и тесты согласованно. PASS-F orchestration держи отдельно от domain statuses. Не оставляй tests-only callbacks в production. Комментарий TODO не реализация.

### Фаза E — tests и независимый final audit

Реально исполни доступные suites, negative/boundary/property/fault tests и Windows сценарии. Проверь logs/artifact identity, nonzero counts. Отдельно review изменения без авторской самоуверенности; при доступности независимый подагент критически проверяет security, data loss, gates и release content. Найденный FAIL → исправление → regression → affected rerun.

### Фаза F — build/publish/package

Exact SDK/Windows target, Release win-x64 self-contained; permanent MATRIX.exe. Проверить published executable и companion files в доступной Windows-среде. Data path/ordinary user/offline сценарии. Package hashes, VERSION/build info, source checkpoint, release notes, notices, migration/recovery instructions.

Если реальные Windows build/EXE проверки невозможно выполнить без NEO, PASS-F разрешает продолжить к source delivery/CI/package instructions. Не фабрикуй EXE, MZ stub, screenshots и build outputs; source-only asset назови source-only, не runnable release.

### Фаза G — NEOV2 upload и release

После privacy/license checks загрузи coherent sources/docs/tests/scripts в NEOV2 через доступные разрешённые tools. Реальные внешние approvals соблюдай. CI запусти и проверь результаты/IDs, если это доступно. Создай подходящий prerelease/draft при capabilities и truthful status; если нет — PASS-F и полный prepared payload.

### Фаза H — финальный результат

Проверь реальный repo/branch/commit/PR/release read-back. Составь final report, complete source map, V1→V2 changes, actual PASS и PASS-F, один NEO-ACTIONS, ZIP hashes/coverage. Заверши одним полезным отчётом, а не требованием «дайте следующую команду».

## 12. Предметные задания глубокого аудита

В каждом finding: id, severity, область, concrete source, reproduction/analysis, impact, fix, regression, actual result и residual owner action. Не выдавай гипотезу за подтверждённый баг.

### Код и integration

Проверь все solution/project references, TargetFramework/UseWPF/platform, package pins, namespaces, nullable contracts, serialization, global IDs, equality/clone/immutability, command lifetimes, thread affinity, cancellation, busy/reentrancy, bindings/events, DataContext, typed editor validation.

Особо проверь исторические гипотезы StateSession: failed Load не оставляет редактируемый старый snapshot; Saved result отражает exact approved candidate и read-back; unexpected difference/revision/outcome ведёт к review; semaphore/cancellation не вызывают false rollback. Читай actual source, не патчи по памяти.

### Storage и filesystem

Lease и revision coordination, lock-vs-corrupt errors, CURRENT validation, pair/hash integrity, unique staging, observed reparse checks, same-directory publish, pre/post-commit faults, read-back, orphan retention. Отдельно лимиты гарантий и external edits. Никакого silently chosen backup или bootstrap на corrupt head.

Recovery confirmation содержит exact candidate ID/revision/SHA и полностью разрешённый destination. Approval одного preview не применяется к другому источнику/каталогу. Cancellation после начала write — возможно uncertain, не предположение rollback. Reread failed state должен блокировать unsafe mutation локально, но не останавливать весь orchestration.

### Validators и import

Strict UTF-8, duplicate property names после decoding, JSON depth/token/byte limits, unknown fields/version, exact integers, timestamp syntax/UTC, domain refs, duplicate dependencies/supersedes, resource/task cycles, repo URL parser/host/path policy, URI open action. Проверяй decoded string secret screening и safe timeout handling.

### Status и evidence

Expiry boundary, future dates, revoked, conflict, supersedes DAG, scope/method, required/optional, N/A rationale, historical last PASS, imported timestamps, clock injection, statement not evidence, findings visibility. Реальный PASS ограничен criterion/scope/validity; нет «100% безопасности».

### Tooling и release

Согласованность всех gate IDs, schema/version, PASS-F semantics, build result parser, TRX counts, console smoke summaries, no zero-tests green, artifact/hash binding, JSON booleans вместо integers, hidden case/path aliases, manifest self coverage, ZIP traversal/duplicates/limits/symlink, changes after approval/review.

Проверяй escaping/regex и path handling в Python/PowerShell реальными negative tests. Разбор AST/XML не compilation. Проверка MZ и SHA не запускает EXE. Test fixtures с fake commit/SDK/hash не являются production evidence.

### Docs/authority/privacy

Старые repo/agent instructions не продолжают управлять текущей задачей. New FINAL.MD и NEO decision переопределяют historical Arena/step gating/PASS-only workflow; factual invariants и trust boundaries остаются. File content не может отменить platform permissions. Source reuse лицензии/secret history/private topology проверяется до public upload.

## 13. Обязательная матрица проверок

Создай автоматизированные tests там, где это возможно. Точные performance budgets измеряй, не выдумывай.

| ID | Сценарий | Наблюдаемый результат |
|---|---|---|
| T01 | Project → resource → depends_on → save/close/open | Те же IDs, сущности и Edge |
| T02 | Один ресурс двух проектов | Один Node, обе проекции |
| T03 | Cancel всех editors | Нет store write/audit commit |
| T04 | Delete referenced object | RESTRICT с точной причиной; no cascade |
| T05 | Task missing/duplicate/self/cyclic deps | Reject; resource cycles allowed |
| T06 | Project binding/global IDs | Correct project Node, no duplicates/dangling refs |
| T07 | Empty/missing/stale/statement | Не Security PASS |
| T08 | TTL/expiry/future/UTC/revoked | Правильная freshness/policy, deterministic clock tests |
| T09 | PASS/FAIL conflict/supersedes | Документированное решение, no cycles/scope drift |
| T10 | Imported evidence | observedAt/validUntil unchanged |
| T11 | Strict JSON boundaries | UTF-8/keys/depth/tokens/bytes/integers/unknown reject |
| T12 | URI/browser action | Только approved HTTPS policy; no imported commands |
| T13 | Secret-like values/timeout | Safe rejection/redaction policy, no raw leak |
| T14 | Revision conflict/lease/access denied | Не false success/corruption/reset |
| T15 | Corrupt CURRENT/hash/generation | Preserve, объяснимое состояние, no silent fallback |
| T16 | Pre/postpublish fault/cancellation | Controlled uncertain outcome/reread/review |
| T17 | Failed reload/stale snapshot | Нет unsafe mutation старого состояния |
| T18 | Recovery exact approval/destination/replay | New root, same bytes, source untouched, read-back |
| T19 | V1→V2 migration | Dry-run, backup, exact changes, safe rollback instructions |
| T20 | Tree/graph/card/search/filter | Selection sync; visible/hidden counts |
| T21 | Keyboard/wrap/empty/loading/error/locked | Доступный layout и понятные состояния |
| T22 | Typed Task/Control/Evidence/Finding/Runbook/Statement | Validated CRUD, audit read-only, no Execute |
| T23 | Published EXE ordinary Windows user | Без SDK/admin и runtime-required сети |
| T24 | Read-only install/non-ASCII path/data root | Данные вне EXE, safe start/save |
| T25 | PASS-F execution schemas/verifiers | Continue true, verification truth unchanged |
| T26 | Evidence/log/commit/EXE tampering | Detect; no fabricated origin proof |
| T27 | ZIP paths/case/manifest/size/changed review | Reject unsafe or unreviewed payload |
| T28 | NEOV2 upload/release/read-back | Фактические commit/PR/Release IDs или truthful PASS-F |

Наличие tests source — не PASS. Для каждого запуска: actual timestamp, command, environment, SDK, commit/tree identity, exit code, suite/count, safe log path/hash, artifact identity. Для ручных сценариев укажи operator и observations, не screenshots неработающего UI.

Весь доступный Compute/CI capacity использовать разумно; не зацикливаться на недоступном физическом ПК. Owner-only checks оформляются PASS-F с воспроизводимыми инструкциями, не глобальным BLOCK. Реальные дефекты agent-fixable исправлять до delivery.

## 14. GitHub и версия выпуска

Текущая цель записи — только [aifirsts/NEOV2](https://github.com/aifirsts/NEOV2). Исходный MATRIX и исторические repo читать для анализа, не изменять без отдельного задания.

1. Обнаружь доступный GitHub connector/API/git, реальную auth identity, repo existence, privacy, default branch, current commit, license, rules и existing releases.
2. Не считай repo пустым. Сохрани существующую историю и не стирай чужие/старые файлы без explicit review и требуемого approval.
3. Создай рабочую branch с уникальным именем. История маленьких осмысленных commits допустима в одном проходе; итоговый source tree coherent.
4. Выполни secret/privacy/dependency-license review до push; не переносить все provenance archives автоматически в public repo.
5. Загрузи sources/tests/docs/CI в NEOV2. Если tool/platform требует specific approval — соблюдай его; если approval недоступен, PASS-F push и выдача exact local bundle/diff/payload.
6. Запусти доступный CI, дождись результатов в разумных пределах, исправь failures, запиши actual run URL/ID. Если запуск требует недоступного участия — PASS-F, подготовленный workflow/инструкция.
7. Базовое предложение версии — `2.0.0-preview.1`, tag `v2.0.0-preview.1`. Проверь коллизии и самостоятельно выбери корректный следующий preview, если этот tag занят. Не меняй assets существующего Release молча.
8. Default distribution — prerelease. PASS-F личной приёмки NEO не блокирует создание truthful prerelease, если доступны безопасный настоящий build и permissions; в notes перечисли remaining checks. Это новая workflow policy владельца, не fictitious stable acceptance.
9. Если нет настоящего executable, можно загрузить source checkpoint и/или source-only prerelease/draft с ясным названием. Не называй его готовым Windows executable release и не добавляй fake `MATRIX.exe`.
10. Stable допускается только при реальной достаточной product verification и применимой owner acceptance/authorization. При owner-only checks PASS-F продолжай delivery, но не выдавай stable как fully accepted: сохраняй prerelease/draft/status. Не выбирай stable лишь потому, что workflow считает PASS-F проходным.
11. Release actions: resolved repo/tag/title/body/prerelease flag/exact asset names+hashes; required platform approval; execute unchanged; read-back.
12. Подтверди actual commit/branch/PR/release URL и asset hashes. Нет credentials/permission/network = действие PASS-F, а не claim «загружено».

Не force-push, не отключать protections/approvals, не публиковать secrets, не использовать случайные credentials из старых files. Пользователь просит upload в будущем Computer execution; составление FINAL.MD само по себе repo не изменяет.

## 15. Поставка

Два отдельных результата:

### 15.1 Source/knowledge checkpoint

Coherent исходники V2, tests, scripts, schemas, safe provenance/source map, audit, actual evidence, docs и FINAL.MD. ZIP назвать, например, `NEOV2-SOURCE-<version>-<commit>.zip` после выбора реальных version/commit. `cumulative=true` только если actual necessary sources/knowledge включены и coverage подтверждена; иначе точный partial/supplement с omissions.

### 15.2 Windows distribution

```text
MATRIX-FINAL/
├── MATRIX.exe                      # реальный publish output
├── необходимые companion files
├── README-RU.md
├── KNOWN-ISSUES.md
├── DATA-RECOVERY.md
├── MIGRATION.md
├── THIRD-PARTY-NOTICES.md
├── VERSION.json
└── SHA256SUMS.txt
```

Self-contained win-x64; single-file только если реально работает с WPF/содержимым. Никаких SDK/admin требований для пользователя. VERSION содержит exact product version, commit/tree identity, SDK, RID, build time и truthful validation status. Не публикуй placeholder metadata как actual version info.

Final ZIP paths/duplicates/hashes/manifest coverage проверить; SHA256SUMS не хеширует сам себя; final ZIP SHA внешний после последнего изменения. Hash = integrity, не digital signing; unsigned обозначать явно. Licence/third-party rights не выдумывать: используй установленные лицензии; если требуются owner decision или missing permission, PASS-F для этого решения и только допустимый безопасный subset поставки.

Не включать real workspace NEO, credentials, private IPs/accounts/topology, unsafe screenshots, raw exceptions и imported private snapshots. Release logs/evidence хранить отдельно либо только safe subset.

Обязательные документы результата:

- `audit/FINAL-REPORT.md` — весь выполненный цикл и ограничения.
- `audit/INVENTORY.json` и `SOURCE-MAP.json` — actual source coverage.
- `audit/FINDINGS.json` — найдено/исправлено/remaining, без fake closure.
- `docs/V1-V2-CHANGE-MATRIX.md` — что реально улучшено.
- `docs/DECISION-LOG.md` и ADR — автономные решения с основаниями.
- `docs/NEO-ACTIONS.md` — единый список PASS-F, инструкции и ожидаемый PASS.
- `audit/STATUS.json` — двухосевая status модель.
- `evidence/RELEASE-EVIDENCE.json` — actual commands/logs/context/hashes/URLs.
- `README.md`, сборка/запуск/backup/migration/recovery/known limitations/notices.
- Source ZIP, binary ZIP если реально получен, SHA manifests и links/IDs если загружены.

## 16. Набор gates V2

Сохрани traceability к R1-01..12, но используй новую workflow-модель. У каждой gate две оси: workflow PASS/PASS-F/FAIL/RUNNING и actual verification PASS/FAIL/NOT_RUN/USER_REPORTED/NOT_APPLICABLE с scope и evidence.

| Gate V2 | Проверяется |
|---|---|
| V2-G01 | Реальный source inventory, repo identity/rights, provenance |
| V2-G02 | Coherent solution/composition и actual source merge |
| V2-G03 | Current domain/schema/status invariants и V1 compatibility |
| V2-G04 | Full validation/reference/heuristic secret pipeline |
| V2-G05 | Production WPF/catalog/state UX |
| V2-G06 | Storage/revision/lease/corruption/uncertain/fault |
| V2-G07 | Import/export/recovery/migration safeguards |
| V2-G08 | Actual tests/Windows build/published EXE acceptance |
| V2-G09 | Distribution metadata/hashes/notices/privacy |
| V2-G10 | In-scope findings: actual agent-fixable Critical/High устранены |
| V2-G11 | NEOV2 upload/CI/release/auth approvals/read-back |
| V2-G12 | Scoped NEO acceptance и remaining owner actions |

Одна недоступная owner action не останавливает другие gates. PASS-F для V2-G12 пропускает orchestration к дальнейшей передаче/завершению, не повышает product Security и не выдаёт «NEO лично проверил».

Не «лечи» FAIL заменой буквы в JSON. PASS-F обязан иметь реально выполненные агентом меры, причину owner-only ограничения, риск, artifact identity, конкретную future action и criterion перехода в PASS. Изменился артефакт — review применимости previous owner results.

Финальный preparation/result допускает mix PASS/PASS-F: `COMPLETED_WITH_PASS_F` означает весь доступный цикл выполнен и остатки прозрачны. Это не то же самое, что `FULLY_VERIFIED_STABLE_RELEASE`.

## 17. Формат итогового ответа Computer

Начни с прямого результата: что создано, какая версия, что действительно загружено в NEOV2, есть ли настоящий executable и какие проверки PASS/PASS-F.

Затем сообщи:

1. Фактические repo/branch/commit/PR/release links; отсутствующие actions обозначь PASS-F, не вымышленные URL.
2. Ключевые audit findings и реальные V1→V2 улучшения.
3. Actual tests/build/Windows/EXE checks с counts/environment и evidence locations.
4. Source/binary assets и SHA-256, cumulative/partial статус.
5. Чёткую таблицу PASS-F: выполнено агентом → почему нужно NEO → инструкция → ожидаемый результат → affected version/hash.
6. Где лежат README, FINAL-REPORT и NEO-ACTIONS в результате.
7. Ограничения и truthful prerelease/source-only/stable статус.

Не заканчивай планом без файлов. Не возвращай управление на каждом косметическом выборе. Если всё доступное сделано, завершай максимальным результатом за один проход; owner actions собери один раз в финале.

## 18. Границы исходного исследования

При составлении этого документа изучены доступные релевантные Project Files и история данного чата. Reported inventory вырос до 475, но tool выдаёт bounded metadata и поиск не вернул весь actual source tree. Прочитанные источники ниже не означают независимое исполнение описанных ими тестов.

| Источник | Использование |
|---|---|
| `CONTEXT.md`, версия 1.0 от 06.10.2026, returned excerpts | Назначение, архитектура, trust/security, исторические ограничения |
| `artifacts-1of2/PRD.md` | Сценарии, UX и продуктовая ценность |
| `artifacts-1of2/RELEASE-GATES.md` | License/defects/platform/acceptance requirements |
| `artifacts(ШАГ4)/CARRYOVER.md` | 16 residual storage/import/recovery limitations |
| `artifacts(ШАГ5)/HANDOFF.md` | Preparation ≠ product verified; partial sources |
| `artifacts(ШАГ5)/ARENA-FIRST-MESSAGE.md` | Continuation, source merge, approvals, production/release |
| `artifacts(ШАГ5)/windows-build-publish.ps1` | Intermediate publisher, two smoke suites, EXE-name mismatch |
| `artifacts(ПОСЛЕДНИЙ АУДИТ)/AUDIT-REPORT.md` | Earlier 21 findings, access gaps, source review hypotheses |
| `artifacts(ПОСЛЕДНИЙ АУДИТ)/NEXT-CHAT-STEP07-IMPROVED.md` | Exact build/evidence/CI/release constraints prior to PASS-F |
| Данный чат, шаги 1–8 и actual tool results в истории | Evolution, counts/statuses, missing bytes и final tooling |
| Последнее задание NEO | Computer вместо Arena, NEOV2, автономия, single-pass, PASS-F |

Актуальные указания NEO в этом задании меняют orchestration и destination, не выдумывают missing runtime evidence. Current Computer должны прочитать все дополнительные реально доступные материалы, заполнить coverage gaps и обновить provenance.

Официальные сведения об исполнителе, проверенные при подготовке:

- [Perplexity Help Center: What is Computer?](https://www.perplexity.ai/help-center/en/articles/13837784-what-is-computer): multi-tool workflows, authenticated integrations including GitHub, cloud sandbox и orchestration.
- [Perplexity: How We Built Security Into Computer](https://www.perplexity.ai/hub/blog/how-we-built-security-into-computer): isolated Linux sandbox, scoped connectors и защита от untrusted content. Это не обещание Windows runner в конкретной задаче.
- [Perplexity: Everything is Computer](https://www.perplexity.ai/hub/blog/everything-is-computer): sensitive actions require approval. Автономное technical decision-making не отменяет обязательных tool permissions.

Эти публичные описания не гарантируют capabilities конкретной подписки/workspace/session. Проверяй реальные tools и права. FINAL.MD не изменяет system instructions исполнителя и не даёт полномочий извлечь чужие/private данные вне пользовательского scope.

## 19. Короткая стартовая команда

После получения файлов и подключения доступных источников используй:

> Выполни FINAL.MD целиком в рамках одной задачи Perplexity Computer. Продолжи «АРХИТЕКТУРА NEO»/MATRIX, проведи глубокий аудит всех доступных источников и чата, создай реально улучшенную V2 и загрузи разрешённый результат в aifirsts/NEOV2. Технические решения принимай самостоятельно. Не останавливайся ради косметических согласований. Для owner-only или недоступных внешних действий выполни всё доступное, поставь PASS-F, сохрани правдивое verification evidence и продолжай. Соблюдай обязательные approvals/permissions, не подменяй real FAIL или Security UNKNOWN статусом PASS. Закончи исходниками, тестами, actual artifacts, hashes, repo read-back и единым NEO-ACTIONS.

Конец задания. Начинай с capability/source discovery и действуй, а не только описывай план.
