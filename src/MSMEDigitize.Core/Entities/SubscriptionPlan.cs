using MSMEDigitize.Core.Common;
namespace MSMEDigitize.Core.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;            // Starter, Growth, Pro, Enterprise
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsMostPopular { get; set; } = false;
    public int MaxUsers { get; set; }
    public int MaxInvoices { get; set; }          // Per month
    public int MaxProducts { get; set; }
    public int MaxCustomers { get; set; }
    public bool HasGSTModule { get; set; }
    public bool HasPayrollModule { get; set; }
    public bool HasInventoryModule { get; set; }
    public bool HasAccountingModule { get; set; }
    public bool HasCRMModule { get; set; }
    public bool HasReportsModule { get; set; }
    public bool HasAPIAccess { get; set; }
    public bool HasWhatsAppAlerts { get; set; }
    public bool HasAdvancedAnalytics { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasCustomDomain { get; set; }
    public bool HasWhiteLabel { get; set; }
    public int StorageGB { get; set; }
    public int SortOrder { get; set; }
    public string? FeaturesJson { get; set; }   // Additional features JSON
    public string? RazorpayPlanId { get; set; }  // Razorpay plan ID

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}