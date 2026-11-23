using System.Net;
using System.Net.Http.Json;
using Web.Contracts;

namespace Web.Services;

public sealed class ProductsApiClient(HttpClient http)
{
    public async Task<List<ProductModel>> ListAsync(
        string? search = null,
        string sortBy = "sku",
        CancellationToken ct = default)
    {
        var query = $"api/products?page=1&pageSize=100&sortBy={Uri.EscapeDataString(sortBy)}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search)}";
        }

        var paged = await http.GetFromJsonAsync<PagedProductsResponse>(query, ct);
        return paged?.Items ?? [];
    }

    public async Task<ProductModel?> FindAsync(string id, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"api/products/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductModel>(ct);
    }

    public Task<ApiResult> CreateAsync(ProductModel model, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, "api/products", model, ct);

    public Task<ApiResult> UpdateAsync(ProductModel model, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"api/products/{model.Id}", model, ct);

    public async Task<ApiResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.DeleteAsync($"api/products/{id}", ct);
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult(true);
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ApiResult(false, "Запис вже було видалено.");
            }
            return new ApiResult(false, $"Помилка видалення: {(int)response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return new ApiResult(false, "API недоступний. Перевірте, чи запущено src/Api.");
        }
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, ProductModel model, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(model)
            };
            using var response = await http.SendAsync(request, ct);

            if (response.IsSuccessStatusCode) return new ApiResult(true);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new ApiResult(false, "Запис не знайдено — можливо, його вже видалили.");

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var conflict = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>(ct);
                return new ApiResult(false, conflict?.Detail ?? "Конфлікт даних (артикул уже існує).");
            }

            if (response.StatusCode != HttpStatusCode.BadRequest)
                return new ApiResult(false, $"Сервер повернув помилку {(int)response.StatusCode}.");

            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>(ct);
            return new ApiResult(false, problem?.Title ?? "Дані не пройшли перевірку.", problem?.Errors);
        }
        catch (HttpRequestException)
        {
            return new ApiResult(false, "API недоступний. Перевірте, чи запущено src/Api.");
        }
    }
}