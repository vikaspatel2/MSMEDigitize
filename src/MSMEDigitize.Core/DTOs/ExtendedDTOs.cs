// ExtendedDTOs.cs — MSMEDigitize v3 (FIXED & STABLE)
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Core.DTOs;

// ═══════════════════════════════════════════════════════
// AUTH DTOs
// ═══════════════════════════════════════════════════════

public class LoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

public class RegisterBusinessDto
{
    public string BusinessName { get; set; } = default!;
    public string LegalName { get; set; } = default!;
    public string GSTIN { get; set; } = default!;
    public string PAN { get; set; } = default!;
    public string MsmeCategory { get; set; } = default!;
    public string BusinessType { get; set; } = default!;
    public string Industry { get; set; } = default!;
    public string City { get; set; } = default!;
    public string State { get; set; } = default!;
    public string Pincode { get; set; } = default!;

    public string PrimaryContactEmail { get; set; } = default!;
    public string PrimaryContactPhone { get; set; } = default!;
    public string OwnerFullName { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
    public TenantUserDto User { get; set; } = default!;
}

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════
// TENANT / USER DTOs
// ═══════════════════════════════════════════════════════

public class TenantUserDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }       // Identity Guid
    public Guid TenantId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class TenantDto
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = "";
    public string GSTIN { get; set; } = "";
    public string PAN { get; set; } = "";
    public string RegisteredAddress { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string InvoicePrefix { get; set; } = "INV-";
    public string CurrentPlan { get; set; } = "";
}

// ═══════════════════════════════════════════════════════
// SUBSCRIPTION DTOs
// ═══════════════════════════════════════════════════════

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }

    public int MaxInvoices { get; set; }
    public int MaxEmployees { get; set; }

    public bool HasGST { get; set; }
    public bool HasPayroll { get; set; }
    public bool HasAI { get; set; }
    public bool HasTally { get; set; }

    public List<string> Features { get; set; } = new();

    public int? MaxUsers { get; set; }
}

public class SubscriptionStatusDto
{
    public string PlanName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }
    public bool IsTrialing { get; set; }

    public int DaysRemaining { get; set; }

    public int InvoicesUsed { get; set; }
    public int InvoicesLimit { get; set; }

    public int EmployeesUsed { get; set; }
    public int EmployeesLimit { get; set; }
    public SubscriptionPlanType SubscriptionPlan { get; set; }
    public SubscriptionStatus Status { get; set; }

    public bool IsExpiringSoon { get; set; }
    public decimal MonthlyPrice { get; set; }
    public int CurrentUsers { get; set; }
    public bool IsTrial { get; set; }
    public int InvoicesThisMonth { get; set; }
    public int MaxUsers { get; set; }
    public int MaxInvoices { get; set; }
}

public class CreateSubscriptionDto
{
    public Guid PlanId { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
    public string? PaymentId { get; set; }
    public string? OrderId { get; set; }
    public bool? IsAnnual { get; set; }
    public string? RazorpayPaymentId { get; set; }
}

// ═══════════════════════════════════════════════════════
// DASHBOARD / ANALYTICS DTOs
// ═══════════════════════════════════════════════════════

public class DashboardSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }

    public int TotalInvoices { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int TotalEmployees { get; set; }

    public decimal BankBalance { get; set; }
    public decimal GSTPayable { get; set; }

    public List<MonthlyRevenueDto> MonthlyRevenueChart { get; set; } = new();
    public List<RecentInvoiceDto> RecentInvoices { get; set; } = new();
    public List<StockAlertDto> StockAlerts { get; set; } = new();
    public List<AIInsightDto> AIInsights { get; set; } = new();
}

public class RevenueAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal GrowthPercent { get; set; }

    public List<MonthlyRevenueDto> MonthlyBreakdown { get; set; } = new();
    public List<CategoryExpenseDto> ExpenseByCategory { get; set; } = new();
}

public class CategoryExpenseDto
{
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
}

public class CashFlowForecastDto
{
    public decimal CurrentBalance { get; set; }
    public decimal ProjectedInflow { get; set; }
    public decimal ProjectedOutflow { get; set; }
    public decimal ProjectedNet { get; set; }
    public decimal NetCashFlow { get; set; }

    public string? Alert { get; set; }

    public List<WeeklyCashFlowDto> WeeklyForecast { get; set; } = new();
    public List<DailyForecastDto> DailyForecast { get; set; } = new();
}

public class WeeklyCashFlowDto
{
    public DateTime WeekStart { get; set; }
    public decimal Inflow { get; set; }
    public decimal Outflow { get; set; }
    public decimal Net { get; set; }
}

public class DailyForecastDto
{
    public DateTime Date { get; set; }
    public decimal Inflow { get; set; }
    public decimal Outflow { get; set; }
    public decimal Balance { get; set; }
}

// ═══════════════════════════════════════════════════════
// AI DTO (ONLY ONE VERSION)
// ═══════════════════════════════════════════════════════

public class AIInsightDto
{
    public Guid Id { get; set; }
    public string InsightType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ActionRecommended { get; set; }
    public decimal ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public AIInsightDto(Guid id, string insightType, string title, string summary, string? actionRecommended, decimal confidenceScore, DateTime createdAt)
    {
        Id = id;
        InsightType = insightType;
        Title = title;
        Summary = summary;
        ActionRecommended = actionRecommended;
        ConfidenceScore = confidenceScore;
        CreatedAt = createdAt;
    }
}

public class UpcomingComplianceDto
{
    public string ComplianceName { get; set; } = default!;
    public DateTime DueDate { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public bool IsOverdue { get; set; }
}

public class TopCustomerDto
{
    public string CustomerName { get; set; } = default!;
    public decimal TotalAmount { get; set; }
}