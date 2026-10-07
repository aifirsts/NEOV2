# STORAGE-PROTOCOL — MATRIX V2

## Путь

%LOCALAPPDATA%\MATRIX

## Структура

- catalog.json — активный каталог
- state.json — активное состояние
- settings.json — настройки
- CURRENT — указатель на активную generation
- .lease — cooperative lease
- generations/ — снимки поколений

## Протокол записи

1. Acquire cooperative lease
2. Serialize workspace to JSON
3. Write to generations/<id>/
4. Flush to disk
5. Reread and validate
6. Compute SHA-256
7. Write generation metadata
8. Publish: copy to main files
9. Update CURRENT pointer
10. Release lease

## Recovery

- Enumerate generations/ candidates
- Validate each (JSON parse, hash verification)
- Preview without auto-reset
- Restore only to new approved destination
- Source untouched

## Ограничения

- Нет гарантии атомарности пары File.Replace
- Нет гарантии power-loss durability
- Нет защиты от всех path races
- Нет защиты от hostile editors
