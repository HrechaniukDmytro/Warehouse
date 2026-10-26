using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("\nОтримано сигнал скасування (Ctrl+C)...");
    e.Cancel = true;
    cts.Cancel();
};

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

ICatalogStore store = useFile
    ? new FileCatalogStore(Path.Combine(AppContext.BaseDirectory, "data", "store.json"))
    : new InMemoryCatalogStore();

var service = new CatalogService(store);

try
{
    Console.WriteLine("АСИНХРОННИЙ ІМПОРТ ДАНИХ");
    var added = await service.ImportAsync(new JsonCatalogImporter(), dataPath, cts.Token);
    Console.WriteLine($"Імпортовано записів: {added}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Імпорт скасовано.");
}
catch (DomainException ex)
{
    Console.WriteLine($"Дані відхилено: {ex.Message}");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Файл не знайдено: {ex.FileName}");
}