using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл не знайдено: {path}", path);

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        List<ProductDto>? dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<ProductDto>>(stream, options, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Файл {Path.GetFileName(path)} має некоректний формат JSON.", ex);
        }

        if (dtos is null || dtos.Count == 0)
            throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                result.Add(Product.FromDto(dtos[i]));
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }
        }

        return result;
    }
}