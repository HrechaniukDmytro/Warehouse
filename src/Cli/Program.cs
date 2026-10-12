using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
ICatalogStore store = useFile
    ? new FileCatalogStore(dataPath)
    : new InMemoryCatalogStore(SampleData.Products());

var service = new CatalogService(store);
Console.WriteLine($"Сховище: {store.GetType().Name}\n");

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
string? logDir = Path.GetDirectoryName(logPath);
if (!string.IsNullOrEmpty(logDir))
{
    Directory.CreateDirectory(logDir);
}

void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
{
    var line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} -> залишок {e.QuantityAfter}";
    Console.WriteLine($" [LOG] {line}");
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnCatalogChanged;

int changes = 0;
service.Changed += (_, _) => changes++;

Console.WriteLine("Сценарій 1: Успішні операції з підпискою");
var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
service.Receive(created.Id, 25);
Console.WriteLine($"Подій отримано (лічильник changes): {changes}\n");

Console.WriteLine("Відписка OnCatalogChanged (-=)");
service.Changed -= OnCatalogChanged;

Console.WriteLine("Виконуємо Receive (+5) після відписки першого слухача:");
service.Receive(created.Id, 5); 
Console.WriteLine($"Подій отримано (лічильник changes): {changes}\n");

Console.WriteLine("Сценарій 2: Відмова інваріанту (подія НЕ має спрацювати)");
int changesBeforeError = changes;
try
{
    service.Receive(created.Id, -10); 
}
catch (Exception ex)
{
    Console.WriteLine($" [Перехоплено виняток]: {ex.GetType().Name} -> {ex.Message}");
}
Console.WriteLine($"Подій після помилки: {changes} (змін лічильника немає: {changes == changesBeforeError})\n");

Console.WriteLine("Сценарій 3: Пошук за допомогою Func<Product, bool> (залишок < 100)");
var lowStock = service.FindAll(p => p.Quantity < 100);
foreach (var p in lowStock)
{
    Console.WriteLine($" {p.Id,-8} {p.Sku,-10} {p.Name,-24} {p.Quantity,5} {p.Unit}");
}