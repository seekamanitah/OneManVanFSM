using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileProductService
{
    Task<List<MobileProductCard>> GetProductsAsync(MobileProductFilter? filter = null);
    Task<MobileProductStats> GetStatsAsync();
}

public class MobileProductCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? PartNumber { get; set; }
    public string? Unit { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MarkupPercent { get; set; }
    public int StockCount { get; set; }
    public int AssetCount { get; set; }
}

public class MobileProductFilter
{
    public string? Search { get; set; }
    public string? Category { get; set; }
}

public class MobileProductStats
{
    public int TotalProducts { get; set; }
    public int Categories { get; set; }
    public int InStockProducts { get; set; }
    public decimal AvgMarkup { get; set; }
}
