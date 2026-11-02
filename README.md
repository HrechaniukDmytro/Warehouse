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
 4. **Непорожня одиниця виміру (`Unit`)**: одиниця виміру є обов'язковим текстовим полем (`ArgumentException`, метод `Product.Create`).
 5. **Невід'ємний початковий залишок (`Quantity >= 0`)**: створення товару з від'ємною кількістю неможливе (`ArgumentOutOfRangeException`, метод `Product.Create`).
 6. **Додатна кількість приходу (`amount > 0`)**: під час реєстрації надходження товару кількість має бути строго більшою за нуль (`ArgumentOutOfRangeException`, метод `RegisterArrival`).
 7. **Додатна кількість видачі (`amount > 0`)**: під час списання або відпуску кількість має бути строго більшою за нуль (`ArgumentOutOfRangeException`, метод `Issue`).
 8. **Неможливість від'ємного залишку (`amount <= _quantity`)**: операція видачі не може списати більше одиниць, ніж фактично є в наявності; захищає сутність від переходу в дефіцитний стан (`DomainException`, метод `Issue`).
 ## Архітектурні компоненти
* **Інтерфейс сховища:** `ICatalogStore` (простір імен `Core.Abstractions`) — визначає контракт доступу до даних (`List`, `GetById`, `Add`, `Update`, `Remove`, `Find`).
* **Реалізації сховища:**
  * `InMemoryCatalogStore` (`Core.Storage`) — сховище в оперативній пам'яті на базі `Dictionary<string, Product>` із підтримкою початкових даних (`SampleData`).
  * `FileCatalogStore` (`Core.Storage`) — файлове сховище у форматі JSON (`data/warehouse.json`) із кешуванням у пам'яті та синхронізацією стану (`Flush`).
  * `CachingCatalogStore` (`Core.Storage`) *(додаткове завдання)* — декоратор над `ICatalogStore`, що додає рівень кешування для прискорення читання.
* **Сервісний шар:** `CatalogService` (`Core.Services`) — інкапсулює прикладні бізнес-операції (`Add`, `Receive`, `Issue`, `Search`, `All`, `Find`) і залежить виключно від абстракції `ICatalogStore`.
## Нові компоненти домену та сервісу
* **`CatalogChangeKind`** (`Core.Events`) — перелічування типів подій (`Added`, `Received`, `Issued`, `Removed`).
* **`CatalogChangedEventArgs`** (`Core.Events`) — клас аргументів події (наслідує `EventArgs`), що зберігає незмінні дані про зміну стану (`Id`, `Kind`, `Delta`, `QuantityAfter`, `At`).
* **`CatalogService.Changed`** (`Core.Services`) — подія типу `event EventHandler<CatalogChangedEventArgs>`, що викликається через безпечний метод `OnChanged` (`Changed?.Invoke(...)`) суворо після успішної зміни стану та проходження всіх доменних інваріантів.
* **`CatalogService.FindAll(Func<Product, bool>)`** (`Core.Services`) — метод предикатного пошуку, що приймає делегат `Func` для фільтрації колекції без прив'язки до конкретних критеріїв.

## Підписки та логування у CLI
* **Перший підписник (локальний метод `OnCatalogChanged`):** здійснює форматований вивід операції в консоль та дозапис у файл `logs/app.log`. Демонструє можливість керованої відписки за допомогою оператора `-=`.
* **Другий підписник (анонімна лямбда):** здійснює підрахунок кількості подій через замикання над локальною змінною `changes`.

## Звіти через LINQ

### 1. Реалізовані звіти (`ReportService`)
* **`LowStock`** — фільтрація товарів за лімітом залишку (`Where`), сортування за кількістю та стабільним SKU (`OrderBy().ThenBy()`), проєкція у DTO `StockLine`.
* **`TopByQuantity`** — отримання $N$ найбільших позицій (`OrderByDescending().ThenBy().Take()`).
* **`ByUnit`** — групування за одиницею виміру (`GroupBy`), підрахунок кількості позицій та сумарного залишку в групі (`GroupSummary`).
* **`Totals`** — обчислення агрегатів (загальна кількість, сума, середнє значення `Average`, максимум `Max`) з безпечною обробкою порожньої колекції (повернення кортежу з нулями без винятків).

### 2. Відкладене виконання LINQ (Deferred Execution)
В операторах `Where`, `Select`, `OrderBy`, `GroupBy` запит LINQ не виконується в момент його оголошення. Він зберігає лише дерево виразу/делегат і виконується щоразу заново під час ітерації (`foreach`, `Count()`, `Sum()`, `ToList()`).

У нашому демо-сценарії:
1. Виклик `query.Count()` спочатку повернув `1` (знайдено 1 дефіцитний товар).
2. Після додавання нового товару в `source`, повторний виклик `query.Count()` повернув `2`, оскільки запит заново пройшов по оновленому джерелу.
3. Після зафіксування результату у `snapshot = query.ToList()` та подальшого очищення `source.Clear()`, змінна `snapshot.Count` залишилася рівною `2`, а виклик `query.Count()` повернув `0`.

## Лабораторна робота 8: асинхронність, скасування та тести

Асинхронний імпорт читає `data/catalog.json` через `FileStream` з `useAsync: true` і `JsonSerializer.DeserializeAsync`. Токен скасування передається з CLI до сервісу та імпортера, де перевіряється під час обробки записів. Для скасування під час роботи натисніть `Ctrl+C`.

```cmd
dotnet run --project src/Cli -- --file
dotnet test
```

`--file` зберігає каталог у `data/store.json` поруч із виконуваним CLI; файл із 17 записами для імпорту залишається окремим і не перезаписується сховищем. Повторний імпорт ідемпотентний: уже наявні ідентифікатори пропускаються.

Тести використовують `InMemoryCatalogStore`; тимчасовий JSON-файл створюється лише для перевірки відхилення некоректних доменних даних і видаляється у `finally`. `ArgumentException` означає некоректний аргумент, наприклад від'ємну кількість приходу; `DomainException` — порушення бізнес-правила або некоректні дані імпорту, наприклад спробу видати більше товару, ніж є в залишку. Технічні помилки JSON та файлової системи не маскуються під доменні.

# Лабораторна робота 9: Шляхи, JSON, конфігурація, Secrets

## 1. Профілі конфігурації (Environment Profiles)

Додаток підтримує розділення налаштувань за середовищами виконання (`CROSSAPP_ENVIRONMENT`):

* **Development (`appsettings.Development.json`)**:
  * `Logging:MinimumLevel`: `Debug` (деталізоване логування).
  * Використовується за замовчуванням при локальній розробці.
* **Production (`appsettings.json`)**:
  * `Logging:MinimumLevel`: `Warning` (логування лише важливих подій та помилок).
  * Основне базове середовище.

---

## 2. Таблиця ключів конфігурації

| Ключ конфігурації | Опис | Значення за замовчуванням | Джерело за замовчуванням |
| :--- | :--- | :--- | :--- |
| `EnvironmentName` | Назва поточного середовища | `Development` | `appsettings.json` / `appsettings.Development.json` |
| `Logging:MinimumLevel` | Мінімальний рівень логування | `Information` / `Debug` | `appsettings.json` / `appsettings.Development.json` |
| `Storage:DataPath` | Відносна папка для збереження даних | `data` | `appsettings.json` |
| `Storage:FileName` | Назва файлу бази даних JSON | `store.json` | `appsettings.json` |
| `Integration:ApiKey` | Секретний ключ інтеграції | — | `UserSecrets` / Змінні середовища |

---

## 3. Перевизначення конфігурації через змінні середовища

Усі ключі з конфігурації можна перевизначити через змінні середовища з префіксом `CROSSAPP_`. Вкладені двоеточки (`:`) замінюються на подвійне підкреслення (`__`).

### PowerShell (Windows)
```powershell
# Зміна середовища виконання
$env:CROSSAPP_ENVIRONMENT = "Production"

# Перевизначення шляху до даних (CROSSAPP_DATA_PATH)
$env:CROSSAPP_DATA_PATH = "custom_data"

# Перевизначення секрету ApiKey (Integration:ApiKey -> CROSSAPP_Integration__ApiKey)
$env:CROSSAPP_Integration__ApiKey = "secret-key-from-powershell"

# Запуск проєкту
dotnet run --project src/Cli -- export

# Очищення змінних середовища після перевірки
Remove-Item Env:\CROSSAPP_ENVIRONMENT
Remove-Item Env:\CROSSAPP_DATA_PATH
Remove-Item Env:\CROSSAPP_Integration__ApiKey