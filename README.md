# Warehouse
 Наскрізний проєкт з крос-платформного програмування.
 Предметна область: Склад. Сутності: Product, StockBatch, Warehouse, Movement.
 Призначення: облік залишків товарів по партіях.
 ## Запуск
 dotnet build
 dotnet run --project src/Cli
 .\src\Cli\bin\Release\net8.0\win-x64\publish\Cli.exe
 ## Середовище
 .NET SDK 8.0, Windows 11 x64
 ## Порівняння режимів публікації
 | RID | Режим | Розмір каталогу | Чи потрібен встановлений runtime? |
 | :--- | :--- | :--- | :--- |
 | `win-x64` | Self-contained  | 70.5 MB | Ні |
 | `win-x64` | Framework-dependent  | 0.18 MB | Так (потрібен встановлений .NET 8) |
 ## Структура Solution
 - `src/Core/` — базова бізнес-логіка.
 - `src/Cli/` — консольний інтерфейс користувача.