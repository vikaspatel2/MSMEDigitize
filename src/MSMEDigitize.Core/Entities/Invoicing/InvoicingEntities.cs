using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Core.Entities.Invoicing;

public class Customer : TenantEntity
{
    public DateTime? LastInvoiceDate { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal TotalRevenue { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public Address BillingAddress { get; set; } = new();
    public Address? ShippingAddress { get; set; }
    public string? ContactPerson { get; set; }
    public int CreditDays { get; set; } = 0;
    public decimal CreditLimit { get; set; } = 0;
    public decimal CurrentOutstanding { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string CustomerType { get; set; } = "B2B"; // B2B, B2C, Export, SEZ
    public string? TDSApplicable { get; set; }
    public string? Notes { get; set; }
    public decimal TotalPurchase { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public decimal LoyaltyPoints { get; set; }
    public string? Tags { get; set; }
    public string? Mobile { get; set; }  // alias for Phone
    public string? Industry { get; set; }
    //public object? Invoices { get; set; }
    public ICollection<Invoice>? Invoices { get; set; } // Or whatever the correct type is

}

public class Invoice : TenantEntity
{
    public bool IsInterState { get; set; }
    // Type is now settable (backed by InvoiceType field); both names work
    public InvoiceType InvoiceType { get; set; }
    public InvoiceType Type { get => InvoiceType; set => InvoiceType = value; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    // Tenant navigation (lazy-resolved from TenantId via EF)
    public MSMEDigitize.Core.Entities.Tenants.Tenant? Tenant { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? PlaceOfSupply { get; set; }
    public bool IsIGST { get; set; }
    public bool IsReverseCharge { get; set; }
    public string? SupplyType { get; set; }
    public string? PortCode { get; set; }
    public string? ShippingBillNumber { get; set; }
    public DateTime? ShippingBillDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal CessAmount { get; set; }
    public decimal TDSAmount { get; set; }
    public decimal TCSAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal? ExchangeRate { get; set; }
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? BankDetails { get; set; }
    public string? Signature { get; set; }
    public string? QRCode { get; set; }
    public EInvoiceStatus EInvoiceStatus { get; set; } = EInvoiceStatus.NotRequired;
    public string? IRN { get; set; }
    public string? AckNumber { get; set; }
    public DateTime? AckDate { get; set; }
    public string? SignedQRCode { get; set; }
    public EWayBillStatus EWayBillStatus { get; set; } = EWayBillStatus.NotRequired;
    public string? EWayBillNumber { get; set; }
    public DateTime? EWayBillExpiryDate { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? PdfUrl { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? RecurringTemplateId { get; set; }
    public Guid? RecurringConfigId { get; set; } // alias
    // Sent tracking
    public bool IsSentToCustomer { get; set; }
    public DateTime? SentAt { get; set; }
    public string? PoNumber { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? EInvoiceIRN { get; set; }  // alias for IRN
    public bool GenerateEInvoice { get; set; }
    public bool GenerateEWayBill { get; set; }
    public decimal? ShippingCharge { get; set; }
    // Denormalised customer info (cached at invoice creation time)
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerGST { get; set; }
    public string? AmountInWords { get; set; }
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<InvoiceActivity> Activities { get; set; } = new List<InvoiceActivity>();
}

public class InvoiceLineItem : TenantEntity
{
    public int SortOrder { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ProductId { get; set; }
    public int SlNo { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ProductName => ItemName;
    public string? Description { get; set; }
    public string? HSNSACCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Nos";
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GSTRate { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal CessRate { get; set; }
    public decimal CessAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsFreeOfCost { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? SerialNumber { get; set; }
}

public class Payment : TenantEntity
{
    public Guid? InvoiceId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string? PaymentNumber { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMode Mode { get; set; }
    public MSMEDigitize.Core.Enums.PaymentMethod Method { get; set; }
    public MSMEDigitize.Core.Enums.PaymentStatus Status { get; set; } = MSMEDigitize.Core.Enums.PaymentStatus.Captured;
    public DateTime PaymentDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? TransactionId { get; set; }
    public string? TransactionReference { get; set; }  // alias for TransactionId
    public string? PaymentMode => Mode.ToString();  // string alias for Mode enum
    public string? ChequeNumber { get; set; }
    public string? BankName { get; set; }
    public string? Notes { get; set; }
    public bool IsReconciled { get; set; }
    public ReconciliationStatus ReconciliationStatus { get; set; } = ReconciliationStatus.Unreconciled;
    public Guid? BankTransactionId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpaySignature { get; set; }
    public string? PaymentLinkUrl { get; set; }
    public bool IsRefunded { get; set; }
    public decimal? RefundedAmount { get; set; }
}

public class RecurringInvoiceTemplate : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string Frequency { get; set; } = "Monthly"; // Daily, Weekly, Monthly, Quarterly, Yearly
    public int? DayOfMonth { get; set; }
    public DateTime NextRunDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoSend { get; set; } = false;
    public string? TemplateData { get; set; } // JSON of invoice template
}

public class InvoiceActivity : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }  // alias for Note
    public string? Note { get; set; }
    public string? PerformedBy { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class Vendor : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public string? BankAccount { get; set; }
    public string? IFSC { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
    public decimal CurrentOutstanding { get; set; }
    public bool IsActive { get; set; } = true;
    public string? VendorCategory { get; set; }
    public decimal TDSRate { get; set; }
    public string? MSMERegistrationNumber { get; set; }
    public bool IsMSMEVendor { get; set; }
}

public class PurchaseOrder : TenantEntity
{
    public string PONumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public DateTime PODate { get; set; }
    public DateTime OrderDate => PODate;
    public string? DeliveryAddress { get; set; }
    public string? TermsAndConditions { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Sent, Confirmed, PartiallyReceived, Received, Cancelled
    public decimal TotalAmount { get; set; }
    public decimal GSTAmount { get; set; }
    public string? Notes { get; set; }
    public ICollection<POLineItem> LineItems { get; set; } = new List<POLineItem>();
}

public class POLineItem : TenantEntity
{
    public Guid POId { get; set; }
    public Guid? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HSNCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string Unit { get; set; } = "Nos";
    public decimal UnitPrice { get; set; }
    public decimal GSTRate { get; set; }
    public decimal TotalAmount { get; set; }
}