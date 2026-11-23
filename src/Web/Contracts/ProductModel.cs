using System.ComponentModel.DataAnnotations;

namespace Web.Contracts;

public sealed class ProductModel
{
    public string Id { get; set; } = "";

    [Required(ErrorMessage = "Артикул обов'язковий.")]
    [StringLength(32, ErrorMessage = "Артикул має бути до 32 символів.")]
    public string Sku { get; set; } = "";

    [Required(ErrorMessage = "Назва обов'язкова.")]
    [StringLength(200, ErrorMessage = "Назва має бути до 200 символів.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Одиниця виміру обов'язкова.")]
    [StringLength(20, ErrorMessage = "Одиниця виміру має бути до 20 символів.")]
    public string Unit { get; set; } = "";

    [Range(0, int.MaxValue, ErrorMessage = "Кількість не може бути від'ємною.")]
    public int Quantity { get; set; }
}

public sealed record ApiResult(
    bool Ok,
    string? Message = null,
    Dictionary<string, string[]>? Errors = null);

public sealed record ValidationProblemResponse(
    string? Title,
    string? Detail,
    Dictionary<string, string[]>? Errors);

public sealed record PagedProductsResponse(List<ProductModel> Items, int Total);