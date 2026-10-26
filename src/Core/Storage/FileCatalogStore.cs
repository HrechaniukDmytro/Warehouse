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

    public FileCatalogStore(string path)
    {
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

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var dtos = JsonSerializer.Deserialize<List<ProductDto>>(json, options);
            _items.Clear();

            if (dtos != null)
            {
                foreach (var dto in dtos)
                {
                    if (dto != null)
                    {
                        var product = Product.FromDto(dto);
                        _items.Add(product);
                    }
                }
            }

            _loaded = true;
        }
        catch (Exception)
        {
            _items.Clear();
            _loaded = true;
        }
    }

    public void Add(Product product)
    {
        EnsureLoaded();
        ArgumentNullException.ThrowIfNull(product);

        if (GetById(product.Id) is not null)
            throw new InvalidOperationException($"Товар з ID {product.Id} вже існує.");

        _items.Add(product);
        Save();
    }

    public void Update(Product product)
    {
        EnsureLoaded();
        ArgumentNullException.ThrowIfNull(product);

        var index = _items.FindIndex(p => p.Id == product.Id);
        if (index < 0)
            throw new KeyNotFoundException($"Товар з ID {product.Id} не знайдено.");

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

    public IReadOnlyList<Product> All()
    {
        EnsureLoaded();
        return _items.AsReadOnly();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dtos = _items.Select(p => p.ToDto()).ToList();
        var json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }

    public IReadOnlyList<Product> List()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<Product> Find(Func<Product, bool> predicate)
    {
        throw new NotImplementedException();
    }
}