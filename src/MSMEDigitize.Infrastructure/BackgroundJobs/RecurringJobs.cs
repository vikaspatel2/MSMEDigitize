using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure.Data;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Infrastructure.BackgroundJobs;

public class InvoiceReminderJob
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<InvoiceReminderJob> _logger;

    public InvoiceReminderJob(AppDbContext db, IEmailService email, ISmsService sms, ILogger<InvoiceReminderJob> logger)
    {
        _db = db; _emailService = email; _smsService = sms; _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendOverdueRemindersAsync()
    {
        var overdueInvoices = await _db.Invoices
            .Include(i => i.Customer)
            .Where(i => !i.IsDeleted && i.DueDate < DateTime.UtcNow.Date
                && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid))
            .ToListAsync();

        foreach (var invoice in overdueInvoices)
        {
            invoice.Status = InvoiceStatus.Overdue;

            if (!string.IsNullOrEmpty(invoice.Customer.Email))
            {
                await _emailService.SendEmailAsync(
                    invoice.Customer.Email,
                    $"Payment Due: Invoice {invoice.InvoiceNumber}",
                    $"<p>Dear {invoice.Customer.Name}, Invoice {invoice.InvoiceNumber} of ₹{invoice.BalanceAmount:N2} is overdue. Please pay immediately.</p>");
            }

            if (!string.IsNullOrEmpty(invoice.Customer.Phone))
            {
                await _smsService.SendPaymentReminderAsync(
                    invoice.Customer.Phone,
                    invoice.Customer.Name,
                    invoice.BalanceAmount,
                    invoice.InvoiceNumber);
            }

            _logger.LogInformation("Sent overdue reminder for invoice {InvoiceNumber}", invoice.InvoiceNumber);
        }

        await _db.SaveChangesAsync();
    }
}

public class LowStockAlertJob
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<LowStockAlertJob> _logger;

    public LowStockAlertJob(AppDbContext db, IEmailService email, ILogger<LowStockAlertJob> logger)
    {
        _db = db; _emailService = email; _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task CheckLowStockAsync()
    {
        var lowStockProducts = await _db.Products
            .Where(p => !p.IsDeleted && p.TrackInventory && p.CurrentStock <= p.ReorderPoint && p.IsActive)
            .ToListAsync();

        var grouped = lowStockProducts.GroupBy(p => p.TenantId);
        foreach (var group in grouped)
        {
            var tenantId = group.Key;
            var adminUser = await _db.TenantUsers
                .Where(tu => tu.TenantId == tenantId && tu.IsOwner)
                .FirstOrDefaultAsync();

            if (adminUser?.Email != null)
            {
                var items = string.Join("<br/>", group.Select(p => $"{p.Name}: {p.CurrentStock} {p.Unit} (Min: {p.MinStockLevel})"));
                await _emailService.SendEmailAsync(adminUser.Email,
                    $"Low Stock Alert - {group.Count()} Products",
                    $"<h3>Low Stock Alert</h3><p>The following products need reordering:</p>{items}");
            }
        }
    }
}

public class SubscriptionRenewalJob
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscriptionRenewalJob> _logger;

    public SubscriptionRenewalJob(AppDbContext db, IEmailService email, ILogger<SubscriptionRenewalJob> logger)
    {
        _db = db; _emailService = email; _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessExpiringSubscriptionsAsync()
    {
        var expiringSoon = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.EndDate <= DateTime.UtcNow.AddDays(7)
                && s.EndDate > DateTime.UtcNow)
            .ToListAsync();

        foreach (var sub in expiringSoon)
        {
            var daysLeft = (sub.EndDate - DateTime.UtcNow).Days;
            var adminUser = await _db.TenantUsers
                .Where(tu => tu.TenantId == sub.TenantId && tu.IsOwner)
                .FirstOrDefaultAsync();

            if (adminUser?.Email != null)
            {
                await _emailService.SendEmailAsync(adminUser.Email,
                    $"Your {sub.Plan.Name} Plan Expires in {daysLeft} Days",
                    $"<p>Your {sub.Plan.Name} subscription expires on {sub.EndDate:dd MMM yyyy}. Renew to avoid interruption.</p>");
            }
        }

        var expired = await _db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate < DateTime.UtcNow)
            .ToListAsync();
        foreach (var sub in expired)
            sub.Status = SubscriptionStatus.Expired;

        await _db.SaveChangesAsync();
    }
}

public class GSTReminderJob
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<GSTReminderJob> _logger;

    public GSTReminderJob(AppDbContext db, IEmailService email, ILogger<GSTReminderJob> logger)
    {
        _db = db; _emailService = email; _logger = logger;
    }

    public async Task SendGSTFilingRemindersAsync()
    {
        var today = DateTime.UtcNow.Day;
        if (today != 7 && today != 10) return;

        var tenants = await _db.Tenants
            .Where(t => !t.IsDeleted && t.GSTNumber != null && t.Status == TenantStatus.Active)
            .ToListAsync();

        foreach (var tenant in tenants)
        {
            var adminUser = await _db.TenantUsers
                .Where(tu => tu.TenantId == tenant.Id && tu.IsOwner)
                .FirstOrDefaultAsync();

            if (adminUser?.Email != null)
            {
                var dueDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 20);
                await _emailService.SendEmailAsync(adminUser.Email,
                    "GSTR-3B Filing Reminder",
                    $"<p>Dear {tenant.BusinessName}, GSTR-3B for {DateTime.UtcNow.AddMonths(-1):MMMM yyyy} is due by {dueDate:dd MMM yyyy}.</p>");
            }
        }
    }
}