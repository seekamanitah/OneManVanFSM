namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IProductService
{
    Task<List<ProductListItem>> GetProductsAsync(ProductFilter? filter = null);
    Task<ProductDetail?> GetProductAsync(int id);
    Task<Product> CreateProductAsync(ProductEditModel model);
    Task<Product> UpdateProductAsync(int id, ProductEditModel model);
    Task<bool> ArchiveProductAsync(int id);
    Task<bool> RestoreProductAsync(int id);
    Task<bool> DeleteProductPermanentlyAsync(int id);
    Task<int> BulkArchiveProductsAsync(List<int> ids);
    Task<int> BulkRestoreProductsAsync(List<int> ids);
    Task<int> BulkDeleteProductsPermanentlyAsync(List<int> ids);
}

public class ProductFilter
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
    public bool ShowArchived { get; set; }
}

public class ProductListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? ModelNumber { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public string? EquipmentType { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MSRP { get; set; }
    public decimal MarkupPercent { get; set; }
    public bool TaxIncludedInPrice { get; set; }
    public string? Unit { get; set; }
    public string? SupplierName { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsActive { get; set; }
    public bool IsDiscontinued { get; set; }
    public int InventoryCount { get; set; }
}

public class ProductEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Manufacturer is required.")]
    public string? Brand { get; set; }
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Model Number is required.")]
    public string? ModelNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProductNumber { get; set; }
    public string? PartNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public string? EquipmentType { get; set; }
    public string? FuelType { get; set; }
    public string? Description { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MSRP { get; set; }
    public decimal MarkupPercent { get; set; }
    public bool TaxIncludedInPrice { get; set; }
    public string? Unit { get; set; }
    public string? Specs { get; set; }
    public string? SupplierName { get; set; }
    public string? Tonnage { get; set; }
    public string? RefrigerantType { get; set; }
    public string? SEERRating { get; set; }
    public string? AFUERating { get; set; }
    public string? Voltage { get; set; }
    public int LaborWarrantyYears { get; set; }
    public int PartsWarrantyYears { get; set; }
    public int CompressorWarrantyYears { get; set; }
    public bool RegistrationRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDiscontinued { get; set; }
    public bool IsTemplate { get; set; } = true;
    public string? Notes { get; set; }
}

public class ProductDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? ModelNumber { get; set; }
    public string? ProductNumber { get; set; }
    public string? PartNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public string? EquipmentType { get; set; }
    public string? FuelType { get; set; }
    public string? Description { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MSRP { get; set; }
    public decimal MarkupPercent { get; set; }
    public bool TaxIncludedInPrice { get; set; }
    public string? Unit { get; set; }
    public string? Specs { get; set; }
    public string? SupplierName { get; set; }
    public string? Tonnage { get; set; }
    public string? RefrigerantType { get; set; }
    public string? SEERRating { get; set; }
    public string? AFUERating { get; set; }
    public string? Voltage { get; set; }
    public int LaborWarrantyYears { get; set; }
    public int PartsWarrantyYears { get; set; }
    public int CompressorWarrantyYears { get; set; }
    public bool RegistrationRequired { get; set; }
    public bool IsActive { get; set; }
    public bool IsDiscontinued { get; set; }
    public bool IsTemplate { get; set; }
    public string? Notes { get; set; }
    public int InventoryCount { get; set; }
    public int AssetCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
