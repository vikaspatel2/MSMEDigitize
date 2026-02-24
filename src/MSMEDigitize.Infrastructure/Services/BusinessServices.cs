using MSMEDigitize.Core.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
namespace MSMEDigitize.Infrastructure.Services;

// ─── Auth Service ──────────────────────────────────────────────────────────
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, ITokenService tokenService,
        INotificationService notificationService, IConfiguration config,
        ILogger<AuthService> logger)
    {
        _db = db; _tokenService = tokenService;
        _notificationService = notificationService; _config = config; _logger = logger;
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.TenantUsers
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.IsActive, ct);

        if (user == null) return Result<LoginResponseDto>.Failure("Invalid email or password");

        var hasher = new PasswordHasher<TenantUser>();
        var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", dto.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            return Result<LoginResponseDto>.Failure("Invalid email or password");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var accessToken = _tokenService.GenerateAccessToken(
            user.Id, user.TenantId, user.Email, user.Role.ToString());
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _db.SaveChangesAsync(ct);

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 86400,
            User = new TenantUserDto
            {
                Id = user.Id,
                UserId = user.Id,
                TenantId = user.TenantId,

                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,

                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                ProfileImageUrl = user.ProfileImageUrl
            }
        });
    }

    public async Task<Result<LoginResponseDto>> RegisterBusinessAsync(RegisterBusinessDto dto, CancellationToken ct = default)
    {
        // Resolve email — support both field names
        var contactEmail = string.IsNullOrEmpty(dto.PrimaryContactEmail) ? dto.PrimaryContactEmail : dto.PrimaryContactEmail;
        var contactPhone = string.IsNullOrEmpty(dto.PrimaryContactPhone) ? dto.PrimaryContactPhone : dto.PrimaryContactPhone;
        var ownerName = string.IsNullOrEmpty(dto.OwnerFullName) ? dto.OwnerFullName : dto.OwnerFullName;

        if (await _db.Tenants.AnyAsync(t => t.GSTIN == dto.GSTIN, ct))
            return Result<LoginResponseDto>.Failure("A business with this GSTIN is already registered");

        if (await _db.TenantUsers.AnyAsync(u => u.Email.ToLower() == contactEmail.ToLower(), ct))
            return Result<LoginResponseDto>.Failure("An account with this email already exists");

        if (!IsValidGSTIN(dto.GSTIN))
            return Result<LoginResponseDto>.Failure("Invalid GSTIN format");

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var slug = GenerateSlug(dto.BusinessName);

        var slugBase = slug;
        var counter = 1;
        while (await _db.Tenants.AnyAsync(t => t.Slug == slug, ct))
            slug = $"{slugBase}-{counter++}";

        var tenant = new Tenant
        {
            Id = tenantId,
            BusinessName = dto.BusinessName,
            LegalName = dto.LegalName,
            Slug = slug,
            GSTIN = dto.GSTIN,
            PAN = dto.PAN,
            MsmeCategory = Enum.TryParse<MsmeCategory>(dto.MsmeCategory, true, out var msmeCat)
    ? msmeCat
    : MsmeCategory.Micro,

            BusinessType = Enum.TryParse<BusinessType>(dto.BusinessType, true, out var businessType)
    ? businessType
    : BusinessType.Proprietorship,
            Industry = dto.Industry,
            PrimaryContactEmail = contactEmail,
            PrimaryContactPhone = contactPhone,
            Status = TenantStatus.Trial,
            SubscriptionPlan = SubscriptionPlanType.Free,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(14),
            RegisteredAddress = new MSMEDigitize.Core.Common.Address
            {
                Line1 = "",
                City = dto.City,
                State = dto.State,
                PinCode = dto.Pincode
            }
        };

        var hasher = new PasswordHasher<TenantUser>();
        var user = new TenantUser
        {
            Id = userId,
            TenantId = tenantId,
            FullName = ownerName,
            Email = contactEmail,
            Phone = contactPhone,
            Role = TenantRole.Owner,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, dto.Password);

        _db.Tenants.Add(tenant);
        _db.TenantUsers.Add(user);

        var defaultModules = new[] { "Invoicing", "GST", "Inventory", "Analytics" };
        foreach (var module in defaultModules)
        {
            _db.TenantModules.Add(new TenantModule
            {
                TenantId = tenantId,
                ModuleName = module,
                IsEnabled = true
            });
        }

        await _db.SaveChangesAsync(ct);

        await _notificationService.SendEmailAsync(contactEmail,
            "Welcome to MSMEDigitize! 🚀",
            $"<h2>Welcome {ownerName}!</h2><p>Your 14-day free trial has started. Let's digitize your business!</p>");

        var accessToken = _tokenService.GenerateAccessToken(userId, tenantId, contactEmail, "Owner");
        var refreshToken = _tokenService.GenerateRefreshToken();

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 86400,
            User = new TenantUserDto
            {
                Id = user.Id,
                UserId = user.Id,
                TenantId = user.TenantId,

                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,

                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                ProfileImageUrl = user.ProfileImageUrl
            }
        });
    }

    public async Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var user = await _db.TenantUsers
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken
                && u.RefreshTokenExpiry > DateTime.UtcNow && u.IsActive, ct);

        if (user == null) return Result<LoginResponseDto>.Failure("Invalid or expired refresh token");

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.TenantId, user.Email, user.Role.ToString());
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _db.SaveChangesAsync(ct);

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 86400,
            User = new TenantUserDto
            {
                Id = user.Id,
                UserId = user.Id,
                TenantId = user.TenantId,

                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,

                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                ProfileImageUrl = user.ProfileImageUrl
            }
        });
    }

    public async Task SendPasswordResetEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _db.TenantUsers.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);
        if (user == null) return;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken = token;
        user.PasswordResetExpiry = DateTime.UtcNow.AddHours(2);
        await _db.SaveChangesAsync(ct);

        var resetUrl = $"{_config["AppUrl"]}/account/reset-password?token={Uri.EscapeDataString(token)}";
        await _notificationService.SendEmailAsync(email, "Reset Your Password",
            $"<p>Click <a href='{resetUrl}'>here</a> to reset your password. Link expires in 2 hours.</p>");
    }

    public async Task<Result<bool>> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.TenantUsers
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token
                && u.PasswordResetExpiry > DateTime.UtcNow, ct);

        if (user == null) return Result<bool>.Failure("Invalid or expired reset token");

        var hasher = new PasswordHasher<TenantUser>();
        user.PasswordHash = hasher.HashPassword(user, newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.TenantUsers.FirstOrDefaultAsync(u => u.Id.ToString() == userId, ct);
        if (user == null) return Result<bool>.Failure("User not found");

        var hasher = new PasswordHasher<TenantUser>();
        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", dto.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
            return Result<bool>.Failure("Current password is incorrect");

        user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task RevokeTokenAsync(string userId, CancellationToken ct = default)
    {
        var user = await _db.TenantUsers.FirstOrDefaultAsync(u => u.Id.ToString() == userId, ct);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _db.SaveChangesAsync(ct);
        }
    }

    private static bool IsValidGSTIN(string gstin)
    {
        if (string.IsNullOrEmpty(gstin) || gstin.Length != 15) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(gstin,
            @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$");
    }

    private static string GenerateSlug(string businessName) =>
        System.Text.RegularExpressions.Regex.Replace(
            businessName.ToLower().Trim().Replace(" ", "-"),
            @"[^a-z0-9\-]", "").Trim('-');
}

// ─── Subscription Service ──────────────────────────────────────────────────
public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly INotificationService _notification;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(AppDbContext db, IPaymentGatewayService paymentGateway,
        INotificationService notification, ILogger<SubscriptionService> logger)
    {
        _db = db; _paymentGateway = paymentGateway;
        _notification = notification; _logger = logger;
    }

    //public async Task<IEnumerable<SubscriptionPlanDto>> GetPlansAsync(CancellationToken ct = default)
    //{
    //    var plans = new List<SubscriptionPlanDto>
    //    {
    //        new("free", "Free Trial", 0, 0, 0, 2, 20, true, false, false, false, false, false, false, false,
    //            "Email only",
    //            new[] { "20 invoices/month", "GST filing", "Basic reports", "2 users", "Email support" }),

    //        new("starter", "Starter", 999, 9990, 17, 5, 100, true, true, false, false, false, false, false, false,
    //            "Email + Chat",
    //            new[] { "100 invoices/month", "GST filing", "E-Invoice", "Inventory management",
    //                "5 users", "WhatsApp notifications", "Customer portal", "Multi-currency" }),

    //        new("growth", "Growth", 2499, 24990, 17, 15, 500, true, true, true, true, false, false, false, false,
    //            "Priority Chat",
    //            new[] { "500 invoices/month", "All Starter features", "Payroll & HR",
    //                "Advanced analytics", "15 users", "Purchase orders", "Vendor management",
    //                "Bank reconciliation", "Loan eligibility checker" }),

    //        new("professional", "Professional", 4999, 49990, 17, 50, int.MaxValue, true, true, true, true, true, true, true, false,
    //            "Dedicated Manager",
    //            new[] { "Unlimited invoices", "All Growth features", "AI-powered insights",
    //                "Multi-warehouse", "API access", "50 users", "Custom reports",
    //                "Cash flow AI forecast", "Tax optimization AI", "Fraud detection" }),

    //        new("enterprise", "Enterprise", 0, 0, 0, int.MaxValue, int.MaxValue, true, true, true, true, true, true, true, true,
    //            "24/7 Dedicated",
    //            new[] { "Everything in Professional", "White-label option", "Unlimited users",
    //                "Custom integrations", "SLA guarantee", "On-premise option",
    //                "Custom AI models", "Dedicated infrastructure" })
    //    };

    //    return await Task.FromResult<IEnumerable<SubscriptionPlanDto>>(plans);
    //}

    public async Task<IEnumerable<SubscriptionPlanDto>> GetPlansAsync(CancellationToken ct = default)
    {
        var plans = new List<SubscriptionPlanDto>
    {
        new SubscriptionPlanDto
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Free Trial",
            MonthlyPrice = 0,
            YearlyPrice = 0,
            MaxInvoices = 20,
            MaxEmployees = 2,
            HasGST = true,
            HasPayroll = false,
            HasAI = false,
            HasTally = false,
            Features = new List<string>
            {
                "20 invoices/month",
                "GST filing",
                "Basic reports",
                "2 users",
                "Email support"
            }
        },

        new SubscriptionPlanDto
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Name = "Starter",
            MonthlyPrice = 999,
            YearlyPrice = 9990,
            MaxInvoices = 100,
            MaxEmployees = 5,
            HasGST = true,
            HasPayroll = true,
            HasAI = false,
            HasTally = false,
            Features = new List<string>
            {
                "100 invoices/month",
                "GST filing",
                "E-Invoice",
                "Inventory management",
                "5 users",
                "WhatsApp notifications",
                "Customer portal",
                "Multi-currency"
            }
        },

        new SubscriptionPlanDto
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Name = "Growth",
            MonthlyPrice = 2499,
            YearlyPrice = 24990,
            MaxInvoices = 500,
            MaxEmployees = 15,
            HasGST = true,
            HasPayroll = true,
            HasAI = true,
            HasTally = true,
            Features = new List<string>
            {
                "500 invoices/month",
                "All Starter features",
                "Payroll & HR",
                "Advanced analytics",
                "15 users",
                "Purchase orders",
                "Vendor management",
                "Bank reconciliation",
                "Loan eligibility checker"
            }
        },

        new SubscriptionPlanDto
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            Name = "Professional",
            MonthlyPrice = 4999,
            YearlyPrice = 49990,
            MaxInvoices = int.MaxValue,
            MaxEmployees = 50,
            HasGST = true,
            HasPayroll = true,
            HasAI = true,
            HasTally = true,
            Features = new List<string>
            {
                "Unlimited invoices",
                "All Growth features",
                "AI-powered insights",
                "Multi-warehouse",
                "API access",
                "50 users",
                "Custom reports",
                "Cash flow AI forecast",
                "Tax optimization AI",
                "Fraud detection"
            }
        },

        new SubscriptionPlanDto
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
            Name = "Enterprise",
            MonthlyPrice = 0,
            YearlyPrice = 0,
            MaxInvoices = int.MaxValue,
            MaxEmployees = int.MaxValue,
            HasGST = true,
            HasPayroll = true,
            HasAI = true,
            HasTally = true,
            Features = new List<string>
            {
                "Everything in Professional",
                "White-label option",
                "Unlimited users",
                "Custom integrations",
                "SLA guarantee",
                "On-premise option",
                "Custom AI models",
                "Dedicated infrastructure"
            }
        }
    };

        return await Task.FromResult<IEnumerable<SubscriptionPlanDto>>(plans);
    }

    public async Task<Result<SubscriptionStatusDto>> GetStatusAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null) return Result<SubscriptionStatusDto>.Failure("Tenant not found");

        var usersCount = await _db.TenantUsers.CountAsync(u => u.TenantId == tenantId && u.IsActive, ct);
        var invoicesThisMonth = await _db.Invoices
            .CountAsync(i => i.TenantId == tenantId
                && i.InvoiceDate.Month == DateTime.UtcNow.Month
                && i.InvoiceDate.Year == DateTime.UtcNow.Year, ct);

        var daysRemaining = (int)(tenant.SubscriptionExpiresAt - DateTime.UtcNow).TotalDays;
        var planLimits = GetPlanLimits(tenant.SubscriptionPlan);

        // Map TenantStatus → SubscriptionStatus
        var subStatus = tenant.Status switch
        {
            TenantStatus.Trial => SubscriptionStatus.Trial,
            TenantStatus.Active => SubscriptionStatus.Active,
            TenantStatus.Suspended => SubscriptionStatus.Suspended,
            TenantStatus.Cancelled => SubscriptionStatus.Cancelled,
            _ => SubscriptionStatus.Expired
        };

        return Result<SubscriptionStatusDto>.Success(new SubscriptionStatusDto
        {
            SubscriptionPlan = tenant.SubscriptionPlan,
            Status = subStatus,
            StartDate = tenant.CreatedAt,
            EndDate = tenant.SubscriptionExpiresAt,
            DaysRemaining = Math.Max(0, daysRemaining),
            IsActive = tenant.Status == TenantStatus.Active,
            IsExpiringSoon = daysRemaining <= 7,
            MonthlyPrice = planLimits.MonthlyPrice,
            IsTrial = tenant.Status == TenantStatus.Trial,
            CurrentUsers = usersCount,
            MaxUsers = tenant.MaxUsers,
            InvoicesThisMonth = invoicesThisMonth,
            MaxInvoices = planLimits.MaxInvoices
        });
    }

    public async Task<Result<SubscriptionStatusDto>> SubscribeAsync(Guid tenantId, CreateSubscriptionDto dto, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null) return Result<SubscriptionStatusDto>.Failure("Tenant not found");

        var plans = await GetPlansAsync(ct);
        var plan = plans.FirstOrDefault(p => p.Id == dto.PlanId);
        if (plan == null) return Result<SubscriptionStatusDto>.Failure("Invalid plan");

        if (!string.IsNullOrEmpty(dto.RazorpayPaymentId))
            _logger.LogInformation("Processing subscription for tenant {TenantId}, plan {Plan}", tenantId, dto.PlanId);

        var planEnum = Enum.Parse<SubscriptionPlanType>(plan.Name.Replace(" ", ""), true);
        tenant.SubscriptionPlan = planEnum;
        tenant.Status = TenantStatus.Active;
        tenant.SubscriptionExpiresAt = Convert.ToBoolean(dto.IsAnnual)
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        tenant.MaxUsers = Convert.ToInt32(plan.MaxUsers);

        await _db.SaveChangesAsync(ct);
        await _notification.SendEmailAsync(tenant.PrimaryContactEmail,
            $"Welcome to {plan.Name} Plan! 🎉",
            $"<h2>Your subscription is active!</h2><p>Enjoy all {plan.Name} features until {tenant.SubscriptionExpiresAt:dd MMM yyyy}.</p>");

        return await GetStatusAsync(tenantId, ct);
    }

    public async Task<Result<bool>> CancelAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null) return Result<bool>.Failure("Tenant not found");

        tenant.Status = TenantStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task HandleRazorpayWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        _logger.LogInformation("Razorpay webhook received: {Event}", payload.Substring(0, Math.Min(100, payload.Length)));
        var evt = JsonSerializer.Deserialize<JsonElement>(payload);
        var eventType = evt.GetProperty("event").GetString();

        switch (eventType)
        {
            case "subscription.activated":
            case "subscription.charged":
                break;
            case "subscription.cancelled":
            case "subscription.expired":
                break;
        }
    }

    public async Task<Result<bool>> CheckFeatureAccessAsync(Guid tenantId, string feature, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null) return Result<bool>.Failure("Tenant not found");

        var hasAccess = feature switch
        {
            "AI" => tenant.SubscriptionPlan >= SubscriptionPlanType.Professional,
            "Payroll" => tenant.SubscriptionPlan >= SubscriptionPlanType.Growth,
            "MultiWarehouse" => tenant.SubscriptionPlan >= SubscriptionPlanType.Professional,
            "APIAccess" => tenant.SubscriptionPlan >= SubscriptionPlanType.Professional,
            _ => true
        };

        return Result<bool>.Success(hasAccess);
    }

    public async Task<bool> IsWithinLimitsAsync(Guid tenantId, string limitType, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null) return false;

        var limits = GetPlanLimits(tenant.SubscriptionPlan);

        return limitType switch
        {
            "users" => await _db.TenantUsers.CountAsync(u => u.TenantId == tenantId && u.IsActive, ct) < limits.MaxUsers,
            "invoices" => await _db.Invoices.CountAsync(i => i.TenantId == tenantId
                && i.InvoiceDate.Month == DateTime.UtcNow.Month
                && i.InvoiceDate.Year == DateTime.UtcNow.Year, ct) < limits.MaxInvoices,
            _ => true
        };
    }

    private static (decimal MonthlyPrice, int MaxUsers, int MaxInvoices) GetPlanLimits(SubscriptionPlanType plan) =>
        plan switch
        {
            SubscriptionPlanType.Free => (0, 2, 20),
            SubscriptionPlanType.Starter => (999, 5, 100),
            SubscriptionPlanType.Growth => (2499, 15, 500),
            SubscriptionPlanType.Professional => (4999, 50, int.MaxValue),
            SubscriptionPlanType.Enterprise => (0, int.MaxValue, int.MaxValue),
            _ => (0, 2, 20)
        };
}

// ─── Analytics Service ─────────────────────────────────────────────────────
public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IAIService _aiService;

    public AnalyticsService(AppDbContext db, ICacheService cache, IAIService aiService)
    {
        _db = db; _cache = cache; _aiService = aiService;
    }

    public async Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(Guid tenantId, string period, CancellationToken ct = default)
    {
        var cacheKey = $"dashboard:{tenantId}:{period}";
        var cached = await _cache.GetAsync<DashboardSummaryDto>(cacheKey);
        if (cached != null) return Result<DashboardSummaryDto>.Success(cached);

        var (from, to) = ParsePeriod(period);
        var now = DateTime.UtcNow;

        var invoices = await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync(ct);

        var previousFrom = from.AddMonths(-1);
        var previousTo = from.AddDays(-1);
        var previousInvoices = await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceDate >= previousFrom && i.InvoiceDate <= previousTo)
            .ToListAsync(ct);

        var totalRevenue = invoices.Sum(i => i.PaidAmount);
        var outstanding = invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .Sum(i => i.TotalAmount - i.PaidAmount);
        var overdueCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue);

        var activeCustomers = await _db.Customers.CountAsync(c => c.TenantId == tenantId && c.IsActive, ct);
        var lowStock = await _db.Products.CountAsync(p => p.TenantId == tenantId && p.TrackInventory
            && p.CurrentStock <= p.MinStockLevel && p.IsActive, ct);
        var cashBalance = await _db.BankAccounts
            .Where(b => b.TenantId == tenantId && b.IsActive).SumAsync(b => b.CurrentBalance, ct);

        // GSTPayable: sum CGST+SGST+IGST from GSTTransactions (TotalTax doesn't exist)
        var gstPayable = await _db.GSTTransactions
            .Where(g => g.TenantId == tenantId && g.InvoiceDate >= from)
            .SumAsync(g => g.CGST + g.SGST + g.IGST, ct);

        // Monthly revenue chart
        var monthlyRevenue = new List<MonthlyRevenueDto>();
        for (int i = 5; i >= 0; i--)
        {
            var mDate = now.AddMonths(-i);
            var mRevenue = await _db.Invoices
                .Where(inv => inv.TenantId == tenantId
                    && inv.InvoiceDate.Year == mDate.Year && inv.InvoiceDate.Month == mDate.Month)
                .SumAsync(inv => inv.PaidAmount, ct);
            var mExpenses = await _db.Expenses
                .Where(e => e.TenantId == tenantId
                    && e.ExpenseDate.Year == mDate.Year && e.ExpenseDate.Month == mDate.Month)
                .SumAsync(e => e.Amount, ct);
            monthlyRevenue.Add(new MonthlyRevenueDto
            {
                Month = mDate.ToString("MMM yyyy"),
                Revenue = mRevenue,
                Expenses = mExpenses
            });
        }

        // Top customers
        var topCustomers = await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceDate >= from)
            .GroupBy(i => i.CustomerId)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(i => i.PaidAmount), Count = g.Count() })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .Join(_db.Customers, g => g.CustomerId, c => c.Id,
                (g, c) => new TopCustomerDto
                {
                    CustomerName = c.Name,
                    TotalAmount = g.Revenue
                })
            .ToListAsync(ct);

        // AI insights
        var insightsList = await _aiService.GetInsightsAsync(tenantId, null, 5);

        var summary = new DashboardSummaryDto
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = totalRevenue,
            OutstandingAmount = outstanding,
            OverdueAmount = overdueCount,
            TotalInvoices = invoices.Count,
            TotalCustomers = activeCustomers,
            LowStockProducts = lowStock,
            BankBalance = cashBalance,
            GSTPayable = gstPayable,
            MonthlyRevenueChart = monthlyRevenue,
            AIInsights = insightsList
        .Select(i => new AIInsightDto(
            i.Id,
            i.InsightType.ToString(),
            i.Title,
            i.Summary,
            i.ActionRecommended, // include the nullable property
            i.ConfidenceScore,
            i.CreatedAt
        )).ToList()
        };

        await _cache.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(5));
        return Result<DashboardSummaryDto>.Success(summary);
    }

    public async Task<Result<RevenueAnalyticsDto>> GetRevenueAnalyticsAsync(
        Guid tenantId, DateTime from, DateTime to, string groupBy, CancellationToken ct = default)
    {
        var invoices = await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceDate >= from && i.InvoiceDate <= to
                && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(ct);

        var total = invoices.Sum(i => i.TotalAmount);

        var byMonth = invoices
            .GroupBy(i => i.InvoiceDate.ToString("MMM yyyy"))
            .Select(g => new MonthlyRevenueDto
            {
                Month = g.Key,
                Revenue = g.Sum(i => i.TotalAmount)
            }).ToList();

        return Result<RevenueAnalyticsDto>.Success(new RevenueAnalyticsDto
        {
            TotalRevenue = total,
            MonthlyBreakdown = byMonth
        });
    }

    public async Task<Result<CashFlowForecastDto>> GetCashFlowForecastAsync(Guid tenantId, int forecastDays, CancellationToken ct = default)
    {
        var cashBalance = await _db.BankAccounts
            .Where(b => b.TenantId == tenantId && b.IsActive)
            .SumAsync(b => b.CurrentBalance, ct);

        var inflows = await _db.Invoices
            .Where(i => i.TenantId == tenantId
                && i.Status != InvoiceStatus.Paid
                && i.Status != InvoiceStatus.Cancelled
                && i.DueDate <= DateTime.UtcNow.AddDays(forecastDays))
            .SumAsync(i => i.TotalAmount - i.PaidAmount, ct);

        var weekly = new List<WeeklyCashFlowDto>();
        var weeks = Math.Max(1, forecastDays / 7);
        for (int w = 0; w < weeks; w++)
        {
            var weekInflow = inflows / weeks;
            var weekOutflow = weekInflow * 0.7m;
            weekly.Add(new WeeklyCashFlowDto
            {
                WeekStart = DateTime.UtcNow.AddDays(w * 7),
                Inflow = weekInflow,
                Outflow = weekOutflow,
                Net = weekInflow - weekOutflow
            });
        }

        return Result<CashFlowForecastDto>.Success(new CashFlowForecastDto
        {
            CurrentBalance = cashBalance,
            ProjectedInflow = inflows,
            ProjectedOutflow = inflows * 0.7m,
            NetCashFlow = cashBalance + inflows * 0.3m,
            WeeklyForecast = weekly
        });
    }

    private static (DateTime From, DateTime To) ParsePeriod(string period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            "today" => (now.Date, now),
            "thisWeek" => (now.AddDays(-(int)now.DayOfWeek), now),
            "thisMonth" => (new DateTime(now.Year, now.Month, 1), now),
            "thisQuarter" => (new DateTime(now.Year, (now.Month - 1) / 3 * 3 + 1, 1), now),
            "thisYear" => (new DateTime(now.Year, 4, 1), now),
            _ => (new DateTime(now.Year, now.Month, 1), now)
        };
    }

    private static IEnumerable<UpcomingComplianceDto> GetUpcomingCompliance()
    {
        var now = DateTime.UtcNow;
        var month = now.Month;
        var year = now.Year;
        return new List<UpcomingComplianceDto>
        {
            new() { Title = "GSTR-1",     DueDate = new DateTime(year, month, 11), Type = "GST",    IsOverdue = now.Day > 11 },
            new() { Title = "GSTR-3B",    DueDate = new DateTime(year, month, 20), Type = "GST",    IsOverdue = now.Day > 20 },
            new() { Title = "TDS Payment",DueDate = new DateTime(year, month, 7),  Type = "TDS",    IsOverdue = now.Day >  7 },
            new() { Title = "EPF/ESI",    DueDate = new DateTime(year, month, 15), Type = "Labour", IsOverdue = now.Day > 15 },
        };
    }
}