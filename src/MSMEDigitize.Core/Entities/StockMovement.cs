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

public class StockMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceType { get; set; }  // Invoice, PO, Adjustment
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }

    public Product Product { get; set; } = null!;
}