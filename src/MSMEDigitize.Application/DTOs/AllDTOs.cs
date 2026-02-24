using MSMEDigitize.Application.DTOs;
using MSMEDigitize.Core.Common;
namespace MSMEDigitize.Application.DTOs;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerGSTIN { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string EInvoiceStatus { get; set; } = string.Empty;
    public string? IRN { get; set; }
    public string? AckNumber { get; set; }
    public string? SignedQRCode { get; set; }
    public string? EWayBillNumber { get; set; }
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}

public class InvoiceListDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerGSTIN { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string EInvoiceStatus { get; set; } = string.Empty;
    public string? IRN { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue => IsOverdue ? (int)(DateTime.UtcNow - DueDate).TotalDays : 0;
}

public class InvoiceLineItemDto
{
    public int SlNo { get; set; }
    public Guid? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? HSNSACCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Nos";
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal GSTRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CreateInvoiceLineItemDto
{
    public Guid? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HSNCode { get; set; }
    public string? HSNSACCode { get => HSNCode; set => HSNCode = value; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Nos";
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal GSTRate { get; set; }
    public decimal CessRate { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Mode { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public bool IsReconciled { get; set; }
}

public class RevenueDto
{
    public decimal Revenue { get; set; }
    public decimal Collected { get; set; }
}

public class AIInsightSummaryDto
{
    public Guid Id { get; set; }
    public string InsightType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public decimal? PotentialSaving { get; set; }
    public string? ActionUrl { get; set; }
}
public class CustomerDto { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string? GSTIN { get; set; } public string Email { get; set; } = string.Empty; public string Phone { get; set; } = string.Empty; public string CustomerType { get; set; } = string.Empty; public decimal CurrentOutstanding { get; set; } public decimal TotalPurchase { get; set; } public DateTime? LastTransactionDate { get; set; } public bool IsActive { get; set; } }
public class ProductDto { public Guid Id { get; set; } public string SKU { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string HSNCode { get; set; } = string.Empty; public decimal GSTRate { get; set; } public decimal SellingPrice { get; set; } public decimal MRP { get; set; } public decimal CurrentStock { get; set; } public decimal MinStockLevel { get; set; } public bool IsLowStock { get; set; } public string Unit { get; set; } = string.Empty; public bool IsActive { get; set; } }
public class GSTSummaryDto { public int Month { get; set; } public int Year { get; set; } public decimal TotalSales { get; set; } public decimal TotalTax { get; set; } public decimal OutputGST { get; set; } public decimal InputGST { get; set; } public decimal NetGSTPayable { get; set; } public string GSTR1Status { get; set; } = string.Empty; public string GSTR3BStatus { get; set; } = string.Empty; public string? GSTIN { get; set; } public Guid? TenantId { get; set; } }
public class SubscriptionPlanDto { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public string Plan { get; set; } = string.Empty; public decimal MonthlyPrice { get; set; } public decimal AnnualPrice { get; set; } public decimal? AnnualDiscountPercent { get; set; } public int MaxUsers { get; set; } public int MaxInvoicesPerMonth { get; set; } public bool HasGSTFiling { get; set; } public bool HasPayroll { get; set; } public bool HasEInvoicing { get; set; } public bool HasAIInsights { get; set; } public List<string> Features { get; set; } = new(); public bool IsCurrentPlan { get; set; } }

// Address DTO
public record AddressDto(string Line1, string? Line2, string City, string State, string PinCode);

// Create/Update DTOs
public class CreateCustomerDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GSTIN { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public MSMEDigitize.Core.Enums.CustomerType CustomerType { get; set; }
    public string? Industry { get; set; }
    public AddressDto? BillingAddress { get; set; }
    public AddressDto? ShippingAddress { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCustomerDto : CreateCustomerDto { }

public class CreateProductDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HSNCode { get; set; } = string.Empty;
    public decimal GSTRate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public MSMEDigitize.Core.Enums.UnitOfMeasure Unit { get; set; }
    public decimal MinStockLevel { get; set; }
    public bool TrackInventory { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

//public class ProductDto
//{
//    public Guid Id { get; set; }
//    public string Code { get; set; } = string.Empty;
//    public string Name { get; set; } = string.Empty;
//    public string HSNCode { get; set; } = string.Empty;
//    public decimal GSTRate { get; set; }
//    public decimal SellingPrice { get; set; }
//    public decimal CurrentStock { get; set; }
//    public bool IsActive { get; set; } = true;
//}

public class StockAdjustmentDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public MSMEDigitize.Core.Enums.StockMovementType MovementType { get; set; }
}

public class RefreshTokenDto { public string Token { get; set; } = string.Empty; }
public class ForgotPasswordDto { public string Email { get; set; } = string.Empty; }
public class ResetPasswordDto { public string Token { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
public class TenantUserDto { public Guid Id { get; set; } public string UserId { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public MSMEDigitize.Core.Enums.TenantRole Role { get; set; } public Guid TenantId { get; set; } public string BusinessName { get; set; } = string.Empty; }

public class InvoiceDetailDto : InvoiceDto { }
public class CreateInvoiceDto { public Guid CustomerId { get; set; } public MSMEDigitize.Core.Enums.InvoiceType Type { get; set; } public DateTime InvoiceDate { get; set; } public DateTime DueDate { get; set; } public bool IsInterState { get; set; } public string? Notes { get; set; } public string? PoNumber { get; set; } public decimal ShippingCharge { get; set; } public string? TermsAndConditions { get; set; } public bool GenerateEInvoice { get; set; } public bool GenerateEWayBill { get; set; } public List<CreateInvoiceLineItemDto> LineItems { get; set; } = new(); }
public class UpdateInvoiceDto : CreateInvoiceDto { }
public class RecordPaymentDto { public Guid InvoiceId { get; set; } public decimal Amount { get; set; } public MSMEDigitize.Core.Enums.PaymentMode Mode { get; set; } public MSMEDigitize.Core.Enums.PaymentMode PaymentMode { get => Mode; set => Mode = value; } public DateTime PaymentDate { get; set; } public string? TransactionId { get; set; } public string? TransactionReference { get; set; } public string? BankName { get; set; } public string? Notes { get; set; } }

// GST Filing DTOs
public class GSTR1SummaryDto { public Guid? TenantId { get; set; } public string? GSTIN { get; set; } public int Month { get; set; } public int Year { get; set; } public List<object> B2BInvoices { get; set; } = new(); public decimal TotalTaxableValue { get; set; } public decimal TotalTax { get; set; } }
public class GSTR3BSummaryDto { public Guid? TenantId { get; set; } public int Month { get; set; } public int Year { get; set; } public decimal TotalOutwardTaxableSupplies { get; set; } public decimal TotalCGST { get; set; } public decimal TotalSGST { get; set; } public decimal TotalIGST { get; set; } public decimal NetTaxPayable { get; set; } }
public class ITCReconciliationDto { public Guid? TenantId { get; set; } public int Month { get; set; } public int Year { get; set; } public List<ITCLineDto> Lines { get; set; } = new(); }
public class ITCLineDto { public string? SupplierGSTIN { get; set; } public string? SupplierName { get; set; } public decimal Amount { get; set; } public bool IsReconciled { get; set; } }
public class B2BEntryDto { public string? GSTIN { get; set; } public string? Name { get; set; } public string? InvoiceNumber { get; set; } public decimal InvoiceValue { get; set; } public decimal TaxAmount { get; set; } }
public class InvoiceActivityDto { public Guid Id { get; set; } public string Action { get; set; } = string.Empty; public string? Description { get; set; } public string? PerformedBy { get; set; } public DateTime CreatedAt { get; set; } }
public class TaxRate { public string HSNCode { get; set; } = string.Empty; public decimal Rate { get; set; } public decimal CGSTRate { get; set; } public decimal SGSTRate { get; set; } public decimal IGSTRate { get; set; } }
public class AIInsightDto { public Guid Id { get; set; } public string Type { get; set; } = string.Empty; public string Title { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string? RiskLevel { get; set; } public decimal? PotentialSaving { get; set; } public string? ActionUrl { get; set; } public DateTime CreatedAt { get; set; } }