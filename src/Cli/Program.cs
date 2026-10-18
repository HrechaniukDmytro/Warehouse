using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
ICatalogStore store = useFile
    ? new FileCatalogStore(dataPath)
    : new InMemoryCatalogStore(SampleData.Products());

var service = new CatalogService(store);

var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "demo";

switch (command)
{
    case "demo":
        RunDeferredExecutionDemo(service);
        break;

    case "report-top":
        Console.WriteLine("ТОП-5 ТОВАРІВ ЗА ЗАЛИШКОМ");
        PrintLines(ReportService.TopByQuantity(service.All(), 5));
        break;

    case "report-low":
        Console.WriteLine("ДЕФІЦИТНІ ТОВАРИ (ЗАЛИШОК < 10)");
        PrintLines(ReportService.LowStock(service.All(), 10));
        break;

    case "report-groups":
        Console.WriteLine("ЗВЕДЕННЯ ЗА ОДИНИЦЯМИ ВИМІРУ");
        foreach (var g in ReportService.ByUnit(service.All()))
        {
            Console.WriteLine($" Одиниця: {g.Key,-6} | Позицій: {g.Count,3} | Всього: {g.TotalQuantity,7}");
        }
        break;

    case "report-sum":
        Console.WriteLine("ЗАГАЛЬНА СТАТИСТИКА КАТАЛОГУ");
        var t = ReportService.Totals(service.All());
        Console.WriteLine($" Всього позицій : {t.Items}");
        Console.WriteLine($" Загальна кількість: {t.Total}");
        Console.WriteLine($" Середній залишок : {t.Average:F2}");
        Console.WriteLine($" Максимальний     : {t.Max}");
        break;

    default:
        Console.WriteLine("Невідома команда. Доступні: report-top | report-low | report-groups | report-sum");
        break;
}

static void PrintLines(IReadOnlyList<StockLine> lines)
{
    if (lines.Count == 0)
    {
        Console.WriteLine(" (записи відсутні)");
        return;
    }

    foreach (var line in lines)
    {
        Console.WriteLine($" {line.Sku,-10} {line.Name,-26} {line.Quantity,6} {line.Unit}");
    }
}

static void RunDeferredExecutionDemo(CatalogService service)
{
    Console.WriteLine("ДЕМОНСТРАЦІЯ ВІДКЛАДЕНОГО ВИКОНАННЯ (LINQ)");

    var source = service.All().ToList();

    var query = source.Where(p => p.Quantity < 10);
    Console.WriteLine($"1. Дефіцитних до зміни джерела: {query.Count()}");

    source.Add(Product.Create("tmp001", "SKU-999", "Тимчасова скоба", "шт", 3));

    Console.WriteLine($"2. Дефіцитних після додавання запису: {query.Count()}");

    var snapshot = query.ToList();

    source.Clear();

    Console.WriteLine($"3. У фіксованому знімку (snapshot) залишилось: {snapshot.Count}");
    Console.WriteLine($"4. Повторний виклик query.Count() на очищеному джерелі: {query.Count()}");
    Console.WriteLine("\nДля виклику звітів використовуйте:");
    Console.WriteLine("  dotnet run --project src/Cli -- report-top");
    Console.WriteLine("  dotnet run --project src/Cli -- report-low");
    Console.WriteLine("  dotnet run --project src/Cli -- report-groups");
    Console.WriteLine("  dotnet run --project src/Cli -- report-sum");
}