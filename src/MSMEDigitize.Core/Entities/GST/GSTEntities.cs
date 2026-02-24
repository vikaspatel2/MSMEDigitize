using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Core.Entities.GST;

public class GSTProfile : TenantEntity
{
    public string GSTIN { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public bool IsCompositionDealer { get; set; }
    public bool IsECommerceOperator { get; set; }
    public string? GSTPortalUsername { get; set; }
    public string? GSTPortalPasswordEncrypted { get; set; } // AES-256 encrypted
    public bool AutoFilingEnabled { get; set; } = false;
    public DateTime? LastSyncAt { get; set; }
    public bool IsValid { get; set; } = true;
    public DateTime RegistrationDate { get; set; }
    public string? CancellationDate { get; set; }
}

public class GSTReturn : TenantEntity
{
    public Guid GSTProfileId { get; set; }
    public GSTReturnType ReturnType { get; set; }
    public int Year { get; set; }
    public int Month { get; set; } // 0 for annual
    public int Quarter { get; set; } // for quarterly
    public GSTReturnStatus Status { get; set; } = GSTReturnStatus.Draft;
    public decimal TotalTaxableValue { get; set; }
    public decimal TotalCGST { get; set; }
    public decimal TotalSGST { get; set; }
    public decimal TotalIGST { get; set; }
    public decimal TotalCess { get; set; }
    public decimal TotalTaxLiability { get; set; }
    public decimal ITCAvailable { get; set; }
    public decimal ITCUtilized { get; set; }
    public decimal NetTaxPayable { get; set; }
    public decimal? LateFee { get; set; }
    public decimal? Interest { get; set; }
    public string? AcknowledgementNumber { get; set; }
    public DateTime? FiledAt { get; set; }
    public DateTime DueDate { get; set; }
    public string? JsonData { get; set; } // full return JSON
    public string? ErrorDetails { get; set; }
    public bool IsNilReturn { get; set; }
    public ICollection<GSTTransaction> Transactions { get; set; } = new List<GSTTransaction>();
    public GSTProfile? GSTProfile { get; set; }
}

public class GSTTransaction : TenantEntity
{
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public Guid ReturnId { get; set; }
    public GSTTransactionType TransactionType { get; set; }
    public string? CounterpartyGSTIN { get; set; }
    public string CounterpartyName { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string? PlaceOfSupply { get; set; }
    public bool IsReverseCharge { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal IGST { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal Cess { get; set; }
    public string? HSNCode { get; set; }
    public SupplyType SupplyType { get; set; }
    public bool IsAmended { get; set; }
    public string? OriginalInvoiceNumber { get; set; }
}

public class HSNMaster : BaseEntity
{
    public string HSNCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal GSTRate { get; set; }
    public decimal? CGSTRate { get; set; }
    public decimal? SGSTRate { get; set; }
    public decimal? IGSTRate { get; set; }
    public decimal? CessRate { get; set; }
    public bool IsActive { get; set; } = true;
    public string Category { get; set; } = string.Empty;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class ITCRegister : TenantEntity
{
    public string SupplierGSTIN { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal TotalITC { get; set; }
    public bool IsAvailableInGSTR2B { get; set; }
    public bool IsReversalRequired { get; set; }
    public decimal? ReversalAmount { get; set; }
    public string? ReversalReason { get; set; }
    public bool IsUtilized { get; set; }
    public DateTime? UtilizedOn { get; set; }
    public string? LinkedReturnId { get; set; }
}

public class GSTNotice : TenantEntity
{
    public string NoticeNumber { get; set; } = string.Empty;
    public string NoticeType { get; set; } = string.Empty; // DRC-01, ADJ-01, etc.
    public DateTime IssuedDate { get; set; }
    public DateTime ResponseDueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DemandAmount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Responded, Resolved, Adjudication
    public string? ResponseFiled { get; set; }
    public DateTime? ResponseFiledAt { get; set; }
    public string? AttachmentUrl { get; set; }
}
