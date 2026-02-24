// Subscription.cs — Root-namespace Subscription entity
// Used by SubscriptionPlan (nav) and UnitOfWork.
// The actual subscription data lives in Subscriptions/TenantSubscription,
// but code referencing MSMEDigitize.Core.Entities.Subscription needs this.
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Core.Entities;

public class Subscription : TenantEntity
{
    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
    public MSMEDigitize.Core.Enums.SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAnnual { get; set; }
    public decimal Amount { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string? RazorpaySubscriptionId { get; set; }
    public string? RazorpayOrderId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public bool IsTrialPeriod { get; set; }  // true during free trial period
    public BillingCycle BillingCycle { get; set; }
    public decimal FinalAmount { get; set; }
}