using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Shared.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<MaterialList> MaterialLists => Set<MaterialList>();
    public DbSet<MaterialListItem> MaterialListItems => Set<MaterialListItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<ServiceAgreement> ServiceAgreements => Set<ServiceAgreement>();
    public DbSet<QuickNote> QuickNotes => Set<QuickNote>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<ServiceHistoryRecord> ServiceHistoryRecords => Set<ServiceHistoryRecord>();
    public DbSet<ClaimAction> ClaimActions => Set<ClaimAction>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<EstimateLine> EstimateLines => Set<EstimateLine>();
    public DbSet<JobEmployee> JobEmployees => Set<JobEmployee>();
    public DbSet<JobAsset> JobAssets => Set<JobAsset>();
    public DbSet<ServiceAgreementAsset> ServiceAgreementAssets => Set<ServiceAgreementAsset>();
    public DbSet<AssetServiceLog> AssetServiceLogs => Set<AssetServiceLog>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<DropdownOption> DropdownOptions => Set<DropdownOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasIndex(c => c.Name);
            e.HasIndex(c => c.Type);
            e.HasOne(c => c.Company)
                .WithMany(co => co.Contacts)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Site
        modelBuilder.Entity<Site>(e =>
        {
            e.HasOne(s => s.Customer)
                .WithMany(c => c.Sites)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.Company)
                .WithMany(co => co.Sites)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Asset
        modelBuilder.Entity<Asset>(e =>
        {
            e.HasOne(a => a.Product)
                .WithMany(p => p.Assets)
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Site)
                .WithMany(s => s.Assets)
                .HasForeignKey(a => a.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // InventoryItem
        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasOne(i => i.Product)
                .WithMany(p => p.InventoryItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Job
        modelBuilder.Entity<Job>(e =>
        {
            e.Property(j => j.ScheduledTime)
                .HasConversion(new ValueConverter<TimeSpan?, long?>(
                    v => v.HasValue ? v.Value.Ticks : null,
                    v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null));
            e.HasIndex(j => j.JobNumber).IsUnique();
            e.HasIndex(j => j.Status);
            e.HasIndex(j => j.ScheduledDate);
            e.HasOne(j => j.Customer)
                .WithMany(c => c.Jobs)
                .HasForeignKey(j => j.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(j => j.Company)
                .WithMany(co => co.Jobs)
                .HasForeignKey(j => j.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(j => j.Site)
                .WithMany(s => s.Jobs)
                .HasForeignKey(j => j.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(j => j.AssignedEmployee)
                .WithMany(emp => emp.AssignedJobs)
                .HasForeignKey(j => j.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(j => j.Estimate)
                .WithOne(est => est.Job)
                .HasForeignKey<Job>(j => j.EstimateId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(j => j.Invoice)
                .WithOne(inv => inv.Job)
                .HasForeignKey<Job>(j => j.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Estimate
        modelBuilder.Entity<Estimate>(e =>
        {
            e.HasIndex(est => est.EstimateNumber).IsUnique();
            e.HasOne(est => est.Customer)
                .WithMany(c => c.Estimates)
                .HasForeignKey(est => est.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(est => est.Company)
                .WithMany(co => co.Estimates)
                .HasForeignKey(est => est.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(est => est.Site)
                .WithMany(s => s.Estimates)
                .HasForeignKey(est => est.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MaterialListItem
        modelBuilder.Entity<MaterialListItem>(e =>
        {
            e.HasOne(mli => mli.MaterialList)
                .WithMany(ml => ml.Items)
                .HasForeignKey(mli => mli.MaterialListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.HasIndex(i => i.Status);
            e.HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.Company)
                .WithMany(co => co.Invoices)
                .HasForeignKey(i => i.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Payment
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InvoiceLine
        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.HasOne(il => il.Invoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(il => il.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(il => il.Product)
                .WithMany()
                .HasForeignKey(il => il.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // EstimateLine
        modelBuilder.Entity<EstimateLine>(e =>
        {
            e.HasOne(el => el.Estimate)
                .WithMany(est => est.Lines)
                .HasForeignKey(el => el.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(el => el.Product)
                .WithMany()
                .HasForeignKey(el => el.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // JobEmployee
        modelBuilder.Entity<JobEmployee>(e =>
        {
            e.HasIndex(je => new { je.JobId, je.EmployeeId }).IsUnique();
            e.HasOne(je => je.Job)
                .WithMany(j => j.JobEmployees)
                .HasForeignKey(je => je.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(je => je.Employee)
                .WithMany()
                .HasForeignKey(je => je.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // JobAsset
        modelBuilder.Entity<JobAsset>(e =>
        {
            e.HasIndex(ja => new { ja.JobId, ja.AssetId }).IsUnique();
            e.HasOne(ja => ja.Job)
                .WithMany(j => j.JobAssets)
                .HasForeignKey(ja => ja.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ja => ja.Asset)
                .WithMany(a => a.JobAssets)
                .HasForeignKey(ja => ja.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ServiceAgreementAsset
        modelBuilder.Entity<ServiceAgreementAsset>(e =>
        {
            e.HasIndex(saa => new { saa.ServiceAgreementId, saa.AssetId }).IsUnique();
            e.HasOne(saa => saa.ServiceAgreement)
                .WithMany(sa => sa.ServiceAgreementAssets)
                .HasForeignKey(saa => saa.ServiceAgreementId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(saa => saa.Asset)
                .WithMany(a => a.ServiceAgreementAssets)
                .HasForeignKey(saa => saa.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AssetServiceLog
        modelBuilder.Entity<AssetServiceLog>(e =>
        {
            e.HasOne(asl => asl.Asset)
                .WithMany(a => a.ServiceLogs)
                .HasForeignKey(asl => asl.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Supplier
        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasIndex(s => s.Name);
        });

        // DropdownOption
        modelBuilder.Entity<DropdownOption>(e =>
        {
            e.HasIndex(d => new { d.Category, d.Value }).IsUnique();
            e.HasIndex(d => d.Category);
        });

        // TimeEntry
        modelBuilder.Entity<TimeEntry>(e =>
        {
            e.HasOne(te => te.Employee)
                .WithMany(emp => emp.TimeEntries)
                .HasForeignKey(te => te.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(te => te.Job)
                .WithMany(j => j.TimeEntries)
                .HasForeignKey(te => te.JobId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(te => te.Asset)
                .WithMany()
                .HasForeignKey(te => te.AssetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Expense
        modelBuilder.Entity<Expense>(e =>
        {
            e.HasOne(exp => exp.Employee)
                .WithMany(emp => emp.Expenses)
                .HasForeignKey(exp => exp.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(exp => exp.Job)
                .WithMany(j => j.Expenses)
                .HasForeignKey(exp => exp.JobId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ServiceAgreement
        modelBuilder.Entity<ServiceAgreement>(e =>
        {
            e.HasIndex(sa => sa.AgreementNumber).IsUnique();
            e.HasOne(sa => sa.Customer)
                .WithMany(c => c.ServiceAgreements)
                .HasForeignKey(sa => sa.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(sa => sa.Company)
                .WithMany(co => co.ServiceAgreements)
                .HasForeignKey(sa => sa.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // QuickNote
        modelBuilder.Entity<QuickNote>(e =>
        {
            e.HasIndex(qn => qn.Category);
            e.HasIndex(qn => qn.EntityType);
        });

        // Document
        modelBuilder.Entity<Document>(e =>
        {
            e.HasOne(d => d.UploadedByEmployee)
                .WithMany()
                .HasForeignKey(d => d.UploadedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Employee)
                .WithMany()
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Job)
                .WithMany(j => j.Documents)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(al => al.Timestamp);
            e.HasIndex(al => al.EntityType);
        });

        // Template
        modelBuilder.Entity<Template>(e =>
        {
            e.HasIndex(t => t.Type);
            e.HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // TemplateVersion
        modelBuilder.Entity<TemplateVersion>(e =>
        {
            e.HasOne(tv => tv.Template)
                .WithMany(t => t.Versions)
                .HasForeignKey(tv => tv.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ServiceHistoryRecord
        modelBuilder.Entity<ServiceHistoryRecord>(e =>
        {
            e.HasIndex(sh => sh.RecordNumber).IsUnique();
            e.HasIndex(sh => sh.Type);
            e.HasIndex(sh => sh.Status);
            e.HasOne(sh => sh.Customer)
                .WithMany()
                .HasForeignKey(sh => sh.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(sh => sh.Company)
                .WithMany()
                .HasForeignKey(sh => sh.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(sh => sh.Site)
                .WithMany()
                .HasForeignKey(sh => sh.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(sh => sh.Asset)
                .WithMany()
                .HasForeignKey(sh => sh.AssetId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(sh => sh.Job)
                .WithMany()
                .HasForeignKey(sh => sh.JobId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(sh => sh.Tech)
                .WithMany()
                .HasForeignKey(sh => sh.TechId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ClaimAction
        modelBuilder.Entity<ClaimAction>(e =>
        {
            e.HasOne(ca => ca.ServiceHistoryRecord)
                .WithMany(sh => sh.ClaimActions)
                .HasForeignKey(ca => ca.ServiceHistoryRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
