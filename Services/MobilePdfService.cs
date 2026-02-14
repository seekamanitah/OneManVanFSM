using System.Text;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobilePdfService : IMobilePdfService
{
    private readonly AppDbContext _db;
    private readonly IMobileReportService _reportService;

    public MobilePdfService(AppDbContext db, IMobileReportService reportService)
    {
        _db = db;
        _reportService = reportService;
    }

    public async Task<string?> GenerateEstimateDocumentAsync(int estimateId)
    {
        var estimate = await _db.Estimates.AsNoTracking()
            .Include(e => e.Customer)
            .Include(e => e.Company)
            .Include(e => e.Site)
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == estimateId);
        if (estimate is null) return null;

        var html = BuildEstimateHtml(estimate);
        var fileName = $"Estimate_{estimate.EstimateNumber}_{DateTime.Now:yyyyMMdd}.html";
        return await SaveDocumentAsync(html, fileName);
    }

    public async Task<string?> GenerateInvoiceDocumentAsync(int invoiceId)
    {
        var invoice = await _db.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Company)
            .Include(i => i.Site)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null) return null;

        var html = BuildInvoiceHtml(invoice);
        var fileName = $"Invoice_{invoice.InvoiceNumber}_{DateTime.Now:yyyyMMdd}.html";
        return await SaveDocumentAsync(html, fileName);
    }

    public async Task<string?> GenerateTechReportDocumentAsync(int employeeId)
    {
        var report = await _reportService.GetTechReportAsync(employeeId);
        var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);
        if (emp is null) return null;

        var html = BuildTechReportHtml(report, emp);
        var fileName = $"TechReport_{emp.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.html";
        return await SaveDocumentAsync(html, fileName);
    }

    public async Task ShareDocumentAsync(string filePath, string title)
    {
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(filePath)
        });
    }

    private static async Task<string> SaveDocumentAsync(string html, string fileName)
    {
        var dir = Path.Combine(FileSystem.CacheDirectory, "documents");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(filePath, html);
        return filePath;
    }

    private static string BuildEstimateHtml(Estimate e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine(DocumentCss());
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='doc'>");
        sb.AppendLine($"<h1>Estimate {e.EstimateNumber}</h1>");
        sb.AppendLine($"<p class='sub'>{e.Title}</p>");

        sb.AppendLine("<table class='info'>");
        sb.AppendLine($"<tr><td><strong>Status:</strong></td><td>{e.Status}</td></tr>");
        if (e.Customer is not null)
            sb.AppendLine($"<tr><td><strong>Customer:</strong></td><td>{e.Customer.Name}</td></tr>");
        if (e.Site is not null)
            sb.AppendLine($"<tr><td><strong>Site:</strong></td><td>{e.Site.Address}, {e.Site.City}</td></tr>");
        if (e.ExpiryDate.HasValue)
            sb.AppendLine($"<tr><td><strong>Expires:</strong></td><td>{e.ExpiryDate.Value:MMM dd, yyyy}</td></tr>");
        sb.AppendLine($"<tr><td><strong>Date:</strong></td><td>{e.CreatedAt:MMM dd, yyyy}</td></tr>");
        sb.AppendLine("</table>");

        if (e.Lines.Count > 0)
        {
            sb.AppendLine("<table class='lines'>");
            sb.AppendLine("<thead><tr><th>Description</th><th>Qty</th><th>Unit Price</th><th>Total</th></tr></thead><tbody>");
            foreach (var line in e.Lines.OrderBy(l => l.SortOrder))
            {
                sb.AppendLine($"<tr><td>{line.Description}</td><td>{line.Quantity}</td><td>${line.UnitPrice:N2}</td><td>${line.LineTotal:N2}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("<div class='totals'>");
        sb.AppendLine($"<div>Subtotal: <strong>${e.Subtotal:N2}</strong></div>");
        if (e.MarkupPercent > 0)
            sb.AppendLine($"<div>Markup ({e.MarkupPercent}%)</div>");
        if (e.TaxPercent > 0)
            sb.AppendLine($"<div>Tax ({e.TaxPercent}%)</div>");
        sb.AppendLine($"<div class='grand'>Total: <strong>${e.Total:N2}</strong></div>");
        if (e.DepositRequired.HasValue && e.DepositRequired.Value > 0)
            sb.AppendLine($"<div>Deposit Required: ${e.DepositRequired.Value:N2}</div>");
        sb.AppendLine("</div>");

        if (!string.IsNullOrWhiteSpace(e.Notes))
            sb.AppendLine($"<div class='notes'><strong>Notes:</strong><br/>{e.Notes}</div>");

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static string BuildInvoiceHtml(Invoice inv)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine(DocumentCss());
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='doc'>");
        sb.AppendLine($"<h1>Invoice {inv.InvoiceNumber}</h1>");
        sb.AppendLine($"<p class='sub'>Status: {inv.Status}</p>");

        sb.AppendLine("<table class='info'>");
        if (inv.Customer is not null)
            sb.AppendLine($"<tr><td><strong>Bill To:</strong></td><td>{inv.Customer.Name}</td></tr>");
        if (inv.Site is not null)
            sb.AppendLine($"<tr><td><strong>Site:</strong></td><td>{inv.Site.Address}, {inv.Site.City}</td></tr>");
        sb.AppendLine($"<tr><td><strong>Invoice Date:</strong></td><td>{inv.InvoiceDate:MMM dd, yyyy}</td></tr>");
        if (inv.DueDate.HasValue)
            sb.AppendLine($"<tr><td><strong>Due Date:</strong></td><td>{inv.DueDate.Value:MMM dd, yyyy}</td></tr>");
        if (!string.IsNullOrWhiteSpace(inv.PaymentTerms))
            sb.AppendLine($"<tr><td><strong>Terms:</strong></td><td>{inv.PaymentTerms}</td></tr>");
        sb.AppendLine("</table>");

        if (inv.Lines.Count > 0)
        {
            sb.AppendLine("<table class='lines'>");
            sb.AppendLine("<thead><tr><th>Description</th><th>Qty</th><th>Unit Price</th><th>Total</th></tr></thead><tbody>");
            foreach (var line in inv.Lines.OrderBy(l => l.SortOrder))
            {
                sb.AppendLine($"<tr><td>{line.Description}</td><td>{line.Quantity}</td><td>${line.UnitPrice:N2}</td><td>${line.LineTotal:N2}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("<div class='totals'>");
        sb.AppendLine($"<div>Subtotal: <strong>${inv.Subtotal:N2}</strong></div>");
        if (inv.TaxAmount > 0)
            sb.AppendLine($"<div>Tax: ${inv.TaxAmount:N2}</div>");
        if (inv.DiscountAmount > 0)
            sb.AppendLine($"<div>Discount: -${inv.DiscountAmount:N2}</div>");
        sb.AppendLine($"<div class='grand'>Total: <strong>${inv.Total:N2}</strong></div>");
        if (inv.AmountPaid > 0)
            sb.AppendLine($"<div>Paid: ${inv.AmountPaid:N2}</div>");
        sb.AppendLine($"<div class='grand'>Balance Due: <strong>${inv.BalanceDue:N2}</strong></div>");
        sb.AppendLine("</div>");

        if (inv.Payments.Count > 0)
        {
            sb.AppendLine("<h3>Payments</h3><table class='lines'>");
            sb.AppendLine("<thead><tr><th>Date</th><th>Method</th><th>Amount</th><th>Reference</th></tr></thead><tbody>");
            foreach (var p in inv.Payments.OrderBy(p => p.PaymentDate))
            {
                sb.AppendLine($"<tr><td>{p.PaymentDate:MMM dd, yyyy}</td><td>{p.Method}</td><td>${p.Amount:N2}</td><td>{p.Reference}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        if (!string.IsNullOrWhiteSpace(inv.Notes))
            sb.AppendLine($"<div class='notes'><strong>Notes:</strong><br/>{inv.Notes}</div>");
        if (!string.IsNullOrWhiteSpace(inv.Terms))
            sb.AppendLine($"<div class='notes'><strong>Terms:</strong><br/>{inv.Terms}</div>");

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static string BuildTechReportHtml(MobileTechReport report, Employee emp)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine(DocumentCss());
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='doc'>");
        sb.AppendLine($"<h1>Technician Performance Report</h1>");
        sb.AppendLine($"<p class='sub'>{emp.Name} — Generated {DateTime.Now:MMM dd, yyyy h:mm tt}</p>");

        sb.AppendLine("<h3>Time Tracking</h3>");
        sb.AppendLine("<table class='info'>");
        sb.AppendLine($"<tr><td>Hours Today:</td><td>{report.HoursToday:0.#}h</td></tr>");
        sb.AppendLine($"<tr><td>Hours This Week:</td><td>{report.HoursThisWeek:0.#}h</td></tr>");
        sb.AppendLine($"<tr><td>Hours This Month:</td><td>{report.HoursThisMonth:0.#}h</td></tr>");
        sb.AppendLine($"<tr><td>Billable Rate:</td><td>{report.BillablePercent}%</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<h3>Job Metrics</h3>");
        sb.AppendLine("<table class='info'>");
        sb.AppendLine($"<tr><td>Completed Today:</td><td>{report.JobsCompletedToday}</td></tr>");
        sb.AppendLine($"<tr><td>Completed This Week:</td><td>{report.JobsCompletedThisWeek}</td></tr>");
        sb.AppendLine($"<tr><td>Completed This Month:</td><td>{report.JobsCompletedThisMonth}</td></tr>");
        sb.AppendLine($"<tr><td>Assigned This Month:</td><td>{report.JobsAssignedThisMonth}</td></tr>");
        sb.AppendLine($"<tr><td>Completion Rate:</td><td>{report.CompletionRate}%</td></tr>");
        sb.AppendLine($"<tr><td>Avg Duration:</td><td>{report.AvgJobDurationHours}h</td></tr>");
        sb.AppendLine("</table>");

        if (report.DailyBreakdown.Count > 0)
        {
            sb.AppendLine("<h3>Last 7 Days</h3>");
            sb.AppendLine("<table class='lines'><thead><tr><th>Day</th><th>Hours</th><th>Jobs Done</th></tr></thead><tbody>");
            foreach (var d in report.DailyBreakdown)
                sb.AppendLine($"<tr><td>{d.DayLabel}</td><td>{d.Hours:0.#}</td><td>{d.JobsCompleted}</td></tr>");
            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static string DocumentCss() => """
        body { font-family: 'Segoe UI', system-ui, -apple-system, sans-serif; font-size: 10pt; color: #333; margin: 0; padding: 12px; }
        .doc { max-width: 800px; margin: 0 auto; }
        h1 { font-size: 16pt; margin-bottom: 4px; color: #1a1a1a; }
        h3 { font-size: 11pt; margin: 12px 0 6px; border-bottom: 1px solid #ddd; padding-bottom: 3px; }
        .sub { color: #666; font-size: 9pt; margin-top: 0; }
        table.info { border-collapse: collapse; margin: 8px 0; width: 100%; }
        table.info td { padding: 3px 10px 3px 0; font-size: 9pt; border-bottom: 1px solid #f0f0f0; }
        table.lines { border-collapse: collapse; width: 100%; margin: 8px 0; }
        table.lines th { background: #f5f5f5; font-size: 8pt; text-align: left; padding: 5px 8px; border-bottom: 2px solid #ddd; }
        table.lines td { padding: 4px 8px; font-size: 9pt; border-bottom: 1px solid #eee; }
        .totals { text-align: right; margin: 12px 0; font-size: 9pt; }
        .totals .grand { font-size: 11pt; margin-top: 4px; }
        .notes { background: #f9f9f9; padding: 8px 12px; border-radius: 4px; font-size: 9pt; margin-top: 12px; }
        """;
}
