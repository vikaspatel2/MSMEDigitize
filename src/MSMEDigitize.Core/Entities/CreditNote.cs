using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;
namespace MSMEDigitize.Core.Entities;

public class CreditNote : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid CustomerId { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public DateTime CreditNoteDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsApplied { get; set; } = false;

    public Invoice Invoice { get; set; } = null!;
}