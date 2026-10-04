using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

ICatalogStore store = useFile
    ? new FileCatalogStore(dataPath)
    : new InMemoryCatalogStore(SampleData.Products());

var service = new CatalogService(store);

Console.WriteLine($"=== Сховище: {store.GetType().Name} ===");

var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
Console.WriteLine($"Створено товар: {created}");

service.Receive(created.Id, 25);
Console.WriteLine($"Після приходу (+25): {service.Find(created.Id)}");

service.Issue(created.Id, 10);
Console.WriteLine($"Після видачі (-10): {service.Find(created.Id)}");

Console.WriteLine($"\nУсього записів у каталозі: {service.All().Count}");
foreach (var p in service.All().Take(5))
{
    Console.WriteLine($" {p.Id,-8} {p.Sku,-10} {p.Name,-24} {p.Quantity,6} {p.Unit}");
}

Console.WriteLine("\n=== Сценарії відмов ===");
TryDo("Прихід для неіснуючого товару", () => service.Receive("NON-EXISTENT-ID", 10));
TryDo("Спроба додати дублікат ID прямо у сховище", () => store.Add(created));
TryDo("Видача більшої кількості, ніж є на залишку", () => service.Issue(created.Id, 9999));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" [FAIL] {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" [OK] {title}: {ex.GetType().Name} -> {ex.Message}");
    }
}