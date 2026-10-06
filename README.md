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
 ## Список інваріантів
 1. **Обов'язковий ідентифікатор (`Id`)**: ідентифікатор товару не може бути порожнім, містити лише пробіли або мати значення `null` (`ArgumentException`, метод `Product.Create`).
 2. **Обов'язковий артикул (`Sku`)**: артикул товару не може бути порожнім, нормалізується (обрізаються пробіли та переводиться у верхній регістр) (`ArgumentException`, метод `Product.Create`).
 3. **Непорожня назва (`Name`)**: найменування товару є обов'язковим текстовим полем (`ArgumentException`, метод `Product.Create`).
 4. **Невід'ємний початковий залишок (`Quantity >= 0`)**: створення товару з від'ємною кількістю неможливе (`ArgumentOutOfRangeException`, метод `Product.Create`).
 5. **Додатна кількість приходу (`amount > 0`)**: під час реєстрації надходження товару кількість має бути строго більшою за нуль (`ArgumentOutOfRangeException`, метод `RegisterArrival`).
 6. **Додатна кількість видачі (`amount > 0`)**: під час списання або відпуску кількість має бути строго більшою за нуль (`ArgumentOutOfRangeException`, метод `Issue`).
 7. **Неможливість від'ємного залишку (`amount <= _quantity`)**: операція видачі не може списати більше одиниць, ніж фактично є в наявності; захищає сутність від переходу в дефіцитний стан (`InvalidOperationException`, метод `Issue`).
 ## Архітектурні компоненти
* **Інтерфейс сховища:** `ICatalogStore` (простір імен `Core.Abstractions`) — визначає контракт доступу до даних (`List`, `GetById`, `Add`, `Update`, `Remove`, `Find`).
* **Реалізації сховища:**
  * `InMemoryCatalogStore` (`Core.Storage`) — сховище в оперативній пам'яті на базі `Dictionary<string, Product>` із підтримкою початкових даних (`SampleData`).
  * `FileCatalogStore` (`Core.Storage`) — файлове сховище у форматі JSON (`data/catalog.json`) із кешуванням у пам'яті та синхронізацією стану (`Flush`).
  * `CachingCatalogStore` (`Core.Storage`) *(додаткове завдання)* — декоратор над `ICatalogStore`, що додає рівень кешування для прискорення читання.
* **Сервісний шар:** `CatalogService` (`Core.Services`) — інкапсулює прикладні бізнес-операції (`Add`, `Receive`, `Issue`, `Search`, `All`, `Find`) і залежить виключно від абстракції `ICatalogStore`.