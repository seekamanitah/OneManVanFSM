using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileMaterialListService(AppDbContext db) : IMobileMaterialListService
{
    public async Task<List<MobileMaterialListCard>> GetListsAsync(MobileMaterialListFilter? filter = null)
    {
        var query = db.MaterialLists
            .Include(m => m.Customer)
            .Include(m => m.Job)
            .Include(m => m.Items)
            .Where(m => !m.IsArchived)
            .AsQueryable();

        if (filter?.Status.HasValue == true)
            query = query.Where(m => m.Status == filter.Status.Value);

        if (filter?.IsTemplate.HasValue == true)
            query = query.Where(m => m.IsTemplate == filter.IsTemplate.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(m => m.Name.ToLower().Contains(s)
                || (m.Customer != null && m.Customer.Name.ToLower().Contains(s))
                || (m.Job != null && m.Job.JobNumber.ToLower().Contains(s)));
        }

        var lists = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

        return lists.Select(m => new MobileMaterialListCard
        {
            Id = m.Id,
            Name = m.Name,
            IsTemplate = m.IsTemplate,
            Status = m.Status,
            TradeType = m.TradeType,
            GrandTotal = m.Total,
            ItemCount = m.Items.Count,
            CustomerName = m.Customer?.Name,
            JobNumber = m.Job?.JobNumber,
            CreatedAt = m.CreatedAt,
        }).ToList();
    }

    public async Task<MobileMaterialListDetail?> GetListDetailAsync(int id)
    {
        var m = await db.MaterialLists
            .Include(ml => ml.Customer)
            .Include(ml => ml.Job)
            .Include(ml => ml.Site)
            .Include(ml => ml.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(ml => ml.Id == id);

        if (m is null) return null;

        return new MobileMaterialListDetail
        {
            Id = m.Id,
            Name = m.Name,
            IsTemplate = m.IsTemplate,
            Status = m.Status,
            TradeType = m.TradeType,
            PricingMethod = m.PricingMethod,
            Subtotal = m.Subtotal,
            MarkupPercent = m.MarkupPercent,
            TaxPercent = m.TaxPercent,
            ContingencyPercent = m.ContingencyPercent,
            Total = m.Total,
            Notes = m.Notes,
            PONumber = m.PONumber,
            JobId = m.JobId,
            JobNumber = m.Job?.JobNumber,
            CustomerId = m.CustomerId,
            CustomerName = m.Customer?.Name,
            SiteId = m.SiteId,
            SiteName = m.Site?.Name,
            CreatedAt = m.CreatedAt,
            Items = m.Items.OrderBy(i => i.SortOrder).Select(i => new MobileMaterialListItemDto
            {
                Id = i.Id,
                Section = i.Section,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                Unit = i.Unit,
                BaseCost = i.BaseCost,
                LaborHours = i.LaborHours ?? 0,
                FlatPrice = i.FlatPrice ?? 0,
                MarkupPercent = i.MarkupPercent,
                SortOrder = i.SortOrder,
                Notes = i.Notes,
                ProductName = i.Product?.Name,
            }).ToList(),
        };
    }

    public async Task<int> CreateListAsync(MobileMaterialListCreate model)
    {
        var list = new MaterialList
        {
            Name = model.Name,
            IsTemplate = model.IsTemplate,
            TradeType = model.TradeType,
            PricingMethod = model.PricingMethod,
            Notes = model.Notes,
            PONumber = model.PONumber,
            JobId = model.JobId,
            CustomerId = model.CustomerId,
            SiteId = model.SiteId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.MaterialLists.Add(list);
        await db.SaveChangesAsync();
        return list.Id;
    }

    public async Task<bool> UpdateListAsync(int id, MobileMaterialListUpdate model)
    {
        var list = await db.MaterialLists.FindAsync(id);
        if (list is null) return false;

        list.Name = model.Name;
        list.TradeType = model.TradeType;
        list.PricingMethod = model.PricingMethod;
        list.MarkupPercent = model.MarkupPercent;
        list.TaxPercent = model.TaxPercent;
        list.ContingencyPercent = model.ContingencyPercent;
        list.Notes = model.Notes;
        list.PONumber = model.PONumber;
        list.JobId = model.JobId;
        list.CustomerId = model.CustomerId;
        list.SiteId = model.SiteId;
        list.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, MaterialListStatus status)
    {
        var list = await db.MaterialLists.FindAsync(id);
        if (list is null) return false;
        list.Status = status;
        list.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteListAsync(int id)
    {
        var list = await db.MaterialLists.FindAsync(id);
        if (list is null) return false;
        list.IsArchived = true;
        list.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<MobileMaterialListStats> GetStatsAsync()
    {
        var lists = await db.MaterialLists.Where(m => !m.IsArchived).ToListAsync();
        return new MobileMaterialListStats
        {
            TotalLists = lists.Count,
            DraftCount = lists.Count(m => m.Status == MaterialListStatus.Draft),
            TemplateCount = lists.Count(m => m.IsTemplate),
            TotalValue = lists.Sum(m => m.Total),
        };
    }

    public async Task<int> AddItemAsync(int listId, MobileMaterialListItemCreate model)
    {
        var maxSort = await db.Set<MaterialListItem>()
            .Where(i => i.MaterialListId == listId)
            .MaxAsync(i => (int?)i.SortOrder) ?? 0;

        var item = new MaterialListItem
        {
            MaterialListId = listId,
            ItemName = model.ItemName,
            Section = model.Section,
            Quantity = model.Quantity,
            Unit = model.Unit,
            BaseCost = model.BaseCost,
            LaborHours = model.LaborHours,
            FlatPrice = model.FlatPrice,
            MarkupPercent = model.MarkupPercent,
            Notes = model.Notes,
            ProductId = model.ProductId,
            InventoryItemId = model.InventoryItemId,
            SortOrder = maxSort + 1,
        };
        db.Set<MaterialListItem>().Add(item);
        await RecalcTotals(listId);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<bool> UpdateItemAsync(int listId, int itemId, MobileMaterialListItemUpdate model)
    {
        var item = await db.Set<MaterialListItem>().FirstOrDefaultAsync(i => i.Id == itemId && i.MaterialListId == listId);
        if (item is null) return false;

        item.ItemName = model.ItemName;
        item.Section = model.Section;
        item.Quantity = model.Quantity;
        item.Unit = model.Unit;
        item.BaseCost = model.BaseCost;
        item.LaborHours = model.LaborHours;
        item.FlatPrice = model.FlatPrice;
        item.MarkupPercent = model.MarkupPercent;
        item.Notes = model.Notes;
        await RecalcTotals(listId);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveItemAsync(int listId, int itemId)
    {
        var item = await db.Set<MaterialListItem>().FirstOrDefaultAsync(i => i.Id == itemId && i.MaterialListId == listId);
        if (item is null) return false;
        db.Set<MaterialListItem>().Remove(item);
        await RecalcTotals(listId);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<MobileMaterialProductOption>> GetProductOptionsAsync(string? search = null)
    {
        var query = db.Products.Where(p => !p.IsArchived).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s));
        }
        return await query.OrderBy(p => p.Name).Take(50).Select(p => new MobileMaterialProductOption
        {
            Id = p.Id,
            Name = p.Name,
            Category = p.Category,
            Unit = p.Unit,
            Cost = p.Cost,
            Price = p.Price,
        }).ToListAsync();
    }

    public async Task<List<MobileJobOption>> GetJobOptionsAsync()
    {
        return await db.Jobs
            .Where(j => !j.IsArchived && j.Status != JobStatus.Completed && j.Status != JobStatus.Closed && j.Status != JobStatus.Cancelled)
            .OrderByDescending(j => j.CreatedAt)
            .Take(50)
            .Select(j => new MobileJobOption { Id = j.Id, JobNumber = j.JobNumber, Title = j.Title })
            .ToListAsync();
    }

    private async Task RecalcTotals(int listId)
    {
        var list = await db.MaterialLists.Include(m => m.Items).FirstOrDefaultAsync(m => m.Id == listId);
        if (list is null) return;

        decimal subtotal = 0;
        foreach (var item in list.Items)
        {
            subtotal += item.FlatPrice > 0
                ? item.FlatPrice.Value * item.Quantity
                : item.BaseCost * item.Quantity * (1 + item.MarkupPercent / 100m);
        }
        list.Subtotal = subtotal;
        var afterMarkup = subtotal * (1 + list.MarkupPercent / 100m);
        var afterTax = afterMarkup * (1 + list.TaxPercent / 100m);
        list.Total = afterTax * (1 + list.ContingencyPercent / 100m);
        list.UpdatedAt = DateTime.UtcNow;
    }
}
