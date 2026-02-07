using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileSearchService : IMobileSearchService
{
    private readonly AppDbContext _db;
    public MobileSearchService(AppDbContext db) => _db = db;

    public async Task<MobileSearchResults> SearchAsync(string query, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new MobileSearchResults();

        var q = query.Trim().ToLower();
        var results = new List<MobileSearchResult>();

        // Jobs
        if (category is null or "Jobs")
        {
            var jobs = await _db.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Site)
                .Where(j => j.JobNumber.ToLower().Contains(q)
                    || (j.Title != null && j.Title.ToLower().Contains(q))
                    || (j.Description != null && j.Description.ToLower().Contains(q))
                    || (j.Customer != null && j.Customer.Name.ToLower().Contains(q)))
                .OrderByDescending(j => j.ScheduledDate)
                .Take(10)
                .ToListAsync();

            foreach (var job in jobs)
            {
                results.Add(new MobileSearchResult
                {
                    Id = job.Id,
                    Title = job.Title ?? job.JobNumber,
                    Subtitle = $"{job.JobNumber} · {job.Customer?.Name ?? "—"} · {job.Site?.Address ?? "—"}",
                    Category = "Jobs",
                    Icon = "bi-wrench-adjustable",
                    BadgeText = job.Status.ToString(),
                    BadgeColor = StatusColor(job.Status),
                    NavigateTo = $"/jobs/{job.Id}",
                });
            }
        }

        // Customers
        if (category is null or "Customers")
        {
            var customers = await _db.Customers
                .Where(c => c.Name.ToLower().Contains(q)
                    || (c.PrimaryPhone != null && c.PrimaryPhone.Contains(q))
                    || (c.PrimaryEmail != null && c.PrimaryEmail.ToLower().Contains(q))
                    || (c.Address != null && c.Address.ToLower().Contains(q)))
                .Take(10)
                .ToListAsync();

            foreach (var c in customers)
            {
                results.Add(new MobileSearchResult
                {
                    Id = c.Id,
                    Title = c.Name,
                    Subtitle = $"{c.PrimaryPhone ?? "—"} · {c.Address ?? "—"}, {c.City ?? ""}",
                    Category = "Customers",
                    Icon = "bi-person",
                    BadgeText = c.Type.ToString(),
                    BadgeColor = "info",
                    NavigateTo = $"/search?highlight=customer-{c.Id}",
                });
            }
        }

        // Sites
        if (category is null or "Sites")
        {
            var sites = await _db.Sites
                .Include(s => s.Customer)
                .Where(s => s.Name.ToLower().Contains(q)
                    || (s.Address != null && s.Address.ToLower().Contains(q))
                    || (s.City != null && s.City.ToLower().Contains(q))
                    || (s.Customer != null && s.Customer.Name.ToLower().Contains(q)))
                .Take(10)
                .ToListAsync();

            foreach (var s in sites)
            {
                results.Add(new MobileSearchResult
                {
                    Id = s.Id,
                    Title = s.Name,
                    Subtitle = $"{s.Address ?? "—"}, {s.City ?? ""} · {s.Customer?.Name ?? "—"}",
                    Category = "Sites",
                    Icon = "bi-building",
                    BadgeText = s.PropertyType.ToString(),
                    BadgeColor = "secondary",
                    NavigateTo = $"/sites/{s.Id}",
                });
            }
        }

        // Assets
        if (category is null or "Assets")
        {
            var assets = await _db.Assets
                .Include(a => a.Site)
                .Where(a => a.Name.ToLower().Contains(q)
                    || (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(q))
                    || (a.Model != null && a.Model.ToLower().Contains(q))
                    || (a.AssetType != null && a.AssetType.ToLower().Contains(q)))
                .Take(10)
                .ToListAsync();

            foreach (var a in assets)
            {
                results.Add(new MobileSearchResult
                {
                    Id = a.Id,
                    Title = a.Name,
                    Subtitle = $"{a.AssetType ?? "—"} · {a.Model ?? "—"} · S/N: {a.SerialNumber ?? "—"}",
                    Category = "Assets",
                    Icon = "bi-cpu",
                    BadgeText = a.Status.ToString(),
                    BadgeColor = a.Status == AssetStatus.Active ? "success" : a.Status == AssetStatus.MaintenanceNeeded ? "warning" : "secondary",
                    NavigateTo = $"/assets/{a.Id}",
                });
            }
        }

        // Notes
        if (category is null or "Notes")
        {
            var notes = await _db.QuickNotes
                .Where(n => (n.Title != null && n.Title.ToLower().Contains(q))
                    || n.Text.ToLower().Contains(q))
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var n in notes)
            {
                results.Add(new MobileSearchResult
                {
                    Id = n.Id,
                    Title = n.Title ?? "Quick Note",
                    Subtitle = n.Text.Length > 80 ? n.Text[..80] + "…" : n.Text,
                    Category = "Notes",
                    Icon = "bi-journal-text",
                    BadgeText = n.IsUrgent ? "Urgent" : null,
                    BadgeColor = n.IsUrgent ? "danger" : null,
                    NavigateTo = "/notes",
                });
            }
        }

        return new MobileSearchResults
        {
            Results = results,
            TotalCount = results.Count,
        };
    }

    private static string StatusColor(JobStatus status) => status switch
    {
        JobStatus.Scheduled => "info",
        JobStatus.EnRoute => "primary",
        JobStatus.OnSite or JobStatus.InProgress => "warning",
        JobStatus.Completed => "success",
        JobStatus.Cancelled => "secondary",
        _ => "secondary"
    };
}
