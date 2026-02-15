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
    public DbSet<ExpenseLine> ExpenseLines => Set<ExpenseLine>();
    public DbSet<EstimateLine> EstimateLines => Set<EstimateLine>();
    public DbSet<JobEmployee> JobEmployees => Set<JobEmployee>();
    public DbSet<JobAsset> JobAssets => Set<JobAsset>();
    public DbSet<ServiceAgreementAsset> ServiceAgreementAssets => Set<ServiceAgreementAsset>();
    public DbSet<AssetServiceLog> AssetServiceLogs => Set<AssetServiceLog>();
    public DbSet<AssetLink> AssetLinks => Set<AssetLink>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<DropdownOption> DropdownOptions => Set<DropdownOption>();
    public DbSet<ItemAssociation> ItemAssociations => Set<ItemAssociation>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<MobileDevice> MobileDevices => Set<MobileDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // RolePermission — unique constraint on (Role, Feature)
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasIndex(rp => new { rp.Role, rp.Feature }).IsUnique();
        });

        // Company
        modelBuilder.Entity<Company>(e =>
        {
            e.HasIndex(co => co.Name);
            e.HasOne(co => co.PrimaryContact)
                .WithMany()
                .HasForeignKey(co => co.PrimaryContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

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
            e.HasOne(a => a.Customer)
                .WithMany(c => c.Assets)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // InventoryItem
        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasOne(i => i.Product)
                .WithMany(p => p.InventoryItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.Supplier)
                .WithMany()
                .HasForeignKey(i => i.SupplierId)
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
            e.HasOne(j => j.MaterialList)
                .WithMany()
                .HasForeignKey(j => j.MaterialListId)
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
            e.HasOne(est => est.MaterialList)
                .WithMany()
                .HasForeignKey(est => est.MaterialListId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MaterialList
        modelBuilder.Entity<MaterialList>(e =>
        {
            e.HasIndex(ml => ml.Status);
            e.HasOne(ml => ml.Job)
                .WithMany()
                .HasForeignKey(ml => ml.JobId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ml => ml.Customer)
                .WithMany()
                .HasForeignKey(ml => ml.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ml => ml.Site)
                .WithMany()
                .HasForeignKey(ml => ml.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MaterialListItem
        modelBuilder.Entity<MaterialListItem>(e =>
        {
            e.HasOne(mli => mli.MaterialList)
                .WithMany(ml => ml.Items)
                .HasForeignKey(mli => mli.MaterialListId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(mli => mli.Product)
                .WithMany()
                .HasForeignKey(mli => mli.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(mli => mli.InventoryItem)
                .WithMany()
                .HasForeignKey(mli => mli.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ItemAssociation
        modelBuilder.Entity<ItemAssociation>(e =>
        {
            e.HasIndex(ia => new { ia.ItemName, ia.AssociatedItemName, ia.TradeType }).IsUnique();
            e.HasIndex(ia => ia.TradeType);
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
            e.HasOne(i => i.Site)
                .WithMany()
                .HasForeignKey(i => i.SiteId)
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
            e.HasOne(il => il.Asset)
                .WithMany()
                .HasForeignKey(il => il.AssetId)
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
            e.HasOne(el => el.Asset)
                .WithMany()
                .HasForeignKey(el => el.AssetId)
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

        // AssetLink (peer-to-peer equipment grouping)
        modelBuilder.Entity<AssetLink>(e =>
        {
            e.HasIndex(al => new { al.AssetId, al.LinkedAssetId }).IsUnique();
            e.HasOne(al => al.Asset)
                .WithMany(a => a.AssetLinksFrom)
                .HasForeignKey(al => al.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(al => al.LinkedAsset)
                .WithMany(a => a.AssetLinksTo)
                .HasForeignKey(al => al.LinkedAssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Supplier
        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasIndex(s => s.Name);
            e.HasOne(s => s.Company)
                .WithMany()
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
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
            e.HasOne(exp => exp.Company)
                .WithMany(co => co.Expenses)
                .HasForeignKey(exp => exp.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(exp => exp.Customer)
                .WithMany()
                .HasForeignKey(exp => exp.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(exp => exp.Invoice)
                .WithMany()
                .HasForeignKey(exp => exp.InvoiceId)
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
            e.HasOne(sa => sa.Site)
                .WithMany()
                .HasForeignKey(sa => sa.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // QuickNote
        modelBuilder.Entity<QuickNote>(e =>
        {
            e.HasIndex(qn => qn.Category);
            e.HasIndex(qn => qn.EntityType);
            e.HasOne(qn => qn.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(qn => qn.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(qn => qn.Customer)
                .WithMany(c => c.QuickNotes)
                .HasForeignKey(qn => qn.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(qn => qn.Job)
                .WithMany(j => j.QuickNotes)
                .HasForeignKey(qn => qn.JobId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Document
        modelBuilder.Entity<Document>(e =>
        {
            e.HasOne(d => d.UploadedByEmployee)
                .WithMany()
                .HasForeignKey(d => d.UploadedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Employee)
                .WithMany(emp => emp.Documents)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Job)
                .WithMany(j => j.Documents)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Customer)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Site)
                .WithMany()
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Asset)
                .WithMany()
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Employee)
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
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

        // CalendarEvent
        modelBuilder.Entity<CalendarEvent>(e =>
        {
            e.HasOne(ce => ce.Job)
                .WithMany()
                .HasForeignKey(ce => ce.JobId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ce => ce.Employee)
                .WithMany()
                .HasForeignKey(ce => ce.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ce => ce.ServiceAgreement)
                .WithMany()
                .HasForeignKey(ce => ce.ServiceAgreementId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ExpenseLine
        modelBuilder.Entity<ExpenseLine>(e =>
        {
            e.HasOne(el => el.Expense)
                .WithMany(exp => exp.Lines)
                .HasForeignKey(el => el.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(el => el.Product)
                .WithMany()
                .HasForeignKey(el => el.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(el => el.InventoryItem)
                .WithMany()
                .HasForeignKey(el => el.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MobileDevice
        modelBuilder.Entity<MobileDevice>(e =>
        {
            e.HasIndex(md => md.DeviceId).IsUnique();
            e.HasIndex(md => md.LastSyncTime);
            e.HasOne(md => md.Employee)
                .WithMany()
                .HasForeignKey(md => md.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(md => md.User)
                .WithMany()
                .HasForeignKey(md => md.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
