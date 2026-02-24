using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;

namespace MSMEDigitize.Core.Entities.Tenants;

public class Tenant : BaseEntity
{
    public string BusinessName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // unique subdomain: abc.msme.app
    public string GSTIN { get; set; } = string.Empty;
    public string PAN { get; set; } = string.Empty;
    public string? UdyamRegistrationNumber { get; set; }
    public MsmeCategory MsmeCategory { get; set; }
    public BusinessType BusinessType { get; set; }
    public string Industry { get; set; } = string.Empty;
    public string? NIC_Code { get; set; }
    public Address RegisteredAddress { get; set; } = new();
    public Address? OperationalAddress { get; set; }
    public string PrimaryContactEmail { get; set; } = string.Empty;
    public string PrimaryContactPhone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Trial;
    public Guid? CurrentSubscriptionId { get; set; }
    public SubscriptionPlanType SubscriptionPlan { get; set; } = SubscriptionPlanType.Free;
    public string CurrentPlan => SubscriptionPlan.ToString();
    public DateTime SubscriptionExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);
    public int MaxUsers { get; set; } = 2;
    public string? RazorpayCustomerId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public string Currency { get; set; } = "INR";
    public string Language { get; set; } = "en-IN";
    public FinancialYearStart FinancialYearStart { get; set; } = FinancialYearStart.April;
    public bool IsKycVerified { get; set; } = false;
    public DateTime? KycVerifiedAt { get; set; }
    public bool IsGSTRegistered { get; set; } = true;
    public bool IsCompositionScheme { get; set; } = false;
    public string? BankAccountNumber { get; set; }
    public string? IFSC { get; set; }
    public string BankIFSC => IFSC ?? string.Empty;
    public string? BankName { get; set; }
    // Invoice settings
    public string InvoicePrefix { get; set; } = "INV";
    public int InvoiceStartNumber { get; set; } = 1;
    // Aliases used in InvoiceService HTML generation
    public string? AddressLine1 => RegisteredAddress?.Line1;
    public string? City => RegisteredAddress?.City;
    public string? State => RegisteredAddress?.State;
    public string? Pincode => RegisteredAddress?.PinCode;
    private string? _gstNumber;
    public string? GSTNumber { get => _gstNumber ?? GSTIN; set { _gstNumber = value; if (value != null) GSTIN = value; } }
    public string? Phone => PrimaryContactPhone;
    public string? Email => PrimaryContactEmail;

    // Navigation
    public ICollection<TenantUser> Users { get; set; } = new List<TenantUser>();
    public ICollection<TenantModule> EnabledModules { get; set; } = new List<TenantModule>();
    public ICollection<ApiIntegration> Integrations { get; set; } = new List<ApiIntegration>();
}

public class TenantUser : TenantEntity
{
    private bool? _isOwner;
    public bool IsOwner { get => _isOwner ?? (Role == MSMEDigitize.Core.Enums.TenantRole.Owner); set => _isOwner = value; }
    public Guid? UserId { get; set; }  // Link to ApplicationUser.Id
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public TenantRole Role { get; set; } = TenantRole.Staff;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfileImageUrl { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public ICollection<string> Permissions { get; set; } = new List<string>();
    // Auth fields
    public string PasswordHash { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    // Navigation
    public Tenant? Tenant { get; set; }
}

public class TenantModule : TenantEntity
{
    public string ModuleName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime EnabledAt { get; set; } = DateTime.UtcNow;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Dictionary<string, string> Settings { get; set; } = new();
}

public class ApiIntegration : TenantEntity
{
    public IntegrationType Type { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Dictionary<string, string> Config { get; set; } = new();
}

// Address is defined in MSMEDigitize.Core.Common.Address — use that one.
// The record below is removed to avoid the ambiguous reference error.