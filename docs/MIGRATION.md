# MIGRATION — V1 to V2

## Совместимость

V2 сохраняет workspace schema v1. Wire contract не изменён.

## Сухой прогон (Dry-run)

1. Экспортируйте текущий workspace (File → Export)
2. Проверьте структуру JSON
3. Импортируйте в V2 (File → Import)
4. Проверьте preview: counts, SHA-256
5. Подтвердите импорт

## Резервное копирование

Перед миграцией скопируйте %LOCALAPPDATA%\MATRIX в backup.

## Откат

1. Остановите MATRIX
2. Восстановите из generations/ или backup
3. Обновите CURRENT pointer
4. Запустите MATRIX
