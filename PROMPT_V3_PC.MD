# PROMPT_V3_PC.MD
# Perplexity Computer — MATRIX V3: финальная готовая версия

Версия задания: 3.0 · Дата: 7 октября 2026 · Владелец: NEO.
Исполнитель: Perplexity Computer.
Целевой репозиторий: [aifirsts/NEOV2](https://github.com/aifirsts/NEOV2).

## Главная команда

Прочитай этот документ целиком и выполни его в рамках одной задачи Perplexity Computer. Создай и опубликуй релиз V3 — финальную версию MATRIX, полностью готовую к скачиванию и установке на ПК NEO.

## Контекст

MATRIX V2 уже реализован и опубликован в [aifirsts/NEOV2](https://github.com/aifirsts/NEOV2):
- 5 C# проектов (Core, Application, Storage, Import, App) + 7 тестовых проектов
- WPF UI (MVVM: дерево, граф, карточка, события)
- Generation-based storage с SHA-256, CURRENT pointer, cooperative lease
- Import/Export с preview → confirmation → commit
- 50 Python тестов (T01-T28 + schema/fixture validation)
- CI Build and Test PASSED на windows-latest
- Release v2.0.0-preview.1 (prerelease)

Актуальный commit: проверь через GitHub API перед началом.

## Что такое V3

V3 — финальная версия, полностью готовая к работе на ПК NEO:
- Реальный self-contained Windows EXE, собранный в CI
- Все тесты проходят (C# + Python)
- UI полностью функционален: CRUD, поиск, фильтры, граф
- Storage работает: сохранение, загрузка, generations, recovery
- Import/Export работает: preview, commit, safe export
- Документация актуальна и полна
- Release загружен на GitHub с реальным EXE asset

## Задачи V3

### 1. Проверь и исправь текущее состояние
- Клонируй репозиторий
- Проверь CI: Build and Test должен быть green
- Если есть ошибки — исправь их
- Убедись что все C# тесты проходят
- Убедись что Python тесты проходят

### 2. Улучши UI/UX
- Добавь реальные typed формы для редактирования Node, Project, Task, Control, Evidence, Finding, Runbook, UserStatement
- Реализуй графическую визуализацию связей (Canvas с Node и Edge отрисовкой)
- Добавь поиск и фильтры по дереву
- Добавь счётчики visible/total/hidden
- Добавь keyboard navigation
- Добавь empty/loading/error/locked states

### 3. Улучши Storage
- Добавь recovery UI: enumerate generations, preview, restore
- Добавь settings management
- Убедись что backup работает корректно
- Добавь dry-run migration

### 4. Сборка и публикация
- Собери self-contained win-x64 EXE в CI
- Создай GitHub Release v3.0.0 с реальным EXE asset
- Включи SHA256SUMS.txt, VERSION.json, документацию
- Release notes на русском языке

### 5. Документация
- Обнови README.md с актуальным статусом
- Обнови CHANGELOG.md с изменениями V2→V3
- Обнови audit/STATUS.json с gates V3
- Создай audit/FINAL-REPORT-V3.md

## Технические решения

- .NET 8 LTS (уже выбран в ADR-V2-SDK-001)
- WPF/MVVM (уже реализован)
- xUnit для C# тестов
- Python для schema/domain validation тестов
- GitHub Actions для CI/CD
- Self-contained win-x64 publish

## GitHub

- Репозиторий: https://github.com/aifirsts/NEOV2
- Ветка: main
- Токен: используй предоставленный пользователем токен (если применимо)
- Создай tag v3.0.0 и Release v3.0.0
- Загрузи MATRIX.exe как release asset

## PASS-F

Для действий, требующих участия NEO (например, запуск на реальном ПК), используй PASS-F:
- Выполни все доступные проверки
- Зафиксируй точную будущую проверку NEO
- Не останавливай остальные работы

## Формат ответа

1. Что создано, какая версия, что загружено
2. Ссылки на repo/branch/commit/PR/release
3. Ключевые улучшения V2→V3
4. Результаты тестов (counts, environment)
5. PASS-F таблица
6. Где лежат README, FINAL-REPORT, NEO-ACTIONS
7. Ограничения и truthful статус

Конец задания. Начинай немедленно.
