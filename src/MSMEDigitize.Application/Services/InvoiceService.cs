using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure.Data;
using MSMEDigitize.Infrastructure.ExternalServices;
using QRCoder;
using System.Text;
using static QRCoder.PayloadGenerator.SwissQrCode;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MSMEDigitize.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IStorageService _storage;
    private readonly ILogger<InvoiceService> _logger;
    private readonly IConfiguration _config;

    public InvoiceService(AppDbContext db, INotificationService notificationService,
        IStorageService storage, ILogger<InvoiceService> logger, IConfiguration config)
    {
        _db = db;
        _notificationService = notificationService;
        _storage = storage;
        _logger = logger;
        _config = config;
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid tenantId, InvoiceType type)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId)
            ?? throw new InvalidOperationException("Tenant not found");

        var prefix = type switch
        {
            InvoiceType.Tax => tenant.InvoicePrefix,
            InvoiceType.Proforma => "PI",
            InvoiceType.Estimate => "EST",
            InvoiceType.CreditNote => "CN",
            InvoiceType.DebitNote => "DN",
            InvoiceType.DeliveryChalan => "DC",
            InvoiceType.PurchaseOrder => "PO",
            _ => tenant.InvoicePrefix
        };

        var fy = GetCurrentFinancialYear();
        var count = await _db.Invoices
            .CountAsync(i => i.TenantId == tenantId && i.Type == type
                && i.CreatedAt >= GetFYStart(tenant.FinancialYearStart == MSMEDigitize.Core.Enums.FinancialYearStart.April ? 4 : 1));

        return $"{prefix}/{fy}/{(count + tenant.InvoiceStartNumber):D4}";
    }

    public async Task<byte[]> GeneratePDFAsync(Guid invoiceId, Guid tenantId)
    {
        var invoice = await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Customer)
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId)
            ?? throw new InvalidOperationException("Invoice not found");

        var html = GenerateInvoiceHtml(invoice);
        return GeneratePdfFromHtml(html);
    }

    public async Task<bool> SendInvoiceEmailAsync(Guid invoiceId, Guid tenantId, string? additionalEmail = null)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);

        if (invoice == null) return false;

        var pdf = await GeneratePDFAsync(invoiceId, tenantId);
        var pdfUrl = await _storage.UploadAsync(
            pdf,
            $"invoice_{invoice.InvoiceNumber}.pdf",
            "application/pdf");

        var emailTo = additionalEmail ?? invoice.CustomerEmail ?? invoice.Customer?.Email ?? "";
        if (string.IsNullOrEmpty(emailTo)) return false;

        var html = BuildInvoiceEmailHtml(invoice, pdfUrl);
        await _notificationService.SendEmailAsync(
            emailTo,
            $"Invoice {invoice.InvoiceNumber} from {invoice.Tenant.BusinessName}",
            html);

        invoice.IsSentToCustomer = true;
        invoice.SentAt = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.Sent;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<Invoice> CreateFromTemplateAsync(Guid recurringConfigId, Guid tenantId)
    {
        var config = await _db.RecurringInvoiceConfigs
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == recurringConfigId && r.TenantId == tenantId)
            ?? throw new InvalidOperationException("Recurring config not found");

        if (string.IsNullOrEmpty(config.TemplateData))
            throw new InvalidOperationException("No template data");

        var templateInvoice = System.Text.Json.JsonSerializer.Deserialize<Invoice>(config.TemplateData);
        if (templateInvoice == null) throw new InvalidOperationException("Invalid template");

        var newInvoice = new Invoice
        {
            TenantId = tenantId,
            Type = InvoiceType.Tax,
            Status = InvoiceStatus.Draft,
            CustomerId = config.CustomerId,
            CustomerName = config.Customer.Name,
            CustomerEmail = config.Customer.Email,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            InvoiceNumber = await GenerateInvoiceNumberAsync(tenantId, InvoiceType.Tax),
            RecurringConfigId = recurringConfigId
        };

        _db.Invoices.Add(newInvoice);
        config.NextInvoiceDate = CalculateNextDate(config.NextInvoiceDate, config.Frequency);
        await _db.SaveChangesAsync();

        return newInvoice;
    }

    public async Task<string?> GenerateEInvoiceAsync(Guid invoiceId, Guid tenantId)
    {
        // Integration with NIC e-Invoice API (IRP)
        // This would call the government's Invoice Registration Portal
        var invoice = await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);

        if (invoice == null) return null;

        // Generate QR code for e-invoice
        var qrData = BuildQRData(invoice);
        var qrUrl = GenerateQRCode(qrData);

        invoice.IRN = GenerateMockIRN(invoice);  // In production: call IRP API
        invoice.QRCode = qrUrl;
        await _db.SaveChangesAsync();

        return invoice.IRN;
    }

    public async Task<string?> GenerateEWayBillAsync(Guid invoiceId, Guid tenantId)
    {
        // E-Way Bill API integration with NIC
        var invoice = await _db.Invoices.FindAsync(invoiceId);
        if (invoice == null) return null;

        // Mock: In production, call GSP/NIC E-Way Bill API
        var ewbNumber = $"EWB{DateTime.UtcNow.Ticks}";
        invoice.EWayBillNumber = ewbNumber;
        await _db.SaveChangesAsync();

        return ewbNumber;
    }

    private string GenerateInvoiceHtml(Invoice invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head>");
        sb.AppendLine("<meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
            body { font-family: 'Segoe UI', sans-serif; font-size: 12px; color: #333; margin: 0; }
            .invoice-box { max-width: 800px; margin: auto; padding: 30px; }
            .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 30px; }
            .company-name { font-size: 24px; font-weight: bold; color: #2563EB; }
            .invoice-title { font-size: 28px; color: #1E40AF; text-align: right; }
            .invoice-meta { text-align: right; }
            .billing-section { display: flex; justify-content: space-between; margin-bottom: 20px; }
            .billing-box { background: #F8FAFC; padding: 15px; border-radius: 8px; flex: 1; margin: 0 5px; }
            .billing-box h4 { color: #64748B; font-size: 10px; text-transform: uppercase; margin-bottom: 5px; }
            table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
            th { background: #1E40AF; color: white; padding: 10px; text-align: left; }
            td { padding: 8px 10px; border-bottom: 1px solid #E2E8F0; }
            tr:nth-child(even) td { background: #F8FAFC; }
            .totals { float: right; width: 350px; }
            .totals table td { border: none; padding: 5px 10px; }
            .total-row td { font-weight: bold; font-size: 14px; background: #EFF6FF; }
            .gst-breakdown { background: #F0FDF4; padding: 10px; border-radius: 6px; margin-bottom: 10px; }
            .footer { margin-top: 30px; border-top: 2px solid #E2E8F0; padding-top: 15px; }
            .amount-words { background: #FEF3C7; padding: 10px; border-radius: 6px; margin: 10px 0; }
            .qr-code { float: right; }
            .status-badge { display: inline-block; padding: 4px 12px; border-radius: 20px; font-weight: bold; }
        ");
        sb.AppendLine("</style></head><body><div class='invoice-box'>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<div><div class='company-name'>{invoice.Tenant?.BusinessName}</div>");
        sb.AppendLine($"<div>{invoice.Tenant?.AddressLine1}, {invoice.Tenant?.City}, {invoice.Tenant?.State} - {invoice.Tenant?.Pincode}</div>");
        sb.AppendLine($"<div>GST: {invoice.Tenant?.GSTNumber} | Ph: {invoice.Tenant?.Phone}</div></div>");
        sb.AppendLine($"<div><div class='invoice-title'>TAX INVOICE</div>");
        sb.AppendLine($"<div class='invoice-meta'><strong>{invoice.InvoiceNumber}</strong><br/>");
        sb.AppendLine($"Date: {invoice.InvoiceDate:dd/MM/yyyy}<br/>");
        sb.AppendLine($"Due: {invoice.DueDate:dd/MM/yyyy}</div></div>");
        sb.AppendLine("</div>");

        // Bill To
        sb.AppendLine("<div class='billing-section'><div class='billing-box'><h4>Bill To</h4>");
        sb.AppendLine($"<strong>{invoice.CustomerName}</strong><br/>{invoice.CustomerAddress}");
        if (!string.IsNullOrEmpty(invoice.CustomerGST))
            sb.AppendLine($"<br/>GST: {invoice.CustomerGST}");
        sb.AppendLine("</div>");
        if (!string.IsNullOrEmpty(invoice.PlaceOfSupply))
            sb.AppendLine($"<div class='billing-box'><h4>Place of Supply</h4>{invoice.PlaceOfSupply}</div>");
        sb.AppendLine("</div>");

        // Line items table
        sb.AppendLine("<table><thead><tr><th>#</th><th>Description</th><th>HSN/SAC</th><th>Qty</th><th>Rate</th><th>Taxable</th><th>GST</th><th>Amount</th></tr></thead><tbody>");
        int sr = 1;
        foreach (var item in invoice.LineItems)
        {
            sb.AppendLine($"<tr><td>{sr++}</td><td>{item.Description}</td><td>{item.HSNSACCode}</td>");
            sb.AppendLine($"<td>{item.Quantity} {item.Unit}</td><td>₹{item.UnitPrice:N2}</td>");
            sb.AppendLine($"<td>₹{item.TaxableAmount:N2}</td><td>{item.GSTRate}%</td><td>₹{item.TotalAmount:N2}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        // GST Breakdown
        sb.AppendLine("<div style='display:flex; justify-content:space-between;'>");
        sb.AppendLine("<div class='gst-breakdown' style='flex:1; margin-right:10px;'><strong>GST Breakdown</strong><br/>");
        if (invoice.CGSTAmount > 0) sb.AppendLine($"CGST: ₹{invoice.CGSTAmount:N2}<br/>");
        if (invoice.SGSTAmount > 0) sb.AppendLine($"SGST: ₹{invoice.SGSTAmount:N2}<br/>");
        if (invoice.IGSTAmount > 0) sb.AppendLine($"IGST: ₹{invoice.IGSTAmount:N2}<br/>");
        sb.AppendLine("</div>");

        // Totals
        sb.AppendLine("<div class='totals'><table>");
        sb.AppendLine($"<tr><td>Sub Total:</td><td>₹{invoice.SubTotal:N2}</td></tr>");
        if (invoice.DiscountAmount > 0)
            sb.AppendLine($"<tr><td>Discount:</td><td>-₹{invoice.DiscountAmount:N2}</td></tr>");
        if (invoice.CGSTAmount > 0) sb.AppendLine($"<tr><td>CGST:</td><td>₹{invoice.CGSTAmount:N2}</td></tr>");
        if (invoice.SGSTAmount > 0) sb.AppendLine($"<tr><td>SGST:</td><td>₹{invoice.SGSTAmount:N2}</td></tr>");
        if (invoice.IGSTAmount > 0) sb.AppendLine($"<tr><td>IGST:</td><td>₹{invoice.IGSTAmount:N2}</td></tr>");
        sb.AppendLine($"<tr class='total-row'><td>Total:</td><td>₹{invoice.TotalAmount:N2}</td></tr>");
        if (invoice.PaidAmount > 0)
        {
            sb.AppendLine($"<tr><td>Paid:</td><td>₹{invoice.PaidAmount:N2}</td></tr>");
            sb.AppendLine($"<tr><td><strong>Balance Due:</strong></td><td><strong>₹{invoice.BalanceAmount:N2}</strong></td></tr>");
        }
        sb.AppendLine("</table></div></div>");

        // Amount in words
        if (!string.IsNullOrEmpty(invoice.AmountInWords))
            sb.AppendLine($"<div class='amount-words'><strong>Amount in Words:</strong> {invoice.AmountInWords}</div>");

        // Bank Details
        if (invoice.Tenant?.BankAccountNumber?.Length > 0)
        {
            sb.AppendLine("<div class='billing-box'><h4>Bank Details</h4>");
            sb.AppendLine($"Bank: {invoice.Tenant.BankName} | A/C: {invoice.Tenant.BankAccountNumber} | IFSC: {invoice.Tenant.BankIFSC}</div>");
        }

        // Footer
        sb.AppendLine("<div class='footer'>");
        if (!string.IsNullOrEmpty(invoice.Notes))
            sb.AppendLine($"<p><strong>Notes:</strong> {invoice.Notes}</p>");
        sb.AppendLine("<p style='text-align:center; color:#94A3B8; font-size:10px;'>This is a computer generated invoice. Powered by MSMEDigitize</p>");
        sb.AppendLine("</div></div></body></html>");

        return sb.ToString();
    }

    private byte[] GeneratePdfFromHtml(string html)
    {
        // Using iTextSharp or DinkToPdf in production
        return Encoding.UTF8.GetBytes(html); // Placeholder - replace with actual PDF lib
    }

    private string BuildInvoiceEmailHtml(Invoice invoice, string pdfUrl)
    {
        return $@"
        <div style='font-family:Segoe UI,sans-serif;max-width:600px;margin:auto;'>
            <div style='background:#1E40AF;color:white;padding:20px;text-align:center;'>
                <h2>Invoice from {invoice.Tenant?.BusinessName}</h2>
            </div>

            <div style='padding:30px;'>
                <p>Dear {invoice.CustomerName},</p>

                <p>
                    Please find your invoice 
                    <strong>{invoice.InvoiceNumber}</strong> 
                    for 
                    <strong>₹{invoice.TotalAmount:N2}</strong>.
                </p>

                <p>
                    Due Date: <strong>{invoice.DueDate:dd MMMM yyyy}</strong>
                </p>

                <div style='background:#EFF6FF;padding:15px;border-radius:8px;text-align:center;margin:20px 0;'>
                    <a href='{pdfUrl}' 
                       style='background:#2563EB;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block;'>
                        Download Invoice PDF
                    </a>
                </div>

                <p>
                    For any queries, please contact {invoice.Tenant?.Email}
                </p>
            </div>

            <div style='background:#F8FAFC;padding:15px;text-align:center;font-size:12px;color:#64748B;'>
                Powered by MSMEDigitize | India's Smart Business Platform
            </div>
        </div>";
    }

    private string BuildQRData(Invoice invoice)
    {
        return $"{invoice.Tenant?.GSTNumber}|{invoice.Tenant?.BusinessName}|{invoice.CustomerGST}|{invoice.CustomerName}|{invoice.InvoiceDate:dd-MM-yyyy}|{invoice.TotalAmount}|{invoice.IGSTAmount}|{invoice.CGSTAmount}|{invoice.SGSTAmount}|{invoice.InvoiceNumber}";
    }

    private string GenerateQRCode(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var bytes = qrCode.GetGraphic(5);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private string GenerateMockIRN(Invoice invoice)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var input = $"{invoice.Tenant?.GSTNumber}{invoice.InvoiceNumber}{invoice.InvoiceDate:yyyyMMdd}{invoice.TotalAmount}";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private string GetCurrentFinancialYear()
    {
        var now = DateTime.Now;
        return now.Month >= 4
            ? $"{now.Year % 100:D2}-{(now.Year + 1) % 100:D2}"
            : $"{(now.Year - 1) % 100:D2}-{now.Year % 100:D2}";
    }

    private DateTime GetFYStart(int startMonth)
    {
        var month = startMonth;
        var now = DateTime.Now;
        var year = now.Month >= month ? now.Year : now.Year - 1;
        return new DateTime(year, month, 1);
    }

    private DateTime CalculateNextDate(DateTime current, RecurringFrequency freq) => freq switch
    {
        RecurringFrequency.Weekly => current.AddDays(7),
        RecurringFrequency.Fortnightly => current.AddDays(14),
        RecurringFrequency.Monthly => current.AddMonths(1),
        RecurringFrequency.Quarterly => current.AddMonths(3),
        RecurringFrequency.HalfYearly => current.AddMonths(6),
        RecurringFrequency.Annually => current.AddYears(1),
        _ => current.AddMonths(1)
    };

    // ── Controller-facing CRUD stubs ─────────────────────────────────────────
    public async Task<MSMEDigitize.Core.DTOs.PagedResult<object>> GetInvoicesAsync(
        Guid tenantId, int page, int size, string? search,
        MSMEDigitize.Core.Enums.InvoiceStatus? status, CancellationToken ct = default)
    {
        var query = _db.Invoices.Where(i => i.TenantId == tenantId);
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(i => i.InvoiceNumber.Contains(search) || i.CustomerName.Contains(search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new MSMEDigitize.Core.DTOs.PagedResult<object>
        {
            Items = items.Cast<object>().ToList(),
            TotalCount = total,
            PageNumber = page,
            PageSize = size
        };
    }

    public async Task<object?> GetInvoiceAsync(Guid invoiceId, Guid tenantId, CancellationToken ct = default)
        => await _db.Invoices.Include(i => i.LineItems).Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId, ct);

    public async Task<MSMEDigitize.Core.DTOs.DashboardMetricsDto> GetDashboardMetricsAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var invoices = await _db.Invoices.Where(i => i.TenantId == tenantId).ToListAsync(ct);
        return new MSMEDigitize.Core.DTOs.DashboardMetricsDto
        {
            TotalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount),
            MonthlyRevenue = invoices.Where(i => i.InvoiceDate >= monthStart && i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount),
            PendingInvoices = invoices.Count(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue),
            OverdueAmount = invoices.Where(i => i.Status == InvoiceStatus.Overdue).Sum(i => i.BalanceAmount)
        };
    }

    public async Task<object> GetGSTSummaryAsync(Guid tenantId, int month, int year, CancellationToken ct = default)
    {
        var invoices = await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceDate.Month == month && i.InvoiceDate.Year == year)
            .ToListAsync(ct);
        return new
        {
            TotalTaxableValue = invoices.Sum(i => i.SubTotal),
            TotalCGST = invoices.Sum(i => i.CGSTAmount),
            TotalSGST = invoices.Sum(i => i.SGSTAmount),
            TotalIGST = invoices.Sum(i => i.IGSTAmount),
            TotalGST = invoices.Sum(i => i.CGSTAmount + i.SGSTAmount + i.IGSTAmount)
        };
    }

    public async Task<MSMEDigitize.Core.Common.Result<object>> CreateInvoiceAsync(
        Guid tenantId, object dto, CancellationToken ct = default)
        => MSMEDigitize.Core.Common.Result<object>.Failure("Use CreateInvoiceCommand via MediatR");

    public async Task<MSMEDigitize.Core.Common.Result<bool>> SendInvoiceAsync(
        Guid invoiceId, Guid tenantId, CancellationToken ct = default)
    {
        var ok = await SendInvoiceEmailAsync(invoiceId, tenantId);
        return ok ? MSMEDigitize.Core.Common.Result<bool>.Success(ok) : MSMEDigitize.Core.Common.Result<bool>.Failure("Failed to send");
    }

    public async Task<MSMEDigitize.Core.Common.Result<bool>> RecordPaymentAsync(
        Guid tenantId, object dto, CancellationToken ct = default)
        => MSMEDigitize.Core.Common.Result<bool>.Failure("Use RecordPaymentCommand via MediatR");

    public async Task<byte[]> GetInvoicePdfAsync(Guid invoiceId, Guid tenantId, CancellationToken ct = default)
        => await GeneratePDFAsync(invoiceId, tenantId);

}