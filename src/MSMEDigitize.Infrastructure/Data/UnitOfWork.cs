using Microsoft.EntityFrameworkCore.Storage;
using MSMEDigitize.Core.Entities;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure.Data.Repositories;

// ─── Entity type aliases (match AppDbContext's usings to avoid Set<T> mismatch) ───
// Root entities (no conflict)
using Vendor = MSMEDigitize.Core.Entities.Invoicing.Vendor;
using Product = MSMEDigitize.Core.Entities.Inventory.Product;
using ProductCategory = MSMEDigitize.Core.Entities.Inventory.ProductCategory;
using Employee = MSMEDigitize.Core.Entities.Payroll.Employee;
using CoreDepartment = MSMEDigitize.Core.Entities.Department;
using CoreDesignation = MSMEDigitize.Core.Entities.Designation;
using Attendance = MSMEDigitize.Core.Entities.Payroll.Attendance;
using LeaveRequest = MSMEDigitize.Core.Entities.Payroll.LeaveRequest;
using ChartOfAccount = MSMEDigitize.Core.Entities.ChartOfAccount;
using LedgerEntry = MSMEDigitize.Core.Entities.LedgerEntry;
using BankAccount = MSMEDigitize.Core.Entities.Banking.BankAccount;
using GSTReturn = MSMEDigitize.Core.Entities.GST.GSTReturn;
using EWayBill = MSMEDigitize.Core.Entities.EWayBill;
using CreditNote = MSMEDigitize.Core.Entities.CreditNote;
using CustomerInteraction = MSMEDigitize.Core.Entities.CustomerInteraction;
using StockMovement = MSMEDigitize.Core.Entities.StockMovement;
using PurchaseOrder = MSMEDigitize.Core.Entities.Invoicing.PurchaseOrder;
using PurchaseInvoice = MSMEDigitize.Core.Entities.PurchaseInvoice;
using SalesOrder = MSMEDigitize.Core.Entities.SalesOrder;
using PayrollRun = MSMEDigitize.Core.Entities.Payroll.PayrollRun;
using TenantSetting = MSMEDigitize.Core.Entities.TenantSetting;
using Notification = MSMEDigitize.Core.Entities.Notification;
using TenantUser = MSMEDigitize.Core.Entities.Tenants.TenantUser;
using CoreSubscription = MSMEDigitize.Core.Entities.Subscription;  // root namespace Subscription (has BillingCycle, FinalAmount)
// Subfolder entities (used in AppDbContext via their namespaces)
using Tenant = MSMEDigitize.Core.Entities.Tenants.Tenant;
using Customer = MSMEDigitize.Core.Entities.Invoicing.Customer;
using Invoice = MSMEDigitize.Core.Entities.Invoicing.Invoice;
using InvoiceLineItem = MSMEDigitize.Core.Entities.Invoicing.InvoiceLineItem;
using Payment = MSMEDigitize.Core.Entities.Invoicing.Payment;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Tenants = new Repository<Tenant>(context);
        TenantUsers = new Repository<TenantUser>(context);
        Departments = new Repository<CoreDepartment>(context);
        Designations = new Repository<CoreDesignation>(context);
        TenantSettings = new Repository<TenantSetting>(context);
        Notifications = new Repository<Notification>(context);
        Subscriptions = new Repository<CoreSubscription>(context);
        SubscriptionPlans = new Repository<SubscriptionPlan>(context);
        Customers = new Repository<Customer>(context);
        Invoices = new Repository<Invoice>(context);
        InvoiceLineItems = new Repository<InvoiceLineItem>(context);
        Payments = new Repository<Payment>(context);
        CreditNotes = new Repository<CreditNote>(context);
        Vendors = new Repository<Vendor>(context);
        PurchaseOrders = new Repository<PurchaseOrder>(context);
        PurchaseInvoices = new Repository<PurchaseInvoice>(context);
        SalesOrders = new Repository<SalesOrder>(context);
        Products = new Repository<Product>(context);
        ProductCategories = new Repository<ProductCategory>(context);
        StockMovements = new Repository<StockMovement>(context);
        Employees = new Repository<Employee>(context);
        Payrolls = new Repository<PayrollRun>(context);
        Attendances = new Repository<Attendance>(context);
        LeaveRequests = new Repository<LeaveRequest>(context);
        ChartOfAccounts = new Repository<ChartOfAccount>(context);
        LedgerEntries = new Repository<LedgerEntry>(context);
        BankAccounts = new Repository<BankAccount>(context);
        GSTReturns = new Repository<GSTReturn>(context);
        EWayBills = new Repository<EWayBill>(context);
        CustomerInteractions = new Repository<CustomerInteraction>(context);
    }

    public IRepository<Tenant> Tenants { get; }
    public IRepository<TenantUser> TenantUsers { get; }
    public IRepository<CoreDepartment> Departments { get; }
    public IRepository<CoreDesignation> Designations { get; }
    public IRepository<TenantSetting> TenantSettings { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<CoreSubscription> Subscriptions { get; }
    public IRepository<SubscriptionPlan> SubscriptionPlans { get; }
    public IRepository<Customer> Customers { get; }
    public IRepository<Invoice> Invoices { get; }
    public IRepository<InvoiceLineItem> InvoiceLineItems { get; }
    public IRepository<Payment> Payments { get; }
    public IRepository<CreditNote> CreditNotes { get; }
    public IRepository<Vendor> Vendors { get; }
    public IRepository<PurchaseOrder> PurchaseOrders { get; }
    public IRepository<PurchaseInvoice> PurchaseInvoices { get; }
    public IRepository<SalesOrder> SalesOrders { get; }
    public IRepository<Product> Products { get; }
    public IRepository<ProductCategory> ProductCategories { get; }
    public IRepository<StockMovement> StockMovements { get; }
    public IRepository<Employee> Employees { get; }
    public IRepository<PayrollRun> Payrolls { get; }
    public IRepository<Attendance> Attendances { get; }
    public IRepository<LeaveRequest> LeaveRequests { get; }
    public IRepository<ChartOfAccount> ChartOfAccounts { get; }
    public IRepository<LedgerEntry> LedgerEntries { get; }
    public IRepository<BankAccount> BankAccounts { get; }
    public IRepository<GSTReturn> GSTReturns { get; }
    public IRepository<EWayBill> EWayBills { get; }
    public IRepository<CustomerInteraction> CustomerInteractions { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
            await _transaction.CommitAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }


}