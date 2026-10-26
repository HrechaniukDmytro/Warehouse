using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class CatalogServiceTests
{
    [Fact]
    public void Receive_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-1", "Кабель", "м", 10);

        Assert.Throws<DomainException>(() => service.Issue(p.Id, 20));
        Assert.Equal(10, store.GetById(p.Id)!.Quantity);
    }

    [Fact]
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("SKU-2", "Гвинт", "шт", 7);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(1, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expected)
    {
        var count = Reports.ReportService.LowStock(TestData.Items(), threshold).Count;
        Assert.Equal(expected, count);
    }

    [Fact]
    public void TopByQuantity_IsDeterministic()
    {
        var top = Reports.ReportService.TopByQuantity(TestData.Items(), 2).Select(l => l.Sku);
        Assert.Equal(new[] { "SKU-BIG", "SKU-MID" }, top);
    }

    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var service = new CatalogService(new InMemoryCatalogStore());

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.ImportAsync(new JsonCatalogImporter(), tempFile, cts.Token));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImportAsync_WithInvalidData_ThrowsDomainException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var invalidContent = "[{\"id\":\"P1\",\"sku\":\"\",\"name\":\"Defect\",\"unit\":\"pcs\",\"quantity\":-10}]";
            await File.WriteAllTextAsync(tempFile, invalidContent);

            var service = new CatalogService(new InMemoryCatalogStore());

            await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonCatalogImporter(), tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}