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
        var query = _db.MaterialLists.Where(m => !m.IsArchived).AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(m => m.Name.ToLower().Contains(term));
            }
            if (filter.IsTemplate.HasValue) query = query.Where(m => m.IsTemplate == filter.IsTemplate.Value);
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDescending ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
                "total" => filter.SortDescending ? query.OrderByDescending(m => m.Total) : query.OrderBy(m => m.Total),
                _ => filter.SortDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt)
            };
        }
        else query = query.OrderByDescending(m => m.CreatedAt);

        return await query.Select(m => new MaterialListListItem
        {
            Id = m.Id, Name = m.Name, IsTemplate = m.IsTemplate,
            PricingMethod = m.PricingMethod,
            TotalMaterialCost = m.Subtotal, TotalLaborCost = 0,
            GrandTotal = m.Total, ItemCount = m.Items.Count,
            CustomerName = m.Customer != null ? m.Customer.Name : null,
            SiteName = m.Site != null ? m.Site.Name : null,
            CreatedAt = m.CreatedAt
        }).ToListAsync();
    }

    public async Task<MaterialListDetail?> GetListAsync(int id)
    {
        var m = await _db.MaterialLists
            .Include(ml => ml.Customer)
            .Include(ml => ml.Site)
            .Include(ml => ml.Items).ThenInclude(i => i.Product)
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
            PricingMethod = m.PricingMethod,
            TotalMaterialCost = m.Subtotal, TotalLaborCost = 0,
            GrandTotal = m.Total,
            MarkupPercent = m.MarkupPercent, TaxPercent = m.TaxPercent,
            ContingencyPercent = m.ContingencyPercent,
            Notes = m.Notes,
            InternalNotes = m.InternalNotes,
            ExternalNotes = m.ExternalNotes,
            PONumber = m.PONumber,
            CustomerId = m.CustomerId,
            CustomerName = m.Customer?.Name,
            CustomerAddress = custAddr,
            SiteId = m.SiteId,
            SiteName = m.Site?.Name,
            SiteAddress = siteAddr,
            CreatedAt = m.CreatedAt, UpdatedAt = m.UpdatedAt,
            Items = m.Items.Select(i => new MaterialListItemDto
            {
                Id = i.Id, Section = i.Section, ItemName = i.ItemName,
                Quantity = i.Quantity, Unit = i.Unit, BaseCost = i.BaseCost,
                LaborHours = i.LaborHours ?? 0m, FlatPrice = i.FlatPrice ?? 0m,
                MarkupPercent = i.MarkupPercent, ProductId = i.ProductId,
                ProductName = i.Product?.Name
            }).ToList()
        };
    }

    public async Task<MaterialList> CreateListAsync(MaterialListEditModel model)
    {
        var list = new MaterialList
        {
            Name = model.Name, IsTemplate = model.IsTemplate,
            PricingMethod = model.PricingMethod, Notes = model.Notes,
            InternalNotes = model.InternalNotes, ExternalNotes = model.ExternalNotes,
            PONumber = model.PONumber,
            CustomerId = model.CustomerId, SiteId = model.SiteId,
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
        list.PricingMethod = model.PricingMethod; list.Notes = model.Notes;
        list.InternalNotes = model.InternalNotes; list.ExternalNotes = model.ExternalNotes;
        list.PONumber = model.PONumber;
        list.CustomerId = model.CustomerId; list.SiteId = model.SiteId;
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

    public async Task<MaterialListItemDto> AddItemAsync(int listId, MaterialListItemEditModel model)
    {
        var list = await _db.MaterialLists.FindAsync(listId)
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
        };
        _db.MaterialListItems.Add(item);
        RecalcTotals(list);
        await _db.SaveChangesAsync();

        return new MaterialListItemDto
        {
            Id = item.Id, Section = item.Section, ItemName = item.ItemName,
            Quantity = item.Quantity, Unit = item.Unit, BaseCost = item.BaseCost,
            LaborHours = item.LaborHours ?? 0m, FlatPrice = item.FlatPrice ?? 0m,
            MarkupPercent = item.MarkupPercent, ProductId = item.ProductId,
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

        var list = await _db.MaterialLists.Include(m => m.Items).FirstAsync(m => m.Id == listId);
        RecalcTotals(list);
        await _db.SaveChangesAsync();

        return new MaterialListItemDto
        {
            Id = item.Id, Section = item.Section, ItemName = item.ItemName,
            Quantity = item.Quantity, Unit = item.Unit, BaseCost = item.BaseCost,
            LaborHours = item.LaborHours ?? 0m, FlatPrice = item.FlatPrice ?? 0m,
            MarkupPercent = item.MarkupPercent, ProductId = item.ProductId,
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
