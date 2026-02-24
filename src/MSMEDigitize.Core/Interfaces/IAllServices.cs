// IAllServices.cs — Unique high-level service interfaces not defined in IServices.cs
// Core infrastructure interfaces (IRepository, IUnitOfWork, ICacheService, IGSTService,
// IAIService, IPDFService, IPaymentGatewayService, IBankingService, INotificationService,
// ITallyIntegrationService) are defined in IServices.cs

using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.DTOs;
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Core.Interfaces;

// ─── Authentication ───────────────────────────────────────────────────────────
public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<Result<LoginResponseDto>> RegisterBusinessAsync(RegisterBusinessDto dto, CancellationToken ct = default);
    Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string email, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
    Task<Result<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task RevokeTokenAsync(string userId, CancellationToken ct = default);
}

// ─── Current User (per-request) ───────────────────────────────────────────────
public interface ICurrentUserService
{
    Guid? TenantId { get; }
    Guid UserId { get; }
    string Email { get; }
    string FullName { get; }
    string BusinessName { get; }
    TenantRole Role { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    bool IsSuperAdmin { get; }
}

// ─── Token Service ────────────────────────────────────────────────────────────
public interface ITokenService
{
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, string role);
    string GenerateRefreshToken();
    bool ValidateToken(string token, out Guid userId);
}


// ─── Subscription ─────────────────────────────────────────────────────────────
public interface ISubscriptionService
{
    Task<IEnumerable<SubscriptionPlanDto>> GetPlansAsync(CancellationToken ct = default);
    Task<Result<SubscriptionStatusDto>> GetStatusAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<SubscriptionStatusDto>> SubscribeAsync(Guid tenantId, CreateSubscriptionDto dto, CancellationToken ct = default);
    Task<Result<bool>> CancelAsync(Guid tenantId, CancellationToken ct = default);
    Task HandleRazorpayWebhookAsync(string payload, string signature, CancellationToken ct = default);
    Task<Result<bool>> CheckFeatureAccessAsync(Guid tenantId, string feature, CancellationToken ct = default);
    Task<bool> IsWithinLimitsAsync(Guid tenantId, string limitType, CancellationToken ct = default);
}

// ─── Analytics / Dashboard ────────────────────────────────────────────────────
public interface IAnalyticsService
{
    Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(Guid tenantId, string period, CancellationToken ct = default);
    Task<Result<RevenueAnalyticsDto>> GetRevenueAnalyticsAsync(Guid tenantId, DateTime from, DateTime to, string groupBy, CancellationToken ct = default);
    Task<Result<CashFlowForecastDto>> GetCashFlowForecastAsync(Guid tenantId, int forecastDays, CancellationToken ct = default);
}

// ─── Invoice Service ──────────────────────────────────────────────────────────
public interface IInvoiceService
{
    // PDF / email / generation
    Task<string> GenerateInvoiceNumberAsync(Guid tenantId, MSMEDigitize.Core.Enums.InvoiceType type);
    Task<byte[]> GeneratePDFAsync(Guid invoiceId, Guid tenantId);
    Task<bool> SendInvoiceEmailAsync(Guid invoiceId, Guid tenantId, string? additionalEmail = null);
    Task<MSMEDigitize.Core.Entities.Invoicing.Invoice> CreateFromTemplateAsync(Guid recurringConfigId, Guid tenantId);
    Task<string?> GenerateEInvoiceAsync(Guid invoiceId, Guid tenantId);
    Task<string?> GenerateEWayBillAsync(Guid invoiceId, Guid tenantId);
    // Controller-facing CRUD
    Task<MSMEDigitize.Core.DTOs.PagedResult<object>> GetInvoicesAsync(Guid tenantId, int page, int size, string? search, MSMEDigitize.Core.Enums.InvoiceStatus? status, CancellationToken ct = default);
    Task<object?> GetInvoiceAsync(Guid invoiceId, Guid tenantId, CancellationToken ct = default);
    Task<MSMEDigitize.Core.DTOs.DashboardMetricsDto> GetDashboardMetricsAsync(Guid tenantId, CancellationToken ct = default);
    Task<object> GetGSTSummaryAsync(Guid tenantId, int month, int year, CancellationToken ct = default);
    Task<MSMEDigitize.Core.Common.Result<object>> CreateInvoiceAsync(Guid tenantId, object dto, CancellationToken ct = default);
    Task<MSMEDigitize.Core.Common.Result<bool>> SendInvoiceAsync(Guid invoiceId, Guid tenantId, CancellationToken ct = default);
    Task<MSMEDigitize.Core.Common.Result<bool>> RecordPaymentAsync(Guid tenantId, object dto, CancellationToken ct = default);
    Task<byte[]> GetInvoicePdfAsync(Guid invoiceId, Guid tenantId, CancellationToken ct = default);
}

// ─── Storage Service ──────────────────────────────────────────────────────────
public interface IStorageService
{
    Task<string> UploadAsync(byte[] data, string fileName, string contentType);
    Task<byte[]> DownloadAsync(string url);
    Task DeleteAsync(string url);
}

// ─── Payroll Service ──────────────────────────────────────────────────────────
public interface IPayrollService
{
    Task<Result<object>> ProcessPayrollAsync(Guid tenantId, int month, int year, CancellationToken ct = default);
    Task<Result<byte[]>> GeneratePayslipAsync(Guid employeeId, int month, int year, CancellationToken ct = default);
    Task<object?> GetPayrollSummaryAsync(Guid tenantId, int month, int year, CancellationToken ct = default);
    Task<Result<bool>> MarkAttendanceAsync(Guid tenantId, Guid employeeId, DateTime date, MSMEDigitize.Core.Enums.AttendanceStatus status, CancellationToken ct = default);
}