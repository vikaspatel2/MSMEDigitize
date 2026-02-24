using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure.Data;
using MSMEDigitize.Core.DTOs;
using System.Linq;

namespace MSMEDigitize.Infrastructure.Services;

public class AIServiceImpl : IAIService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<AIServiceImpl> _logger;

    public AIServiceImpl(AppDbContext db, ICacheService cache, ILogger<AIServiceImpl> logger)
    {
        _db = db; _cache = cache; _logger = logger;
    }

    public async Task<decimal> PredictCashFlowAsync(Guid tenantId, int daysAhead, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var pendingReceivables = await _db.Invoices
            .Where(i => i.TenantId == tenantId &&
                       i.Status != InvoiceStatus.Paid &&
                       i.Status != InvoiceStatus.Cancelled &&
                       i.DueDate <= today.AddDays(daysAhead))
            .SumAsync(i => i.BalanceAmount, ct);

        var upcomingExpenses = await _db.Expenses
            .Where(e => e.TenantId == tenantId &&
                       e.ExpenseDate >= today &&
                       e.ExpenseDate <= today.AddDays(daysAhead))
            .SumAsync(e => e.Amount, ct);

        return Math.Round(pendingReceivables * 0.8m - upcomingExpenses, 2);
    }

    public async Task<IEnumerable<AIInsight>> GenerateInsightsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var insights = new List<AIInsight>();
        var today = DateTime.UtcNow;

        var overdueInvoices = await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.Status == InvoiceStatus.Overdue)
            .ToListAsync(ct);

        if (overdueInvoices.Any())
        {
            var totalOverdue = overdueInvoices.Sum(i => i.BalanceAmount);
            insights.Add(new AIInsight
            {
                TenantId         = tenantId,
                InsightType      = AIInsightType.CashFlow,
                Title            = $"{overdueInvoices.Count} Overdue Invoices Detected",
                Summary          = $"₹{totalOverdue:N0} pending in {overdueInvoices.Count} overdue invoices. Immediate follow-up recommended.",
                ActionRecommended = "Send payment reminders",
                ActionUrl        = "/invoices?filter=overdue",
                ConfidenceScore  = 0.95m,
                RiskLevel        = RiskLevel.High,
                Status           = AIInsightStatus.Active
            });
        }

        var lowStockCount = await _db.Products
            .Where(p => p.TenantId == tenantId && p.CurrentStock <= p.MinStockLevel && !p.IsDeleted)
            .CountAsync(ct);

        if (lowStockCount > 0)
        {
            insights.Add(new AIInsight
            {
                TenantId          = tenantId,
                InsightType       = AIInsightType.Inventory,
                Title             = $"{lowStockCount} Products Below Reorder Level",
                Summary           = $"{lowStockCount} products need restocking to avoid stockouts.",
                ActionRecommended = "Create purchase orders",
                ActionUrl         = "/products?filter=lowstock",
                ConfidenceScore   = 0.90m,
                RiskLevel         = RiskLevel.Medium,
                Status            = AIInsightStatus.Active
            });
        }

        return insights;
    }

    public async Task<decimal> PredictCustomerChurnAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { customerId }, ct);
        if (customer == null) return 0;

        var recentInvoices = await _db.Invoices
            .Where(i => i.CustomerId == customerId && i.InvoiceDate >= DateTime.UtcNow.AddMonths(-3))
            .CountAsync(ct);

        // Simple churn model: no recent invoices = higher churn risk
        return recentInvoices == 0 ? 0.8m : recentInvoices < 2 ? 0.4m : 0.1m;
    }

    public async Task<decimal> GetOptimalPriceAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { productId }, ct);
        if (product == null) return 0;

        // Simple pricing: suggest 15% above selling price if competitive
        return Math.Round(product.SellingPrice * 1.15m, 2);
    }

    public async Task<decimal> ForecastInventoryDemandAsync(Guid productId, int days, CancellationToken ct = default)
    {
        var historicalSales = await _db.InvoiceLineItems
            .Where(li => li.ProductId == productId && li.CreatedAt >= DateTime.UtcNow.AddDays(-90))
            .SumAsync(li => li.Quantity, ct);

        var dailyAvg = historicalSales / 90m;
        return Math.Round(dailyAvg * days, 2);
    }

    public async Task<bool> DetectFraudAsync(Guid transactionId, string transactionType, CancellationToken ct = default)
    {
        // Simplified fraud detection
        await Task.CompletedTask;
        _logger.LogInformation("FraudDetect check for {TransactionType} {TransactionId}", transactionType, transactionId);
        return false; // No fraud detected in basic implementation
    }

    public async Task<decimal> CalculateLoanEligibilityAsync(Guid tenantId, CancellationToken ct = default)
    {
        var annualRevenue = await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.Status == InvoiceStatus.Paid
                     && i.InvoiceDate >= DateTime.UtcNow.AddYears(-1))
            .SumAsync(i => i.TotalAmount, ct);
        return Math.Round(annualRevenue / 2, 0);
    }

    public async Task<IEnumerable<MSMEDigitize.Core.DTOs.AIInsightDto>> GetInsightsAsync(Guid tenantId, MSMEDigitize.Core.Enums.AIInsightType? type = null, int limit = 10, CancellationToken ct = default)
    {
        var query = _db.AIInsights.Where(a => a.TenantId == tenantId && !a.IsDeleted);
        if (type.HasValue) query = query.Where(a => a.InsightType == type.Value);
        var items = await query.OrderByDescending(a => a.Priority).Take(limit).ToListAsync(ct);
        return items.Select(a => new MSMEDigitize.Core.DTOs.AIInsightDto(
            a.Id, a.InsightType.ToString(), a.Title, a.Summary, a.ActionRecommended, a.ConfidenceScore, a.CreatedAt));
    }
}
