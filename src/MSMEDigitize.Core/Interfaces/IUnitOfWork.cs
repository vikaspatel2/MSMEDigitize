// Entity types matching AppDbContext's registered types
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.AI;
// Root entities (unique, no conflicts)
using MSMEDigitize.Core.Entities;

namespace MSMEDigitize.Core.Interfaces;

/// <summary>
/// Unit of Work - typed repository access and transaction management.
/// Entity types match AppDbContext's registered EF Core entity sets.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Tenant & Organization
    IRepository<Tenant>               Tenants              { get; }
    IRepository<TenantUser>           TenantUsers          { get; }
    IRepository<Department>           Departments          { get; }
    IRepository<Designation>          Designations         { get; }
    IRepository<TenantSetting>        TenantSettings       { get; }
    IRepository<Notification>         Notifications        { get; }

    // Subscriptions
    IRepository<Subscription>         Subscriptions        { get; }
    IRepository<SubscriptionPlan>     SubscriptionPlans    { get; }

    // Sales & Invoicing (from Invoicing namespace)
    IRepository<Customer>             Customers            { get; }
    IRepository<Invoice>              Invoices             { get; }
    IRepository<InvoiceLineItem>      InvoiceLineItems     { get; }
    IRepository<Payment>              Payments             { get; }
    IRepository<CreditNote>           CreditNotes          { get; }

    // Purchasing
    IRepository<Vendor>               Vendors              { get; }
    IRepository<PurchaseOrder>        PurchaseOrders       { get; }
    IRepository<PurchaseInvoice>      PurchaseInvoices     { get; }
    IRepository<SalesOrder>           SalesOrders          { get; }

    // Inventory
    IRepository<Product>              Products             { get; }
    IRepository<ProductCategory>      ProductCategories    { get; }
    IRepository<StockMovement>        StockMovements       { get; }

    // HR & Payroll
    IRepository<Employee>             Employees            { get; }
    IRepository<PayrollRun>           Payrolls             { get; }
    IRepository<Attendance>           Attendances          { get; }
    IRepository<LeaveRequest>         LeaveRequests        { get; }

    // Finance & Banking
    IRepository<ChartOfAccount>       ChartOfAccounts      { get; }
    IRepository<LedgerEntry>          LedgerEntries        { get; }
    IRepository<BankAccount>          BankAccounts         { get; }

    // GST & Compliance
    IRepository<GSTReturn>            GSTReturns           { get; }
    IRepository<EWayBill>             EWayBills            { get; }

    // CRM
    IRepository<CustomerInteraction>  CustomerInteractions { get; }

    // Transaction management
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
