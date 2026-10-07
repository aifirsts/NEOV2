# NEO-ACTIONS — Действия владельца

## PASS-F: Требуют участия NEO

### 1. Windows Build/EXE
- Действие: dotnet publish на ПК NEO с .NET 8 SDK
- Команда: dotnet publish src/MATRIX.App/MATRIX.App.csproj -c Release -r win-x64 --self-contained
- Ожидаемый результат: MATRIX.exe в publish/

### 2. CI Активация
- Действие: Push в GitHub для запуска CI workflows
- Ожидаемый результат: Green build-test.yml

### 3. Приёмка EXE
- Действие: Запустить MATRIX.exe на ПК NEO как обычный пользователь
- Проверить: Запуск без SDK/admin, данные в %LOCALAPPDATA%\MATRIX
- Ожидаемый результат: UI загружается, CRUD работает

### 4. V1→V2 Миграция
- Действие: Экспорт V1 workspace → импорт в V2
- Проверить: Counts совпадают, SHA-256 подтверждён

## Как стать PASS

Для каждого действия: выполнить сценарий, добавить результат и evidence.
