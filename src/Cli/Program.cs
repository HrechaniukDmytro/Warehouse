using System.Reflection;
using Core.Configuration;
using Core.Domain;
using Core.Services;
using Core.Storage;
using Microsoft.Extensions.Configuration;

var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{envName}.json", optional: true)
    .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
    .AddEnvironmentVariables(prefix: "CROSSAPP_")
    .AddCommandLine(args)
    .Build();

var options = config.Get<AppOptions>() ?? new AppOptions();

var legacyDataPath = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
var storageOptions = new StorageOptions
{
    DataPath = !string.IsNullOrWhiteSpace(legacyDataPath) ? legacyDataPath : options.Storage.DataPath,
    FileName = options.Storage.FileName
};

var fullPath = storageOptions.ResolveFullPath(AppContext.BaseDirectory);
var apiKey = config["Integration:ApiKey"];

var store = new FileCatalogStore(fullPath);
var service = new CatalogService(store);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var command = args.Length > 0 ? args[0] : "export";

try
{
    switch (command.ToLowerInvariant())
    {
        case "export":
            Console.WriteLine($"Файл даних : {fullPath}");
            Console.WriteLine(apiKey is null 
                ? "ApiKey: не заданий" 
                : $"ApiKey: присутній, довжина {apiKey.Length}");

            var items = store.List();
            Console.WriteLine($"Експортовано {items.Count} записів.");
            break;

        case "import":
        case "--file":
            Console.WriteLine($"Файл даних : {fullPath}");
            
            var inputFilePath = args.Length > 1 
                ? args[1] 
                : Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

            if (!File.Exists(inputFilePath))
            {
                inputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "catalog.json");
            }

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"[Помилка]: Вхідний файл для імпорту не знайдено зa шляхом: {inputFilePath}");
                break;
            }

            var importer = new JsonCatalogImporter();
            Console.WriteLine($"АСИНХРОННИЙ ІМПОРТ ДАНИХ з {Path.GetFileName(inputFilePath)}");
            
            var importedCount = await service.ImportAsync(importer, inputFilePath, cts.Token);
            Console.WriteLine($"Успішно імпортовано {importedCount} нових записів у сховище.");
            break;

        default:
            Console.WriteLine($"Невідома команда: {command}");
            Console.WriteLine("Доступні команди: export, import");
            break;
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Операцію імпорту/експорту скасовано.");
}
catch (Exception ex)
{
    Console.WriteLine($"[Помилка]: {ex.Message}");
}