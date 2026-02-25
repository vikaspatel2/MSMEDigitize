using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MSMEDigitize.Core.Entities;
using Microsoft.EntityFrameworkCore;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Interfaces;
using System.Reflection;

namespace MSMEDigitize.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ITenantContext? _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    // Tenant Management
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<MSMEDigitize.Core.Entities.Department> Departments => Set<MSMEDigitize.Core.Entities.Department>();
    public DbSet<MSMEDigitize.Core.Entities.Designation> Designations => Set<MSMEDigitize.Core.Entities.Designation>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();
    public DbSet<ApiIntegration> ApiIntegrations => Set<ApiIntegration>();

    // Subscriptions
    public DbSet<SubscriptionPlanDefinition> SubscriptionPlans => Set<SubscriptionPlanDefinition>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<MSMEDigitize.Core.Entities.Subscription> Subscriptions => Set<MSMEDigitize.Core.Entities.Subscription>();
    public DbSet<SubscriptionInvoice> SubscriptionInvoices => Set<SubscriptionInvoice>();
    public DbSet<UsageTracking> UsageTrackings => Set<UsageTracking>();

    // GST
    public DbSet<GSTProfile> GSTProfiles => Set<GSTProfile>();
    public DbSet<GSTReturn> GSTReturns => Set<GSTReturn>();
    public DbSet<GSTTransaction> GSTTransactions => Set<GSTTransaction>();
    public DbSet<HSNMaster> HSNMaster => Set<HSNMaster>();
    public DbSet<ITCRegister> ITCRegisters => Set<ITCRegister>();
    public DbSet<GSTNotice> GSTNotices => Set<GSTNotice>();

    // Invoicing
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RecurringInvoiceTemplate> RecurringTemplates => Set<RecurringInvoiceTemplate>();
    public DbSet<MSMEDigitize.Core.Entities.RecurringInvoiceConfig> RecurringInvoiceConfigs => Set<MSMEDigitize.Core.Entities.RecurringInvoiceConfig>();
    public DbSet<InvoiceActivity> InvoiceActivities => Set<InvoiceActivity>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<POLineItem> POLineItems => Set<POLineItem>();

    // Inventory
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockLedger> StockLedger => Set<StockLedger>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<LedgerEntry> LedgerEntry => Set<LedgerEntry>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<BatchSerial> BatchSerials => Set<BatchSerial>();

    // Payroll
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // Banking
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CashFlowForecast> CashFlowForecasts => Set<CashFlowForecast>();
    public DbSet<CashFlowItem> CashFlowItems => Set<CashFlowItem>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<LoanDocument> LoanDocuments => Set<LoanDocument>();

    // AI
    public DbSet<AIInsight> AIInsights => Set<AIInsight>();
    public DbSet<TaxOptimizationSuggestion> TaxOptimizationSuggestions => Set<TaxOptimizationSuggestion>();
    public DbSet<CustomerChurnPrediction> CustomerChurnPredictions => Set<CustomerChurnPrediction>();
    public DbSet<FraudAlert> FraudAlerts => Set<FraudAlert>();
    public DbSet<PriceOptimization> PriceOptimizations => Set<PriceOptimization>();
    public DbSet<InventoryOptimization> InventoryOptimizations => Set<InventoryOptimization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore non-entity types that EF would otherwise try to map
        modelBuilder.Ignore<MSMEDigitize.Core.Common.DomainEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filters for soft delete
        modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        ApplyTenantFilter<TenantUser>(modelBuilder);
        ApplyTenantFilter<ApiIntegration>(modelBuilder);
        ApplyTenantFilter<TenantSubscription>(modelBuilder);
        ApplyTenantFilter<MSMEDigitize.Core.Entities.Subscription>(modelBuilder);
        ApplyTenantFilter<GSTProfile>(modelBuilder);
        ApplyTenantFilter<GSTReturn>(modelBuilder);
        ApplyTenantFilter<Customer>(modelBuilder);
        ApplyTenantFilter<Invoice>(modelBuilder);
        ApplyTenantFilter<Payment>(modelBuilder);
        ApplyTenantFilter<Vendor>(modelBuilder);
        ApplyTenantFilter<Product>(modelBuilder);
        ApplyTenantFilter<Warehouse>(modelBuilder);
        ApplyTenantFilter<Employee>(modelBuilder);
        ApplyTenantFilter<Payslip>(modelBuilder);
        ApplyTenantFilter<BankAccount>(modelBuilder);
        ApplyTenantFilter<BankTransaction>(modelBuilder);
        ApplyTenantFilter<Expense>(modelBuilder);
        ApplyTenantFilter<AIInsight>(modelBuilder);

        // Owned types (value objects stored as columns)
        modelBuilder.Entity<Tenant>().OwnsOne(t => t.RegisteredAddress);
        modelBuilder.Entity<Tenant>().OwnsOne(t => t.OperationalAddress);
        modelBuilder.Entity<Customer>().OwnsOne(c => c.BillingAddress);
        modelBuilder.Entity<Customer>().OwnsOne(c => c.ShippingAddress);
        modelBuilder.Entity<Vendor>().OwnsOne(v => v.Address);
        modelBuilder.Entity<Employee>().OwnsOne(e => e.PermanentAddress);
        modelBuilder.Entity<Employee>().OwnsOne(e => e.CurrentAddress);
        modelBuilder.Entity<Warehouse>().OwnsOne(w => w.Address);

        // Critical unique indexes
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();
        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.TenantId, p.SKU }).IsUnique();
        modelBuilder.Entity<GSTReturn>()
            .HasIndex(g => new { g.TenantId, g.ReturnType, g.Year, g.Month }).IsUnique();
        modelBuilder.Entity<Payslip>()
            .HasIndex(p => new { p.TenantId, p.EmployeeId, p.Year, p.Month }).IsUnique();
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.GSTIN).IsUnique();
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Slug).IsUnique();

        // Performance indexes
        modelBuilder.Entity<Invoice>().HasIndex(i => new { i.TenantId, i.Status, i.DueDate });
        modelBuilder.Entity<Invoice>().HasIndex(i => new { i.TenantId, i.CustomerId, i.InvoiceDate });
        modelBuilder.Entity<BankTransaction>().HasIndex(b => new { b.BankAccountId, b.TransactionDate });
        modelBuilder.Entity<BankTransaction>().HasIndex(b => new { b.TenantId, b.ReconciliationStatus });
        modelBuilder.Entity<Product>().HasIndex(p => new { p.TenantId, p.HSNCode });
        modelBuilder.Entity<Product>().HasIndex(p => new { p.TenantId, p.Barcode });
        modelBuilder.Entity<AIInsight>().HasIndex(a => new { a.TenantId, a.InsightType, a.Status });
    }

    private static void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : TenantEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = _tenantContext?.UserId.ToString() ?? "system";

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = userId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Added && _tenantContext != null)
            {
                entry.Entity.TenantId = _tenantContext.TenantId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}