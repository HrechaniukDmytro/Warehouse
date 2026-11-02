using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileCatalogStore : ICatalogStore
{
    private readonly string _path;
    private readonly List<Product> _items = new();
    private bool _loaded;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public FileCatalogStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (!File.Exists(_path))
        {
            _items.Clear();
            _loaded = true;
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            if (string.IsNullOrWhiteSpace(json))
            {
                _items.Clear();
                _loaded = true;
                return;
            }

            var dtos = JsonSerializer.Deserialize<List<ProductDto>>(json, Options);
            _items.Clear();

            if (dtos != null)
            {
                foreach (var dto in dtos)
                {
                    if (dto is null || string.IsNullOrWhiteSpace(dto.Id))
                        continue;

                    var product = Product.FromDto(dto);
                    _items.Add(product);
                }
            }

            _loaded = true;
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Файл за шляхом {_path} містить некоректний JSON.", ex);
        }
    }

    public void Add(Product product)
    {
        EnsureLoaded();
        ArgumentNullException.ThrowIfNull(product);

        if (GetById(product.Id) is not null)
            throw new DomainException($"Товар з ID {product.Id} вже існує.");

        _items.Add(product);
        Save();
    }

    public void Update(Product product)
    {
        EnsureLoaded();
        ArgumentNullException.ThrowIfNull(product);

        var index = _items.FindIndex(p => p.Id == product.Id);
        if (index < 0)
            throw new DomainException($"Товар з ID {product.Id} не знайдено.");

        _items[index] = product;
        Save();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        var index = _items.FindIndex(p => p.Id == id);
        if (index < 0) return false;

        _items.RemoveAt(index);
        Save();
        return true;
    }

    public Product? GetById(string id)
    {
        EnsureLoaded();
        return _items.FirstOrDefault(p => p.Id == id);
    }

    public IReadOnlyList<Product> List()
    {
        EnsureLoaded();
        return _items.AsReadOnly();
    }

    public IReadOnlyList<Product> Find(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureLoaded();
        return _items.Where(predicate).ToList().AsReadOnly();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dtos = _items.Select(p => p.ToDto()).ToList();
        var tmpPath = _path + ".tmp";

        // Атомарний запис через .tmp
        File.WriteAllText(tmpPath, JsonSerializer.Serialize(dtos, Options));
        File.Move(tmpPath, _path, overwrite: true);
    }
}