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

public class LedgerEntry : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid AccountId { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string Narration { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ChartOfAccount Account { get; set; } = null!;
}