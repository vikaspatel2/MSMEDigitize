using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Core.Entities.Subscriptions;

public class SubscriptionPlanDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public MSMEDigitize.Core.Enums.SubscriptionPlanTier Plan { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public decimal? AnnualDiscountPercent { get; set; }
    public int MaxUsers { get; set; }
    public int MaxInvoicesPerMonth { get; set; } // -1 = unlimited
    public int MaxInventoryItems { get; set; }
    public bool HasGSTFiling { get; set; }
    public bool HasPayroll { get; set; }
    public bool HasBankReconciliation { get; set; }
    public bool HasEInvoicing { get; set; }
    public bool HasAIInsights { get; set; }
    public bool HasMultiWarehouse { get; set; }
    public bool HasAPIAccess { get; set; }
    public bool HasWhiteLabelOption { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasDedicatedAccountManager { get; set; }
    public string? RazorpayPlanId { get; set; }
    public bool IsActive { get; set; } = true;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<string> Features { get; set; } = new();
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<string> Limitations { get; set; } = new();
}

public class TenantSubscription : TenantEntity
{
    public MSMEDigitize.Core.Enums.SubscriptionPlanTier Plan { get; set; }
    public Guid PlanDefinitionId { get; set; }
    public Guid PlanId { get => PlanDefinitionId; set => PlanDefinitionId = value; }
    public MSMEDigitize.Core.Enums.BillingCycle BillingCycle { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAnnual { get; set; }
    public decimal Amount { get; set; }
    public decimal GST { get; set; }
    public decimal TotalAmount { get; set; }
    public string? RazorpaySubscriptionId { get; set; }
    public string? RazorpayOrderId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public bool AutoRenew { get; set; } = true;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? TrialEndDate { get => TrialEndsAt; set => TrialEndsAt = value; }
    public bool IsTrialPeriod => Status == MSMEDigitize.Core.Enums.SubscriptionStatus.Trial;
    public ICollection<SubscriptionInvoice> Invoices { get; set; } = new List<SubscriptionInvoice>();
}

public class SubscriptionInvoice : TenantEntity
{
    public Guid SubscriptionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentId { get; set; }
    public bool IsPaid { get; set; }
    public string? InvoiceUrl { get; set; }
}

// SubscriptionStatus is defined in MSMEDigitize.Core.Enums

public class UsageTracking : TenantEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int InvoicesCreated { get; set; }
    public int EInvoicesGenerated { get; set; }
    public int GSTReturnsfiled { get; set; }
    public int APICallsMade { get; set; }
    public int StorageUsedMB { get; set; }
    public int UsersActive { get; set; }
    public int PayslipsGenerated { get; set; }
    public int WhatsAppMessagesSent { get; set; }
    public int SMSSent { get; set; }
    public int AIInsightsGenerated { get; set; }
}

//using MSMEDigitize.Core.Common;
//using MSMEDigitize.Core.Enums;

//namespace MSMEDigitize.Core.Entities.Subscriptions;

//public class SubscriptionPlanDefinition : BaseEntity
//{
//    public string Name { get; set; } = string.Empty;
//    public MSMEDigitize.Core.Enums.SubscriptionPlanTier Plan { get; set; }
//    public string Description { get; set; } = string.Empty;
//    public decimal MonthlyPrice { get; set; }
//    public decimal AnnualPrice { get; set; }
//    public decimal? AnnualDiscountPercent { get; set; }
//    public int MaxUsers { get; set; }
//    public int MaxInvoicesPerMonth { get; set; } // -1 = unlimited
//    public int MaxInventoryItems { get; set; }
//    public bool HasGSTFiling { get; set; }
//    public bool HasPayroll { get; set; }
//    public bool HasBankReconciliation { get; set; }
//    public bool HasEInvoicing { get; set; }
//    public bool HasAIInsights { get; set; }
//    public bool HasMultiWarehouse { get; set; }
//    public bool HasAPIAccess { get; set; }
//    public bool HasWhiteLabelOption { get; set; }
//    public bool HasPrioritySupport { get; set; }
//    public bool HasDedicatedAccountManager { get; set; }
//    public string? RazorpayPlanId { get; set; }
//    public bool IsActive { get; set; } = true;
//    public List<string> Features { get; set; } = new();
//    public List<string> Limitations { get; set; } = new();
//}

//public class TenantSubscription : TenantEntity
//{
//    public MSMEDigitize.Core.Enums.SubscriptionPlanTier Plan { get; set; }
//    public Guid PlanDefinitionId { get; set; }
//    public Guid PlanId { get => PlanDefinitionId; set => PlanDefinitionId = value; }
//    public MSMEDigitize.Core.Enums.BillingCycle BillingCycle { get; set; }
//    public decimal FinalAmount { get; set; }
//    public DateTime StartDate { get; set; }
//    public DateTime EndDate { get; set; }
//    public bool IsAnnual { get; set; }
//    public decimal Amount { get; set; }
//    public decimal GST { get; set; }
//    public decimal TotalAmount { get; set; }
//    public string? RazorpaySubscriptionId { get; set; }
//    public string? RazorpayOrderId { get; set; }
//    public SubscriptionStatus Status { get; set; }
//    public bool AutoRenew { get; set; } = true;
//    public DateTime? CancelledAt { get; set; }
//    public string? CancellationReason { get; set; }
//    public DateTime? TrialEndsAt { get; set; }
//    public DateTime? TrialEndDate { get => TrialEndsAt; set => TrialEndsAt = value; }
//    public bool IsTrialPeriod => Status == MSMEDigitize.Core.Enums.SubscriptionStatus.Trial;
//    public ICollection<SubscriptionInvoice> Invoices { get; set; } = new List<SubscriptionInvoice>();
//}

//public class SubscriptionInvoice : TenantEntity
//{
//    public Guid SubscriptionId { get; set; }
//    public string InvoiceNumber { get; set; } = string.Empty;
//    public decimal Amount { get; set; }
//    public decimal GSTAmount { get; set; }
//    public decimal Total { get; set; }
//    public DateTime DueDate { get; set; }
//    public DateTime? PaidAt { get; set; }
//    public string? PaymentId { get; set; }
//    public bool IsPaid { get; set; }
//    public string? InvoiceUrl { get; set; }
//}

//// SubscriptionStatus is defined in MSMEDigitize.Core.Enums

//public class UsageTracking : TenantEntity
//{
//    public int Year { get; set; }
//    public int Month { get; set; }
//    public int InvoicesCreated { get; set; }
//    public int EInvoicesGenerated { get; set; }
//    public int GSTReturnsfiled { get; set; }
//    public int APICallsMade { get; set; }
//    public int StorageUsedMB { get; set; }
//    public int UsersActive { get; set; }
//    public int PayslipsGenerated { get; set; }
//    public int WhatsAppMessagesSent { get; set; }
//    public int SMSSent { get; set; }
//    public int AIInsightsGenerated { get; set; }
//}