using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class MaterialListService : IMaterialListService
{
    private readonly AppDbContext _db;
    public MaterialListService(AppDbContext db) => _db = db;

    public async Task<List<MaterialListListItem>> GetListsAsync(MaterialListFilter? filter = null)
    {
        var query = _db.MaterialLists.Where(m => (filter != null && filter.ShowArchived) ? m.IsArchived : !m.IsArchived).AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(m => m.Name.ToLower().Contains(term));
            }
            if (filter.IsTemplate.HasValue) query = query.Where(m => m.IsTemplate == filter.IsTemplate.Value);
            if (filter.Status.HasValue) query = query.Where(m => m.Status == filter.Status.Value);
            if (!string.IsNullOrWhiteSpace(filter.TradeType)) query = query.Where(m => m.TradeType == filter.TradeType);
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDescending ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
                "total" => filter.SortDescending ? query.OrderByDescending(m => m.Total) : query.OrderBy(m => m.Total),
                "status" => filter.SortDescending ? query.OrderByDescending(m => m.Status) : query.OrderBy(m => m.Status),
                _ => filter.SortDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt)
            };
        }
        else query = query.OrderByDescending(m => m.CreatedAt);

        return await query.Select(m => new MaterialListListItem
        {
            Id = m.Id, Name = m.Name, IsTemplate = m.IsTemplate,
            Status = m.Status, TradeType = m.TradeType,
            PricingMethod = m.PricingMethod,
            TotalMaterialCost = m.Subtotal, TotalLaborCost = 0,
            GrandTotal = m.Total, ItemCount = m.Items.Count,
            CustomerName = m.Customer != null ? m.Customer.Name : null,
            SiteName = m.Site != null ? m.Site.Name : null,
            JobNumber = m.Job != null ? m.Job.JobNumber : null,
            CreatedAt = m.CreatedAt
        }).ToListAsync();
    }

    public async Task<MaterialListDetail?> GetListAsync(int id)
    {
        var m = await _db.MaterialLists
            .Include(ml => ml.Job)
            .Include(ml => ml.Customer)
            .Include(ml => ml.Site)
            .Include(ml => ml.Items).ThenInclude(i => i.Product)
            .Include(ml => ml.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(ml => ml.Id == id && !ml.IsArchived);

        if (m is null) return null;

        var siteAddr = m.Site is not null
            ? string.Join(", ", new[] { m.Site.Address, m.Site.City, m.Site.State, m.Site.Zip }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : null;
        var custAddr = m.Customer is not null
            ? string.Join(", ", new[] { m.Customer.Address, m.Customer.City, m.Customer.State, m.Customer.Zip }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : null;

        return new MaterialListDetail
        {
            Id = m.Id, Name = m.Name, IsTemplate = m.IsTemplate,
            Status = m.Status, TradeType = m.TradeType,
            PricingMethod = m.PricingMethod,
            TotalMaterialCost = m.Subtotal, TotalLaborCost = 0,
            GrandTotal = m.Total,
            MarkupPercent = m.MarkupPercent, TaxPercent = m.TaxPercent,
            ContingencyPercent = m.ContingencyPercent,
            Notes = m.Notes,
            InternalNotes = m.InternalNotes,
            ExternalNotes = m.ExternalNotes,
            PONumber = m.PONumber,
            JobId = m.JobId,
            JobNumber = m.Job?.JobNumber,
            CustomerId = m.CustomerId,
            CustomerName = m.Customer?.Name,
            CustomerAddress = custAddr,
            SiteId = m.SiteId,
            SiteName = m.Site?.Name,
            SiteAddress = siteAddr,
            CreatedAt = m.CreatedAt, UpdatedAt = m.UpdatedAt,
            Items = m.Items.OrderBy(i => i.SortOrder).Select(i => new MaterialListItemDto
            {
                Id = i.Id, Section = i.Section, ItemName = i.ItemName,
                Quantity = i.Quantity, Unit = i.Unit, BaseCost = i.BaseCost,
                LaborHours = i.LaborHours ?? 0m, FlatPrice = i.FlatPrice ?? 0m,
                MarkupPercent = i.MarkupPercent, SortOrder = i.SortOrder,
                ProductId = i.ProductId, ProductName = i.Product?.Name,
                InventoryItemId = i.InventoryItemId,
                InventoryStockQty = i.InventoryItem?.Quantity,
                Notes = i.Notes
            }).ToList()
        };
    }

    public async Task<MaterialList> CreateListAsync(MaterialListEditModel model)
    {
        var list = new MaterialList
        {
            Name = model.Name, IsTemplate = model.IsTemplate,
            Status = model.Status, TradeType = model.TradeType,
            PricingMethod = model.PricingMethod, Notes = model.Notes,
            InternalNotes = model.InternalNotes, ExternalNotes = model.ExternalNotes,
            PONumber = model.PONumber,
            JobId = model.JobId, CustomerId = model.CustomerId, SiteId = model.SiteId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.MaterialLists.Add(list);
        await _db.SaveChangesAsync();
        return list;
    }

    public async Task<MaterialList> UpdateListAsync(int id, MaterialListEditModel model)
    {
        var list = await _db.MaterialLists.FindAsync(id) ?? throw new InvalidOperationException("Material list not found.");
        list.Name = model.Name; list.IsTemplate = model.IsTemplate;
        list.Status = model.Status; list.TradeType = model.TradeType;
        list.PricingMethod = model.PricingMethod; list.Notes = model.Notes;
        list.InternalNotes = model.InternalNotes; list.ExternalNotes = model.ExternalNotes;
        list.PONumber = model.PONumber;
        list.JobId = model.JobId; list.CustomerId = model.CustomerId; list.SiteId = model.SiteId;
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return list;
    }

    public async Task<bool> ArchiveListAsync(int id)
    {
        var list = await _db.MaterialLists.FindAsync(id);
        if (list is null) return false;
        list.IsArchived = true; list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreListAsync(int id)
    {
        var list = await _db.MaterialLists.FindAsync(id);
        if (list is null) return false;
        list.IsArchived = false; list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteListPermanentlyAsync(int id)
    {
        var list = await _db.MaterialLists.Include(m => m.Items).FirstOrDefaultAsync(m => m.Id == id);
        if (list is null) return false;
        _db.MaterialListItems.RemoveRange(list.Items);
        _db.MaterialLists.Remove(list);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveListsAsync(List<int> ids)
    {
        var lists = await _db.MaterialLists.Where(m => ids.Contains(m.Id) && !m.IsArchived).ToListAsync();
        foreach (var m in lists) { m.IsArchived = true; m.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return lists.Count;
    }

    public async Task<int> BulkRestoreListsAsync(List<int> ids)
    {
        var lists = await _db.MaterialLists.Where(m => ids.Contains(m.Id) && m.IsArchived).ToListAsync();
        foreach (var m in lists) { m.IsArchived = false; m.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return lists.Count;
    }

    public async Task<int> BulkDeleteListsPermanentlyAsync(List<int> ids)
    {
        var lists = await _db.MaterialLists.Include(m => m.Items).Where(m => ids.Contains(m.Id)).ToListAsync();
        foreach (var m in lists) _db.MaterialListItems.RemoveRange(m.Items);
        _db.MaterialLists.RemoveRange(lists);
        await _db.SaveChangesAsync();
        return lists.Count;
    }

    public async Task<MaterialListItemDto> AddItemAsync(int listId, MaterialListItemEditModel model)
    {
        var list = await _db.MaterialLists.Include(m => m.Items).FirstOrDefaultAsync(m => m.Id == listId)
            ?? throw new InvalidOperationException("Material list not found.");

        var item = new MaterialListItem
        {
            MaterialListId = listId,
            Section = model.Section,
            ItemName = model.ItemName,
            Quantity = model.Quantity,
            Unit = model.Unit,
            BaseCost = model.BaseCost,
            LaborHours = model.LaborHours,
            FlatPrice = model.FlatPrice,
            MarkupPercent = model.MarkupPercent,
            Notes = model.Notes,
            ProductId = model.ProductId,
            InventoryItemId = model.InventoryItemId,
            SortOrder = model.SortOrder,
        };
        list.Items.Add(item);
        RecalcTotals(list);
        await _db.SaveChangesAsync();

        return new MaterialListItemDto
        {
            Id = item.Id, Section = item.Section, ItemName = item.ItemName,
            Quantity = item.Quantity, Unit = item.Unit, BaseCost = item.BaseCost,
            LaborHours = item.LaborHours ?? 0m, FlatPrice = item.FlatPrice ?? 0m,
            MarkupPercent = item.MarkupPercent, SortOrder = item.SortOrder,
            ProductId = item.ProductId, InventoryItemId = item.InventoryItemId,
            Notes = item.Notes,
        };
    }

    public async Task<MaterialListItemDto> UpdateItemAsync(int listId, int itemId, MaterialListItemEditModel model)
    {
        var item = await _db.MaterialListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.MaterialListId == listId)
            ?? throw new InvalidOperationException("Item not found.");

        item.Section = model.Section;
        item.ItemName = model.ItemName;
        item.Quantity = model.Quantity;
        item.Unit = model.Unit;
        item.BaseCost = model.BaseCost;
        item.LaborHours = model.LaborHours;
        item.FlatPrice = model.FlatPrice;
        item.MarkupPercent = model.MarkupPercent;
        item.Notes = model.Notes;
        item.ProductId = model.ProductId;
        item.InventoryItemId = model.InventoryItemId;
        item.SortOrder = model.SortOrder;

        var list = await _db.MaterialLists.Include(m => m.Items).FirstAsync(m => m.Id == listId);
        RecalcTotals(list);
        await _db.SaveChangesAsync();

        return new MaterialListItemDto
        {
            Id = item.Id, Section = item.Section, ItemName = item.ItemName,
            Quantity = item.Quantity, Unit = item.Unit, BaseCost = item.BaseCost,
            LaborHours = item.LaborHours ?? 0m, FlatPrice = item.FlatPrice ?? 0m,
            MarkupPercent = item.MarkupPercent, SortOrder = item.SortOrder,
            ProductId = item.ProductId, InventoryItemId = item.InventoryItemId,
            Notes = item.Notes,
        };
    }

    public async Task<bool> RemoveItemAsync(int listId, int itemId)
    {
        var item = await _db.MaterialListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.MaterialListId == listId);
        if (item is null) return false;
        _db.MaterialListItems.Remove(item);

        var list = await _db.MaterialLists.Include(m => m.Items).FirstAsync(m => m.Id == listId);
        RecalcTotals(list);
        await _db.SaveChangesAsync();
        return true;
    }

    // --- Template cloning ---
    public async Task<MaterialList> CloneFromTemplateAsync(int templateListId, string newName)
    {
        var template = await _db.MaterialLists
            .Include(m => m.Items)
            .FirstOrDefaultAsync(m => m.Id == templateListId && !m.IsArchived)
            ?? throw new InvalidOperationException("Template not found.");

        var clone = new MaterialList
        {
            Name = newName,
            IsTemplate = false,
            Status = MaterialListStatus.Draft,
            TradeType = template.TradeType,
            PricingMethod = template.PricingMethod,
            MarkupPercent = template.MarkupPercent,
            TaxPercent = template.TaxPercent,
            ContingencyPercent = template.ContingencyPercent,
            Notes = template.Notes,
            InternalNotes = template.InternalNotes,
            ExternalNotes = template.ExternalNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.MaterialLists.Add(clone);
        await _db.SaveChangesAsync();

        foreach (var srcItem in template.Items)
        {
            _db.MaterialListItems.Add(new MaterialListItem
            {
                MaterialListId = clone.Id,
                Section = srcItem.Section,
                ItemName = srcItem.ItemName,
                Quantity = srcItem.Quantity,
                Unit = srcItem.Unit,
                BaseCost = srcItem.BaseCost,
                LaborHours = srcItem.LaborHours,
                FlatPrice = srcItem.FlatPrice,
                MarkupPercent = srcItem.MarkupPercent,
                Notes = srcItem.Notes,
                ProductId = srcItem.ProductId,
                InventoryItemId = srcItem.InventoryItemId,
                SortOrder = srcItem.SortOrder,
            });
        }

        RecalcTotals(clone);
        await _db.SaveChangesAsync();
        return clone;
    }

    // --- Status workflow ---
    public async Task<bool> UpdateStatusAsync(int id, MaterialListStatus status)
    {
        var list = await _db.MaterialLists.FindAsync(id);
        if (list is null) return false;
        list.Status = status;
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // --- Product picker ---
    public async Task<List<MaterialProductOption>> GetProductOptionsAsync(string? search = null)
    {
        var query = _db.Products.Where(p => !p.IsArchived && p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term)
                || (p.Category != null && p.Category.ToLower().Contains(term))
                || (p.PartNumber != null && p.PartNumber.ToLower().Contains(term))
                || (p.ModelNumber != null && p.ModelNumber.ToLower().Contains(term))
                || (p.Brand != null && p.Brand.ToLower().Contains(term)));
        }

        var products = await query.OrderBy(p => p.Name).Take(50).Select(p => new MaterialProductOption
        {
            Id = p.Id,
            Name = p.Name,
            Category = p.Category,
            Unit = p.Unit,
            Cost = p.Cost,
            Price = p.Price,
            MarkupPercent = p.MarkupPercent,
        }).ToListAsync();

        // Populate inventory info separately to avoid subquery translation issues
        var productIds = products.Select(p => p.Id).ToList();
        var inventoryLookup = await _db.InventoryItems
            .Where(i => i.ProductId != null && productIds.Contains(i.ProductId.Value))
            .GroupBy(i => i.ProductId!.Value)
            .Select(g => new { ProductId = g.Key, FirstId = g.Min(i => i.Id), TotalStock = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(g => g.ProductId);

        foreach (var p in products)
        {
            if (inventoryLookup.TryGetValue(p.Id, out var inv))
            {
                p.InventoryItemId = inv.FirstId;
                p.StockQty = inv.TotalStock;
            }
        }

        return products;
    }

    // --- Inventory stock check ---
    public async Task<List<MaterialStockCheck>> CheckInventoryStockAsync(int listId)
    {
        var items = await _db.MaterialListItems
            .Where(i => i.MaterialListId == listId && i.InventoryItemId != null)
            .Include(i => i.InventoryItem)
            .ToListAsync();

        return items.Where(i => i.InventoryItem is not null).Select(i => new MaterialStockCheck
        {
            ItemId = i.Id,
            ItemName = i.ItemName,
            RequestedQty = i.Quantity,
            AvailableQty = i.InventoryItem!.Quantity,
        }).ToList();
    }

    // --- HVAC auto-pairings ---
    public async Task<List<ItemAssociation>> GetPairingsAsync(string itemName, string tradeType = "HVAC")
    {
        var term = itemName.Trim().ToLower();
        return await _db.ItemAssociations
            .Where(ia => ia.IsActive && ia.TradeType == tradeType && ia.ItemName.ToLower().Contains(term))
            .ToListAsync();
    }

    // --- Convert to Estimate ---
    public async Task<int> ConvertToEstimateAsync(int listId, string? estimateTitle = null)
    {
        var list = await _db.MaterialLists
            .Include(m => m.Items)
            .FirstOrDefaultAsync(m => m.Id == listId && !m.IsArchived)
            ?? throw new InvalidOperationException("Material list not found.");

        var count = await _db.Estimates.CountAsync() + 1;
        var estimate = new Estimate
        {
            EstimateNumber = $"EST-{count:D5}",
            Title = estimateTitle ?? $"From Material List: {list.Name}",
            Status = EstimateStatus.Draft,
            TradeType = list.TradeType,
            PricingMethod = list.PricingMethod,
            MarkupPercent = list.MarkupPercent,
            TaxPercent = list.TaxPercent,
            ContingencyPercent = list.ContingencyPercent,
            CustomerId = list.CustomerId,
            SiteId = list.SiteId,
            MaterialListId = list.Id,
            Notes = list.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var subtotal = 0m;
        int order = 0;
        foreach (var item in list.Items.OrderBy(i => i.SortOrder))
        {
            var lineTotal = item.BaseCost * item.Quantity;
            subtotal += lineTotal;
            estimate.Lines.Add(new EstimateLine
            {
                Description = item.ItemName,
                LineType = "Material",
                Unit = item.Unit,
                Section = item.Section,
                Quantity = item.Quantity,
                UnitPrice = item.BaseCost,
                LineTotal = lineTotal,
                ProductId = item.ProductId,
                SortOrder = order++,
            });
        }

        estimate.Subtotal = subtotal;
        var afterMarkup = subtotal * (1 + estimate.MarkupPercent / 100m);
        var afterTax = afterMarkup * (1 + estimate.TaxPercent / 100m);
        var afterContingency = afterTax * (1 + estimate.ContingencyPercent / 100m);
        estimate.Total = Math.Round(afterContingency, 2);

        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync();
        return estimate.Id;
    }

    // --- Job options for linkage ---
    public async Task<List<MaterialJobOption>> GetJobOptionsAsync()
    {
        return await _db.Jobs.Where(j => !j.IsArchived)
            .OrderByDescending(j => j.CreatedAt)
            .Take(100)
            .Select(j => new MaterialJobOption
            {
                Id = j.Id,
                JobNumber = j.JobNumber,
                Title = j.Title,
            }).ToListAsync();
    }

    private static void RecalcTotals(MaterialList list)
    {
        var subtotal = list.Items.Sum(i => i.BaseCost * i.Quantity);
        list.Subtotal = subtotal;
        var afterMarkup = subtotal * (1 + list.MarkupPercent / 100m);
        var afterTax = afterMarkup * (1 + list.TaxPercent / 100m);
        var afterContingency = afterTax * (1 + list.ContingencyPercent / 100m);
        list.Total = Math.Round(afterContingency, 2);
        list.UpdatedAt = DateTime.UtcNow;
    }
}
