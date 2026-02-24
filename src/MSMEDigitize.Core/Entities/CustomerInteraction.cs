using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Core.Entities;

public class CustomerInteraction : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public InteractionType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime InteractionDate { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public bool IsFollowUpDone { get; set; } = false;
    public Guid? AssignedToUserId { get; set; }

    public Customer Customer { get; set; } = null!;
}