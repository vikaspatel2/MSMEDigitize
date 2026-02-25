using MSMEDigitize.Application.DTOs;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.DTOs;
using MSMEDigitize.Core.Entities;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MSMEDigitize.Web.ViewModels;

// Dashboard
public class DashboardViewModel
{
    public DashboardSummaryDto Summary { get; set; } = null!;
    public List<AIInsight> AIInsights { get; set; } = new();
    public List<SalesDataPoint> SalesTrend { get; set; } = new();
    public string CurrentMonth { get; set; } = "";
}

// Auth
public class RegisterRequest
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Phone { get; set; } = "";
    [Required] public string BusinessName { get; set; } = "";
    public string? GSTNumber { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public BusinessType BusinessType { get; set; }
    public BusinessCategory BusinessCategory { get; set; }
    public Guid PlanId { get; set; }
}

public class RegisterViewModel
{
    public RegisterRequest Request { get; set; } = new();
    public List<MSMEDigitize.Core.Entities.SubscriptionPlan> Plans { get; set; } = new();
    // Passthrough properties so controllers can access directly
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get => Request.Email; set => Request.Email = value; }
    [System.ComponentModel.DataAnnotations.Required]
    public string Password { get => Request.Password; set => Request.Password = value; }
    [System.ComponentModel.DataAnnotations.Required]
    public string FirstName { get => Request.FirstName; set => Request.FirstName = value; }
    [System.ComponentModel.DataAnnotations.Required]
    public string LastName { get => Request.LastName; set => Request.LastName = value; }
    [System.ComponentModel.DataAnnotations.Required]
    public string Phone { get => Request.Phone; set => Request.Phone = value; }
    [System.ComponentModel.DataAnnotations.Required]
    public string BusinessName { get => Request.BusinessName; set => Request.BusinessName = value; }
    public string? GSTNumber { get => Request.GSTNumber; set => Request.GSTNumber = value; }
    //public string? Industry { get => Request.Industry; set => Request.Industry = value; }
    public MSMEDigitize.Core.Enums.BusinessType BusinessType { get => Request.BusinessType; set => Request.BusinessType = value; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}

// Invoice
public class InvoiceFilter
{
    public InvoiceStatus? Status { get; set; }
    public InvoiceType? Type { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? CustomerId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class InvoiceListViewModel
{
    public List<Invoice> Invoices { get; set; } = new();
    public InvoiceFilter Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Filter.PageSize);
}

public class CreateInvoiceViewModel
{
    // Form fields (POST)
    public InvoiceType Type { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? CustomerId { get; set; }
    public string? PlaceOfSupply { get; set; }
    public string? PoNumber { get; set; }
    public bool IsInterState { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public List<InvoiceLineItemRequest> LineItems { get; set; } = new();
    // Dropdown data (GET)
    public List<CustomerDropdown> Customers { get; set; } = new();
    public List<ProductDropdown> Products { get; set; } = new();
    public List<TaxRate> TaxRates { get; set; } = new();
    public string TenantState { get; set; } = "";
}

public record CustomerDropdown(Guid Id, string Name, string GSTNumber, string State, string Email, string Phone, int PaymentTermsDays);
public record ProductDropdown(Guid Id, string Name, decimal Price, decimal GSTRate, string HSNCode, string Unit);

public class CreateInvoiceRequest
{
    public InvoiceType Type { get; set; }
    public Guid? CustomerId { get; set; }
    [Required] public string CustomerName { get; set; } = "";
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerGST { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerState { get; set; }
    public string? PlaceOfSupply { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? PONumber { get; set; }
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public bool IsReverseCharge { get; set; }
    public bool IsExport { get; set; }
    public bool SaveAsDraft { get; set; }
    public List<InvoiceLineItemRequest> LineItems { get; set; } = new();
}

public class InvoiceLineItemRequest
{
    public Guid? ProductId { get; set; }
    [Required] public string Description { get; set; } = "";
    public string? HSNSACCode { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "Nos";
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GSTRate { get; set; }
    public decimal CessRate { get; set; }
    public int SortOrder { get; set; }
}

public class RecordPaymentRequest
{
    [Required] public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
}

// Filters

public class AuditActionFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUser;
    private readonly Infrastructure.Data.AppDbContext _db;

    public AuditActionFilter(ICurrentUserService currentUser, Infrastructure.Data.AppDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
    {
        var result = await next();
        // Log audit trail for write operations
        if (_currentUser.IsAuthenticated && context.HttpContext.Request.Method != "GET")
        {
            var audit = new AuditLog
            {
                //TenantId = _currentUser.TenantId,
                //UserId = _currentUser.UserId,
                UserEmail = _currentUser.Email,
                Action = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                // IsSuccess removed - not in entity
            };
            _db.AuditLogs.Add(audit);
            await _db.SaveChangesAsync();
        }
    }
}

public class TenantAuthorizationFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
        => await next();
}

// Alias for backward compatibility
public class InvoiceViewModel : CreateInvoiceViewModel { }

// Auth ViewModels  
public class LoginViewModel
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

public class ForgotPasswordViewModel
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    public string Token { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(8)]
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}