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

public class EWayBill : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public string EWayBillNumber { get; set; } = string.Empty;
    public string TransporterName { get; set; } = string.Empty;
    public string? TransporterId { get; set; }
    public string? VehicleNumber { get; set; }
    public string TransportMode { get; set; } = "1";  // Road
    public string? DocumentNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUpto { get; set; }
    public decimal Distance { get; set; }
    public EWayBillStatus Status { get; set; }

    public Invoice Invoice { get; set; } = null!;
}