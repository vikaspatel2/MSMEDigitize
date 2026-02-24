using MediatR;
using Microsoft.EntityFrameworkCore;
using MSMEDigitize.Application.Commands.Invoice;
using MSMEDigitize.Application.DTOs;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure.Data;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Application.Handlers.Invoice;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Result<InvoiceDetailDto>>
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGSTService _gstService;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cache;

    public CreateInvoiceCommandHandler(AppDbContext db, IUnitOfWork unitOfWork,
        IGSTService gstService, INotificationService notificationService, ICacheService cache)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _gstService = gstService;
        _notificationService = notificationService;
        _cache = cache;
    }

    public async Task<Result<InvoiceDetailDto>> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // 1. Validate customer
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.TenantId == request.TenantId && !c.IsDeleted, ct);
        if (customer == null)
            return Result<InvoiceDetailDto>.Failure("Customer not found");

        // 2. Get tenant for GST info
        var tenant = await _db.Tenants.FindAsync(new object[] { request.TenantId }, ct);
        if (tenant == null)
            return Result<InvoiceDetailDto>.Failure("Tenant not found");

        // 3. Generate invoice number
        var invoiceNumber = await GenerateInvoiceNumberAsync(request.TenantId, dto.Type, ct);

        // 4. Resolve products and calculate GST
        var lineItems = new List<InvoiceLineItem>();
        decimal subTotal = 0, totalCGST = 0, totalSGST = 0, totalIGST = 0, totalCess = 0;

        foreach (var lineDto in dto.LineItems)
        {
            // Fetch product if linked
            decimal gstRate = lineDto.GSTRate;
            //string? hsnCode = lineDto.HSNCode;
            string? hsnCode = lineDto.HSNSACCode;
            if (lineDto.ProductId.HasValue)
            {
                var product = await _db.Products.FindAsync(new object[] { lineDto.ProductId.Value }, ct);
                if (product != null)
                {
                    gstRate = product.GSTRate;
                    hsnCode ??= product.HSNCode;
                }
            }

            var qty = lineDto.Quantity;
            var price = lineDto.UnitPrice;
            //var discountPct = lineDto.DiscountPercent ?? 0;
            var discountPct = lineDto.DiscountPercent;
            var taxableAmt = qty * price * (1 - discountPct / 100);
            var discountAmt = qty * price * (discountPct / 100);

            decimal cgst = 0, sgst = 0, igst = 0, cess = 0;
            if (dto.IsInterState)
            {
                igst = taxableAmt * gstRate / 100;
            }
            else
            {
                cgst = taxableAmt * (gstRate / 2) / 100;
                sgst = taxableAmt * (gstRate / 2) / 100;
            }
            // Cess for specific goods (luxury, tobacco, etc.) — determined by HSN
            cess = await _gstService.CalculateCessAsync(hsnCode ?? "", taxableAmt);

            var lineTotal = taxableAmt + cgst + sgst + igst + cess;

            lineItems.Add(new InvoiceLineItem
            {
                ProductId = lineDto.ProductId,
                Description = lineDto.Description,
                //HSNCode = hsnCode,
                Quantity = qty,
                Unit = lineDto.Unit,
                UnitPrice = price,
                DiscountPercent = discountPct,
                DiscountAmount = discountAmt,
                TaxableAmount = taxableAmt,
                GSTRate = gstRate,
                CGSTRate = dto.IsInterState ? 0 : gstRate / 2,
                CGSTAmount = cgst,
                SGSTRate = dto.IsInterState ? 0 : gstRate / 2,
                SGSTAmount = sgst,
                IGSTRate = dto.IsInterState ? gstRate : 0,
                IGSTAmount = igst,
                CessRate = 0,
                CessAmount = cess,
                TotalAmount = lineTotal
            });

            subTotal += taxableAmt + discountAmt;
            totalCGST += cgst;
            totalSGST += sgst;
            totalIGST += igst;
            totalCess += cess;
        }

        var discountTotal = lineItems.Sum(l => l.DiscountAmount);
        var taxableTotal = lineItems.Sum(l => l.TaxableAmount);
        var grandTotal = taxableTotal + totalCGST + totalSGST + totalIGST + totalCess;

        // Add shipping if applicable
        //if (dto.ShippingCharge.HasValue && dto.ShippingCharge > 0)
        //    grandTotal += dto.ShippingCharge.Value;

        if (dto.ShippingCharge > 0)
            grandTotal += dto.ShippingCharge;

        // 5. Create invoice entity
        var invoice = new Core.Entities.Invoicing.Invoice
        {
            TenantId = request.TenantId,
            InvoiceNumber = invoiceNumber,
            Type = dto.Type,
            CustomerId = dto.CustomerId,
            InvoiceDate = dto.InvoiceDate,
            DueDate = dto.DueDate,
            LineItems = lineItems,
            SubTotal = subTotal,
            DiscountAmount = discountTotal,
            TaxableAmount = taxableTotal,
            CGSTAmount = totalCGST,
            SGSTAmount = totalSGST,
            IGSTAmount = totalIGST,
            CessAmount = totalCess,
            TotalAmount = grandTotal,
            PaidAmount = 0,
            Status = InvoiceStatus.Draft,
            Notes = dto.Notes,
            TermsAndConditions = dto.TermsAndConditions,
            PoNumber = dto.PoNumber,
            IsInterState = dto.IsInterState,
            EInvoiceStatus = dto.GenerateEInvoice ? EInvoiceStatus.Pending : EInvoiceStatus.NotRequired,
            EWayBillStatus = dto.GenerateEWayBill ? EWayBillStatus.Pending : EWayBillStatus.NotRequired,
            CreatedBy = request.UserId.ToString()
        };

        // Add activity log
        invoice.Activities.Add(new InvoiceActivity
        {
            Action = "Created",
            Description = $"Invoice {invoiceNumber} created",
            PerformedBy = request.UserId.ToString()
        });

        _db.Invoices.Add(invoice);

        // 6. Update product stock if needed
        foreach (var lineDto in dto.LineItems.Where(l => l.ProductId.HasValue))
        {
            var product = await _db.Products.FindAsync(new object[] { lineDto.ProductId!.Value }, ct);
            if (product != null && product.TrackInventory && dto.Type == InvoiceType.TaxInvoice)
            {
                product.CurrentStock -= (int)lineDto.Quantity;
                _db.StockLedger.Add(new StockLedger
                {
                    TenantId = request.TenantId,
                    ProductId = product.Id,
                    Quantity = -(int)lineDto.Quantity,
                    Reason = StockAdjustmentReason.Sale
                   // Reference = invoiceNumber
                });
            }
        }

        // 7. Record GST transaction
        _db.GSTTransactions.Add(new GSTTransaction
        {
            TenantId = request.TenantId,
           // InvoiceId = invoice.Id,
            TransactionType = dto.IsInterState ? GSTTransactionType.B2B : GSTTransactionType.B2B,
            CounterpartyGSTIN = customer.GSTIN,
            CounterpartyName = customer.Name,
            //TaxableAmount = taxableTotal,
            //CGSTAmount = totalCGST,
            //SGSTAmount = totalSGST,
            //IGSTAmount = totalIGST,
            //CessAmount = totalCess,
            //TotalTax = totalCGST + totalSGST + totalIGST + totalCess,
            TransactionDate = dto.InvoiceDate
        });

        await _db.SaveChangesAsync(ct);

        // 8. Invalidate cache
        await _cache.RemoveAsync($"dashboard:{request.TenantId}");
        await _cache.RemoveAsync($"invoices:{request.TenantId}");

        // 9. Generate E-Invoice if requested (async)
        if (dto.GenerateEInvoice && grandTotal >= 50000) // E-Invoice mandatory above ₹50,000
        {
            _ = Task.Run(async () => await _gstService.GenerateEInvoiceAsync(invoice.Id), ct);
        }

        return Result<InvoiceDetailDto>.Success(MapToDetail(invoice, customer));
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid tenantId, InvoiceType type, CancellationToken ct)
    {
        var prefix = type switch
        {
            InvoiceType.TaxInvoice => "INV",
            InvoiceType.CreditNote => "CN",
            InvoiceType.DebitNote => "DN",
            InvoiceType.ProformaInvoice => "PRO",
            InvoiceType.PurchaseOrder => "PO",
            InvoiceType.QuotationEstimate => "QT",
            _ => "INV"
        };

        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        var fy = month >= 4 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";

        var count = await _db.Invoices
            .CountAsync(i => i.TenantId == tenantId && i.Type == type
                && i.InvoiceDate.Year == year, ct);

        return $"{prefix}/{fy.Replace("-", "")}/{count + 1:D4}";
    }

    //    private static InvoiceDetailDto MapToDetail(Core.Entities.Invoicing.Invoice inv, Core.Entities.Invoicing.Customer cust) =>
    //        new(inv.Id, inv.InvoiceNumber, inv.Type, inv.InvoiceDate, inv.DueDate,
    //            new CustomerDto(cust.Id, cust.Code, cust.Name, cust.GSTIN, cust.PAN,
    //                cust.Email, cust.Phone, cust.Mobile,
    //                new AddressDto(cust.BillingAddress.Line1, cust.BillingAddress.Line2,
    //                    cust.BillingAddress.City, cust.BillingAddress.State, cust.BillingAddress.Pincode),
    //                null, cust.IsActive, cust.CreditLimit, cust.CreditDays,
    //                0, 0, 0, CustomerType.Retail, cust.Industry),
    //            inv.LineItems.Select(l => new InvoiceLineItemDto(
    //                l.Id, l.ProductId, null, l.HSNCode, l.Description,
    //                l.Quantity, l.Unit, l.UnitPrice, l.DiscountPercent, l.DiscountAmount,
    //                l.TaxableAmount, l.GSTRate, l.CGSTRate, l.CGSTAmount,
    //                l.SGSTRate, l.SGSTAmount, l.IGSTRate, l.IGSTAmount,
    //                l.CessRate, l.CessAmount, l.TotalAmount)),
    //            inv.SubTotal, inv.DiscountAmount, inv.TaxableAmount,
    //            inv.CGSTAmount, inv.SGSTAmount, inv.IGSTAmount, inv.CessAmount,
    //            inv.TotalAmount, inv.PaidAmount, inv.TotalAmount - inv.PaidAmount,
    //            inv.Status, inv.Notes, inv.TermsAndConditions, inv.PoNumber,
    //            inv.EInvoiceIRN, inv.EInvoiceStatus, inv.EWayBillNumber, inv.EWayBillStatus,
    //            Enumerable.Empty<PaymentDto>(),
    //            inv.Activities.Select(a => new InvoiceActivityDto(a.Id, a.Action, a.Description, a.CreatedAt, a.PerformedBy)));
    //}

    private static InvoiceDetailDto MapToDetail(
    Core.Entities.Invoicing.Invoice inv,
    Core.Entities.Invoicing.Customer cust)
    {
        return new InvoiceDetailDto
        {
            Id = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            InvoiceType = inv.Type.ToString(),
            Status = inv.Status.ToString(),

            CustomerName = cust.Name,
            CustomerGSTIN = cust.GSTIN,

            InvoiceDate = inv.InvoiceDate,
            DueDate = inv.DueDate,

            SubTotal = inv.SubTotal,
            TaxableAmount = inv.TaxableAmount,
            CGSTAmount = inv.CGSTAmount,
            SGSTAmount = inv.SGSTAmount,
            IGSTAmount = inv.IGSTAmount,
            TotalAmount = inv.TotalAmount,
            BalanceAmount = inv.TotalAmount - inv.PaidAmount,

            EInvoiceStatus = inv.EInvoiceStatus.ToString(),
            IRN = inv.EInvoiceIRN,
            //AckNumber = inv.EInvoiceAckNumber,
            AckNumber = null,
            SignedQRCode = inv.SignedQRCode,
            EWayBillNumber = inv.EWayBillNumber,

            LineItems = inv.LineItems
                .Select((l, index) => new InvoiceLineItemDto
                {
                    SlNo = index + 1,
                    ProductId = l.ProductId,
                    ItemName = l.Description ?? string.Empty,
                    HSNSACCode = l.HSNSACCode,
                    Quantity = l.Quantity,
                    Unit = l.Unit,
                    UnitPrice = l.UnitPrice,
                    DiscountPercent = Convert.ToDecimal(l.DiscountPercent),
                    GSTRate = l.GSTRate,
                    TaxableAmount = l.TaxableAmount,
                    CGSTAmount = l.CGSTAmount,
                    SGSTAmount = l.SGSTAmount,
                    IGSTAmount = l.IGSTAmount,
                    TotalAmount = l.TotalAmount
                })
                .ToList(),

            Payments = inv.Payments?
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate
                    //PaymentMode = p.PaymentMode,
                    //TransactionReference = p.TransactionReference,
                    //BankName = p.BankName,
                    //Notes = p.Notes
                })
                .ToList() ?? new List<PaymentDto>()
        };
    }

    public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Result<PaymentDto>>
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly ICacheService _cache;

        public RecordPaymentCommandHandler(AppDbContext db, INotificationService notificationService, ICacheService cache)
        {
            _db = db;
            _notificationService = notificationService;
            _cache = cache;
        }

        public async Task<Result<PaymentDto>> Handle(RecordPaymentCommand request, CancellationToken ct)
        {
            var dto = request.Dto;

            var invoice = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId && i.TenantId == request.TenantId, ct);

            if (invoice == null) return Result<PaymentDto>.Failure("Invoice not found");
            if (invoice.Status == InvoiceStatus.Cancelled) return Result<PaymentDto>.Failure("Cannot record payment on cancelled invoice");

            var balance = invoice.TotalAmount - invoice.PaidAmount;
            if (dto.Amount > balance)
                return Result<PaymentDto>.Failure($"Payment amount ₹{dto.Amount:N2} exceeds balance ₹{balance:N2}");

            var payment = new Payment
            {
                TenantId = request.TenantId,
                InvoiceId = invoice.Id,
                Amount = dto.Amount,
                PaymentDate = dto.PaymentDate,
                //PaymentMode = dto.PaymentMode,
                TransactionReference = dto.TransactionReference,
                BankName = dto.BankName,
                Notes = dto.Notes
            };

            invoice.PaidAmount += dto.Amount;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;

            invoice.Activities.Add(new InvoiceActivity
            {
                Action = "PaymentRecorded",
                Description = $"Payment of ₹{dto.Amount:N2} via {dto.PaymentMode} recorded",
                PerformedBy = "system"
            });

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);

            // Send payment receipt notification
            //    await _notificationService.SendPaymentReceiptAsync(invoice, payment);
            //    await _cache.RemoveAsync($"dashboard:{request.TenantId}");

            //return Result<PaymentDto>.Success(new PaymentDto(
            //    payment.Id, payment.Amount, payment.PaymentDate, payment.PaymentMode,
            //    payment.TransactionReference, payment.BankName, payment.Notes));
            return Result<PaymentDto>.Success(new PaymentDto
            {
                Id = payment.Id,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate
            });
        }
    }
}
