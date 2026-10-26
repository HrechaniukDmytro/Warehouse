using Core.Domain;

namespace Core.Tests;

public static class TestData
{
    public static IReadOnlyList<Product> Items() =>
    [
        Product.Create("P-1", "SKU-MID", "Товар 1", "шт", 50),
        Product.Create("P-2", "SKU-BIG", "Товар 2", "кг", 100),
        Product.Create("P-3", "SKU-LOW", "Товар 3", "шт", 5)
    ];
}