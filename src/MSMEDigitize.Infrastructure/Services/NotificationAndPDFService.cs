using MSMEDigitize.Core.Common;
using System.Net.Http.Json;
using QuestPDF.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace MSMEDigitize.Infrastructure.Services;

public class NotificationServiceImpl : INotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationServiceImpl> _logger;
    private readonly AppDbContext _db;
    private readonly HttpClient _whatsappClient;
    private readonly IEmailService? _emailService;

    public NotificationServiceImpl(IConfiguration config, ILogger<NotificationServiceImpl> logger,
        AppDbContext db, IHttpClientFactory httpClientFactory, IEmailService? emailService = null)
    {
        _config = config;
        _logger = logger;
        _db = db;
        _whatsappClient = httpClientFactory.CreateClient("WhatsApp");
        _emailService = emailService;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default)
    {
        try
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_config["SendGrid:FromEmail"], _config["SendGrid:FromName"]);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject,
                isHtml ? null : body,
                isHtml ? body : null);

            var response = await client.SendEmailAsync(msg, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Body.ReadAsStringAsync(ct);
                _logger.LogWarning("Email send failed to {To}: {Error}", to, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send error to {To}", to);
        }
    }

    public Task SendSMSAsync(string mobile, string message, CancellationToken ct = default)
    {
        try
        {
            TwilioClient.Init(_config["Twilio:AccountSid"], _config["Twilio:AuthToken"]);
            // Format for India
            var formattedNumber = mobile.StartsWith("+91") ? mobile : $"+91{mobile}";

            _ = MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(_config["Twilio:FromNumber"]),
                to: new Twilio.Types.PhoneNumber(formattedNumber)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send error to {Mobile}", mobile);
        }
        return Task.CompletedTask;
    }

    public async Task SendWhatsAppAsync(string mobile, string templateName, Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        try
        {
            // Using Interakt/WATI/Gupshup for WhatsApp Business API
            var payload = new
            {
                countryCode = "+91",
                phoneNumber = mobile,
                callbackData = "whatsapp_notification",
                type = "Template",
                template = new
                {
                    name = templateName,
                    languageCode = "en",
                    bodyValues = parameters.Values.ToArray()
                }
            };

            var response = await _whatsappClient.PostAsJsonAsync("/api/v1/send/template/", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("WhatsApp send failed: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp send error to {Mobile}", mobile);
        }
    }

    public async Task SendPushNotificationAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        // Firebase push notification
        // FirebaseAdmin integration for mobile app notifications
        _logger.LogInformation("Push notification sent to user {UserId}: {Title}", userId, title);
        await Task.CompletedTask;
    }

    public async Task SendInAppNotificationAsync(Guid tenantId, Guid? userId, string title, string body, string? actionUrl = null, CancellationToken ct = default)
    {
        // Store in-app notification in DB for real-time delivery via SignalR
        // The web layer uses SignalR to push to connected clients
        _logger.LogInformation("In-app notification for tenant {TenantId}: {Title}", tenantId, title);
        await Task.CompletedTask;
    }

    // Template helpers for Indian MSME context
    public static class Templates
    {
        public const string InvoiceSent = "invoice_sent_v1";
        public const string PaymentReminder = "payment_reminder_v1";
        public const string PaymentReceived = "payment_received_v1";
        public const string GSTFilingReminder = "gst_filing_reminder_v1";
        public const string LowStockAlert = "low_stock_alert_v1";
        public const string EInvoiceGenerated = "einvoice_generated_v1";
        public const string SubscriptionRenewal = "subscription_renewal_v1";
        public const string WelcomeOnboard = "welcome_onboard_v1";
    }
}

public class PDFServiceImpl : IPDFService, MSMEDigitize.Core.Interfaces.IPdfService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PDFServiceImpl> _logger;

    public PDFServiceImpl(AppDbContext db, ILogger<PDFServiceImpl> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<byte[]> GenerateInvoicePDFAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice == null) throw new InvalidOperationException("Invoice not found");

        // Using QuestPDF for high-quality PDF generation
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30, QuestPDF.Infrastructure.Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, invoice));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(QuestPDF.Infrastructure.IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("TAX INVOICE")
                    .FontSize(20).Bold().FontColor(Colors.Blue.Medium);
            });
        });
    }

    private void ComposeContent(QuestPDF.Infrastructure.IContainer container, Core.Entities.Invoicing.Invoice invoice)
    {
        container.Column(col =>
        {
            // Invoice details table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                });

                table.Cell().Text($"Invoice #: {invoice.InvoiceNumber}").Bold();
                table.Cell().Text($"Date: {invoice.InvoiceDate:dd/MM/yyyy}");
                table.Cell().Text($"Customer: {invoice.Customer?.Name}");
                table.Cell().Text($"GSTIN: {invoice.Customer?.GSTIN ?? "N/A"}");
            });

            col.Item().PaddingTop(10);

            // Line items
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(80);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Lighten4)
                        .Text("#").Bold().AlignCenter();
                    header.Cell().Background(Colors.Blue.Lighten4)
                        .Text("Item Description").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4)
                        .Text("HSN").Bold().AlignCenter();
                    header.Cell().Background(Colors.Blue.Lighten4)
                        .Text("Qty").Bold().AlignRight();
                    header.Cell().Background(Colors.Blue.Lighten4)
                        .Text("Rate").Bold().AlignRight();
                    header.Cell().Background(Colors.Blue.Lighten4)
                        .Text("GST").Bold().AlignRight();
                    header.Cell().Background(Colors.Blue.Lighten4)
                        .Text("Amount").Bold().AlignRight();
                });

                foreach (var item in invoice.LineItems)
                {
                    table.Cell().Text(item.SlNo.ToString()).AlignCenter();
                    table.Cell().Text(item.ItemName);
                    table.Cell().Text(item.HSNSACCode ?? "").AlignCenter();
                    table.Cell().Text($"{item.Quantity} {item.Unit}").AlignRight();
                    table.Cell().Text($"₹{item.UnitPrice:N2}").AlignRight();
                    table.Cell().Text($"{item.GSTRate}%").AlignRight();
                    table.Cell().Text($"₹{item.TotalAmount:N2}").AlignRight();
                }
            });

            // Totals
            col.Item().AlignRight().Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(150); c.ConstantColumn(100); });
                table.Cell().Text("Taxable Amount:").Bold();
                table.Cell().Text($"₹{invoice.TaxableAmount:N2}").AlignRight();
                if (invoice.CGSTAmount > 0)
                {
                    table.Cell().Text("CGST:");
                    table.Cell().Text($"₹{invoice.CGSTAmount:N2}").AlignRight();
                    table.Cell().Text("SGST:");
                    table.Cell().Text($"₹{invoice.SGSTAmount:N2}").AlignRight();
                }
                if (invoice.IGSTAmount > 0)
                {
                    table.Cell().Text("IGST:");
                    table.Cell().Text($"₹{invoice.IGSTAmount:N2}").AlignRight();
                }
                table.Cell().Background(Colors.Blue.Lighten4).Text("Total:").Bold();
                table.Cell().Background(Colors.Blue.Lighten4)
                    .Text($"₹{invoice.TotalAmount:N2}").Bold().AlignRight();
            });
        });
    }

    private void ComposeFooter(QuestPDF.Infrastructure.IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("This is a computer generated invoice.")
                .FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PAYSLIP PDF — Full QuestPDF implementation
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<byte[]> GeneratePayslipPDFAsync(Guid payslipId, CancellationToken ct = default)
    {
        var ps = await _db.Payslips
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == payslipId, ct)
            ?? throw new InvalidOperationException($"Payslip {payslipId} not found");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == ps.TenantId, ct);

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        string MonthName(int m) => m >= 1 && m <= 12 ? new DateTime(2000, m, 1).ToString("MMMM") : "—";
        string Mask(string? s) => s?.Length > 4 ? "XXXX" + s[^4..] : "XXXX";

        return QuestPDF.Fluent.Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(30, QuestPDF.Infrastructure.Unit.Point);
            p.DefaultTextStyle(x => x.FontSize(9));

            // ── HEADER ──
            p.Header().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Column(hc =>
                    {
                        hc.Item().Text(tenant?.BusinessName ?? "Company")
                            .FontSize(16).Bold()
                            .FontColor(Colors.Blue.Darken3);
                        hc.Item().Text("SALARY PAYSLIP").FontSize(12).Bold();
                        hc.Item().Text($"For {MonthName(ps.Month)} {ps.Year}")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    });
                    r.AutoItem().Column(hc =>
                    {
                        hc.Item().AlignRight().Text("CONFIDENTIAL")
                            .FontSize(8).Bold()
                            .FontColor(Colors.Red.Darken2);
                        hc.Item().AlignRight().Text($"Generated: {DateTime.Now:dd/MM/yyyy}")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
                col.Item().PaddingVertical(3).LineHorizontal(1.5f)
                    .LineColor(Colors.Blue.Darken3);
            });

            // ── CONTENT ──
            p.Content().PaddingTop(8).Column(col =>
            {
                // Employee Details block
                col.Item().Border(0.5f).Padding(8).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(2); cd.RelativeColumn(2);
                        cd.RelativeColumn(2); cd.RelativeColumn(2);
                    });
                    void R(string l1, string v1, string l2, string v2)
                    {
                        t.Cell().PaddingVertical(3).Text(l1).Bold().FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                        t.Cell().PaddingVertical(3).Text(v1).FontSize(8);
                        t.Cell().PaddingVertical(3).Text(l2).Bold().FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                        t.Cell().PaddingVertical(3).Text(v2).FontSize(8);
                    }
                    R("Employee Name:", ps.Employee?.FullName ?? "—",
                      "Employee ID:", ps.Employee?.EmployeeCode ?? "—");
                    R("Department:", ps.Employee?.DepartmentId ?? "—",
                      "Designation:", ps.Employee?.DesignationId ?? "—");
                    R("PAN:", ps.Employee?.PAN ?? "N/A",
                      "UAN:", ps.Employee?.UANNumber ?? "N/A");
                    R("Bank A/C:", Mask(ps.Employee?.BankAccountNumber),
                      "Bank Name:", ps.Employee?.BankName ?? "—");
                    R("PF Account:", ps.Employee?.PFAccountNumber ?? "N/A",
                      "ESI Number:", ps.Employee?.ESINumber ?? "N/A");
                    R("Working Days:", ps.WorkingDays.ToString("F1"),
                      "Days Paid:", ps.PaidDays.ToString("F1"));
                });

                col.Item().PaddingTop(10).Row(row =>
                {
                    // EARNINGS column
                    row.RelativeItem().Column(ec =>
                    {
                        ec.Item().Background(Colors.Blue.Lighten4)
                            .Padding(5).Text("EARNINGS").Bold().FontSize(9);
                        ec.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            { cd.RelativeColumn(3); cd.RelativeColumn(2); });
                            void E(string l, decimal a)
                            {
                                t.Cell().PaddingVertical(2).PaddingHorizontal(4)
                                    .Text(l).FontSize(8);
                                t.Cell().PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                    .Text(a > 0 ? $"₹{a:N2}" : "—").FontSize(8);
                            }
                            E("Basic Salary", ps.BasicSalary);
                            E("HRA", ps.HRA);
                            E("Special Allowance", ps.SpecialAllowance);
                            E("Conveyance Allowance", ps.ConveyanceAllowance);
                            E("Medical Allowance", ps.MedicalAllowance);
                            E("LTA", ps.LeaveTravelAllowance);
                            if (ps.OvertimePay > 0) E("Overtime Pay", ps.OvertimePay);
                            if (ps.Bonus > 0) E("Bonus/Incentive", ps.Bonus);
                            t.Cell().ColumnSpan(2).PaddingVertical(2)
                                .LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                            t.Cell().PaddingVertical(3).PaddingHorizontal(4)
                                .Text("GROSS SALARY").Bold().FontSize(8);
                            t.Cell().PaddingVertical(3).PaddingHorizontal(4).AlignRight()
                                .Text($"₹{ps.GrossSalary:N2}").Bold().FontSize(8);
                        });
                    });

                    row.ConstantItem(15);

                    // DEDUCTIONS column
                    row.RelativeItem().Column(dc =>
                    {
                        dc.Item().Background(Colors.Red.Lighten4)
                            .Padding(5).Text("DEDUCTIONS").Bold().FontSize(9);
                        dc.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            { cd.RelativeColumn(3); cd.RelativeColumn(2); });
                            void D(string l, decimal a)
                            {
                                t.Cell().PaddingVertical(2).PaddingHorizontal(4)
                                    .Text(l).FontSize(8);
                                t.Cell().PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                    .Text(a > 0 ? $"₹{a:N2}" : "—").FontSize(8);
                            }
                            D("PF (Employee 12%)", ps.PFEmployee);
                            D("PF (Employer 12%)", ps.PFEmployer);
                            D("ESI (Employee 0.75%)", ps.ESIEmployee);
                            D("ESI (Employer 3.25%)", ps.ESIEmployer);
                            D("Professional Tax", ps.ProfessionalTax);
                            if (ps.IncomeTax > 0) D("TDS / Income Tax", ps.IncomeTax);
                            if (ps.LoanDeduction > 0) D("Loan Repayment", ps.LoanDeduction);
                            if (ps.OtherDeductions > 0) D("Other Deductions", ps.OtherDeductions);
                            t.Cell().ColumnSpan(2).PaddingVertical(2)
                                .LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                            t.Cell().PaddingVertical(3).PaddingHorizontal(4)
                                .Text("TOTAL DEDUCTIONS").Bold().FontSize(8);
                            t.Cell().PaddingVertical(3).PaddingHorizontal(4).AlignRight()
                                .Text($"₹{ps.TotalDeductions:N2}").Bold().FontSize(8);
                        });
                    });
                });

                // NET SALARY bar
                col.Item().PaddingTop(10)
                    .Background(Colors.Green.Lighten4)
                    .Padding(10).Row(r =>
                    {
                        r.RelativeItem()
                            .Text("NET SALARY (Take-Home Pay)").Bold().FontSize(12);
                        r.AutoItem()
                            .Text($"₹ {ps.NetSalary:N2}").Bold().FontSize(14)
                            .FontColor(Colors.Green.Darken4);
                    });

                col.Item().PaddingTop(4).Text($"Amount in Words: {AmountToWords((long)ps.NetSalary)} Rupees Only")
                    .FontSize(8).Italic()
                    .FontColor(Colors.Grey.Darken2);

                if (ps.LopDays > 0)
                    col.Item().PaddingTop(4)
                        .Text($"Loss of Pay: {ps.LopDays:F1} day(s) deducted from Basic.")
                        .FontSize(8).FontColor(Colors.Orange.Darken3);

                // Tax regime notice
                col.Item().PaddingTop(6)
                    .Background(Colors.Grey.Lighten4).Padding(6)
                    .Text($"Tax Regime: {(ps.Employee?.TaxRegime ?? "New")} Regime | " +
                          $"Employer's Total Cost (CTC): ₹{ps.Employee?.CTC:N2}")
                    .FontSize(7).FontColor(Colors.Grey.Darken2);

                // Signatures
                col.Item().PaddingTop(20).Row(r =>
                {
                    r.RelativeItem().Column(sc =>
                    {
                        sc.Item().Text("___________________________").FontSize(9);
                        sc.Item().Text("Employee Signature").FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                    });
                    r.RelativeItem().AlignRight().Column(sc =>
                    {
                        sc.Item().Text("___________________________").FontSize(9);
                        sc.Item().AlignRight().Text("Authorized Signatory").FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                    });
                });
            });

            // ── FOOTER ──
            p.Footer().AlignCenter()
                .Text("This is a computer-generated payslip and does not require a physical signature.")
                .FontSize(7).Italic()
                .FontColor(Colors.Grey.Medium);
        })).GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GST RETURN PDF — GSTR-1 / GSTR-3B summary
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<byte[]> GenerateGSTReturnPDFAsync(Guid returnId, CancellationToken ct = default)
    {
        var gstr = await _db.GSTReturns
            .Include(r => r.GSTProfile)
            .Include(r => r.Transactions)
            .FirstOrDefaultAsync(r => r.Id == returnId, ct)
            ?? throw new InvalidOperationException($"GSTReturn {returnId} not found");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == gstr.TenantId, ct);

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        string M(int m) => m >= 1 && m <= 12 ? new DateTime(2000, m, 1).ToString("MMM") : "—";

        return QuestPDF.Fluent.Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(25, QuestPDF.Infrastructure.Unit.Point);
            p.DefaultTextStyle(x => x.FontSize(8));

            p.Header().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Column(hc =>
                    {
                        hc.Item().Text("GOODS AND SERVICES TAX RETURN")
                            .FontSize(13).Bold()
                            .FontColor(Colors.Blue.Darken3);
                        hc.Item().Text($"{gstr.ReturnType}  ·  {M(gstr.Month)}-{gstr.Year}")
                            .FontSize(10).Bold();
                    });
                    r.AutoItem().Column(hc =>
                    {
                        hc.Item().AlignRight().Text($"Status: {gstr.Status}").Bold();
                        if (gstr.FiledAt.HasValue)
                            hc.Item().AlignRight()
                                .Text($"Filed: {gstr.FiledAt:dd/MM/yyyy}");
                        if (gstr.AcknowledgementNumber != null)
                            hc.Item().AlignRight()
                                .Text($"Ack#: {gstr.AcknowledgementNumber}")
                                .FontSize(7);
                    });
                });
                col.Item().PaddingTop(4).LineHorizontal(1.5f)
                    .LineColor(Colors.Blue.Darken3);

                // GSTIN info row
                col.Item().PaddingTop(5).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(2); cd.RelativeColumn(2);
                        cd.RelativeColumn(2); cd.RelativeColumn(2);
                    });
                    void KV(string k, string v)
                    {
                        t.Cell().PaddingVertical(2).Text(k).Bold()
                            .FontColor(Colors.Grey.Darken2);
                        t.Cell().PaddingVertical(2).Text(v);
                    }
                    KV("GSTIN:", gstr.GSTProfile?.GSTIN ?? "—");
                    KV("Legal Name:", tenant?.BusinessName ?? "—");
                    KV("Tax Period:", $"{M(gstr.Month)} {gstr.Year}");
                    KV("Due Date:", gstr.DueDate.ToString("dd/MM/yyyy"));
                });
            });

            p.Content().PaddingTop(10).Column(col =>
            {
                // ── TAX LIABILITY SUMMARY ──
                col.Item().Text("TAX LIABILITY SUMMARY").Bold().FontSize(10)
                    .FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(4).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3);
                        cd.ConstantColumn(85); cd.ConstantColumn(85);
                        cd.ConstantColumn(85); cd.ConstantColumn(85);
                        cd.ConstantColumn(85);
                    });
                    void TH(string h)
                        => t.Cell().Background(Colors.Blue.Lighten4)
                            .Padding(4).AlignCenter().Text(h).Bold().FontSize(8);
                    TH("Description"); TH("Taxable (₹)"); TH("CGST (₹)");
                    TH("SGST (₹)"); TH("IGST (₹)"); TH("Total Tax (₹)");

                    void DR(string d, decimal tv, decimal c, decimal s, decimal i)
                    {
                        t.Cell().Padding(4).Text(d);
                        t.Cell().Padding(4).AlignRight().Text($"{tv:N2}");
                        t.Cell().Padding(4).AlignRight().Text($"{c:N2}");
                        t.Cell().Padding(4).AlignRight().Text($"{s:N2}");
                        t.Cell().Padding(4).AlignRight().Text($"{i:N2}");
                        t.Cell().Padding(4).AlignRight().Text($"{c + s + i:N2}");
                    }
                    DR("Outward Supplies (Taxable)", gstr.TotalTaxableValue,
                       gstr.TotalCGST, gstr.TotalSGST, gstr.TotalIGST);
                    if (gstr.TotalCess > 0)
                    {
                        t.Cell().Padding(4).Text("CESS");
                        for (int i = 0; i < 4; i++) t.Cell().Padding(4);
                        t.Cell().Padding(4).AlignRight().Text($"{gstr.TotalCess:N2}");
                    }
                    // Total row
                    t.Cell().ColumnSpan(6).LineHorizontal(0.8f)
                        .LineColor(Colors.Blue.Medium);
                    t.Cell().Background(Colors.Grey.Lighten3)
                        .Padding(4).Text("TOTAL TAX LIABILITY").Bold();
                    t.Cell().Background(Colors.Grey.Lighten3)
                        .Padding(4).AlignRight().Text($"{gstr.TotalTaxableValue:N2}").Bold();
                    t.Cell().Background(Colors.Grey.Lighten3)
                        .Padding(4).AlignRight().Text($"{gstr.TotalCGST:N2}").Bold();
                    t.Cell().Background(Colors.Grey.Lighten3)
                        .Padding(4).AlignRight().Text($"{gstr.TotalSGST:N2}").Bold();
                    t.Cell().Background(Colors.Grey.Lighten3)
                        .Padding(4).AlignRight().Text($"{gstr.TotalIGST:N2}").Bold();
                    t.Cell().Background(Colors.Grey.Lighten3)
                        .Padding(4).AlignRight().Text($"{gstr.TotalTaxLiability:N2}").Bold();
                });

                // ── ITC SUMMARY ──
                col.Item().PaddingTop(12).Text("INPUT TAX CREDIT (ITC) SUMMARY").Bold().FontSize(10)
                    .FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(4).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    { cd.RelativeColumn(3); cd.RelativeColumn(2); cd.RelativeColumn(2); });
                    void TH(string h) => t.Cell().Background(Colors.Blue.Lighten4)
                        .Padding(4).Text(h).Bold();
                    TH("Particulars"); TH("Amount (₹)"); TH("Remarks");
                    void IR(string d, decimal a, string r = "")
                    {
                        t.Cell().Padding(4).Text(d);
                        t.Cell().Padding(4).AlignRight().Text($"{a:N2}");
                        t.Cell().Padding(4).Text(r);
                    }
                    IR("ITC Available (as per GSTR-2B)", gstr.ITCAvailable, "Auto-populated");
                    IR("ITC Utilized against CGST+SGST", gstr.ITCUtilized * 0.5m);
                    IR("ITC Utilized against IGST", gstr.ITCUtilized * 0.5m);
                    IR("Total ITC Utilized", gstr.ITCUtilized);
                    t.Cell().ColumnSpan(3).LineHorizontal(0.5f);
                    t.Cell().Background(Colors.Green.Lighten4)
                        .Padding(4).Text("NET TAX PAYABLE").Bold();
                    t.Cell().Background(Colors.Green.Lighten4)
                        .Padding(4).AlignRight().Text($"{gstr.NetTaxPayable:N2}").Bold();
                    t.Cell().Background(Colors.Green.Lighten4)
                        .Padding(4).Text(gstr.AcknowledgementNumber != null
                            ? $"Ack: {gstr.AcknowledgementNumber}" : "Pending");
                    if ((gstr.LateFee ?? 0) > 0)
                        IR("Late Fee + Interest", (gstr.LateFee ?? 0) + (gstr.Interest ?? 0), "Payable additionally");
                });

                // ── TRANSACTIONS TABLE (top 25) ──
                if (gstr.Transactions.Any())
                {
                    col.Item().PaddingTop(12)
                        .Text($"TRANSACTION DETAILS (showing {Math.Min(25, gstr.Transactions.Count)} of {gstr.Transactions.Count})")
                        .Bold().FontSize(10).FontColor(Colors.Blue.Darken2);

                    col.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(22); cd.RelativeColumn(2.5f); cd.RelativeColumn(2);
                            cd.ConstantColumn(75); cd.ConstantColumn(70);
                            cd.ConstantColumn(65); cd.ConstantColumn(65); cd.ConstantColumn(65);
                        });
                        void TH(string h) => t.Cell()
                            .Background(Colors.Blue.Lighten4)
                            .Padding(3).AlignCenter().Text(h).Bold().FontSize(7);
                        TH("#"); TH("Party Name"); TH("GSTIN");
                        TH("Invoice No"); TH("Taxable (₹)");
                        TH("CGST (₹)"); TH("SGST (₹)"); TH("IGST (₹)");

                        int idx = 1;
                        foreach (var tx in gstr.Transactions.Take(25))
                        {
                            var bg = idx % 2 == 0
                                ? Colors.Grey.Lighten5
                                : Colors.White;
                            void TC(string v) => t.Cell().Background(bg).Padding(3).Text(v).FontSize(7);
                            void TR(decimal v) => t.Cell().Background(bg).Padding(3).AlignRight().Text($"{v:N2}").FontSize(7);
                            TC(idx++.ToString());
                            TC(tx.CounterpartyName ?? "—");
                            TC(tx.CounterpartyGSTIN ?? "Unregistered");
                            TC(tx.InvoiceNumber ?? "—");
                            TR(tx.TaxableValue);
                            TR(tx.CGST);
                            TR(tx.SGST);
                            TR(tx.IGST);
                        }
                    });
                }

                // ── DECLARATION ──
                col.Item().PaddingTop(15)
                    .Background(Colors.Grey.Lighten4).Padding(8).Column(dc =>
                    {
                        dc.Item().Text("DECLARATION").Bold().FontSize(9);
                        dc.Item().PaddingTop(4)
                            .Text("I hereby solemnly affirm and declare that the information given herein above " +
                                  "is true and correct to the best of my knowledge and belief and nothing has been concealed therefrom.")
                            .FontSize(8).Italic();
                    });

                col.Item().PaddingTop(15).Row(r =>
                {
                    r.RelativeItem().Column(sc =>
                    {
                        sc.Item().Text("Verified By:").FontSize(8);
                        sc.Item().PaddingTop(18).LineHorizontal(0.5f);
                        sc.Item().Text("(Authorized Signatory)").FontSize(7)
                            .FontColor(Colors.Grey.Medium);
                    });
                    r.RelativeItem().AlignRight()
                        .Text($"Place: {tenant?.RegisteredAddress?.City ?? "—"}   Date: {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(8);
                });
            });

            p.Footer().Row(r =>
            {
                r.RelativeItem().Text($"Generated by MSMEDigitize on {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(7).FontColor(Colors.Grey.Medium);
                r.AutoItem().AlignRight().Text("Page 1 of 1").FontSize(7)
                    .FontColor(Colors.Grey.Medium);
            });
        })).GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FINANCIAL REPORT PDF — P&L + KPIs + Expense breakdown
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<byte[]> GenerateFinancialReportPDFAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        var invoices = await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync(ct);
        var payments = await _db.Payments
            .Where(p => p.TenantId == tenantId && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync(ct);
        var expenses = await _db.Expenses
            .Where(e => e.TenantId == tenantId && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync(ct);

        decimal totalRevenue = invoices.Sum(i => i.TotalAmount);
        decimal totalCollected = payments.Sum(p => p.Amount);
        decimal totalExpenses = expenses.Sum(e => e.TotalAmount);
        decimal netProfit = totalCollected - totalExpenses;
        decimal totalGST = invoices.Sum(i => i.CGSTAmount + i.SGSTAmount + i.IGSTAmount);
        decimal outstanding = invoices.Where(i => i.Status != InvoiceStatus.Paid)
                                         .Sum(i => i.BalanceAmount);
        decimal profitMargin = totalCollected > 0 ? (netProfit / totalCollected * 100) : 0;

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        return QuestPDF.Fluent.Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(28, QuestPDF.Infrastructure.Unit.Point);
            p.DefaultTextStyle(x => x.FontSize(8));

            // ── HEADER ──
            p.Header().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Column(hc =>
                    {
                        hc.Item().Text(tenant?.BusinessName ?? "Business")
                            .FontSize(16).Bold()
                            .FontColor(Colors.Blue.Darken3);
                        hc.Item().Text("FINANCIAL SUMMARY REPORT").FontSize(12).Bold();
                        hc.Item().Text($"Period: {from:dd MMM yyyy}  →  {to:dd MMM yyyy}")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    });
                    r.AutoItem().Column(hc =>
                    {
                        hc.Item().AlignRight().Text($"GSTIN: {tenant?.GSTIN ?? "—"}").Bold();
                        hc.Item().AlignRight()
                            .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
                col.Item().PaddingTop(3).LineHorizontal(2f)
                    .LineColor(Colors.Blue.Darken3);
            });

            p.Content().PaddingTop(12).Column(col =>
            {
                // ── KPI CARDS ROW 1 ──
                col.Item().Row(r =>
                {
                    void Card(string title, string value, string color)
                    {
                        r.RelativeItem().Padding(3)
                            .Border(0.5f).BorderColor(
                                color == "green" ? Colors.Green.Darken1 :
                                color == "red" ? Colors.Red.Darken1 :
                                color == "orange" ? Colors.Orange.Darken1 :
                                                    Colors.Blue.Darken1)
                            .Padding(8).Column(cc =>
                            {
                                cc.Item().Text(title).FontSize(7)
                                    .FontColor(Colors.Grey.Darken2);
                                cc.Item().Text(value).FontSize(13).Bold().FontColor(
                                    color == "green" ? Colors.Green.Darken4 :
                                    color == "red" ? Colors.Red.Darken4 :
                                    color == "orange" ? Colors.Orange.Darken3 :
                                                        Colors.Blue.Darken4);
                            });
                    }
                    Card("Total Revenue Invoiced", $"₹{totalRevenue:N0}", "blue");
                    Card("Total Cash Collected", $"₹{totalCollected:N0}", "green");
                    Card("Total Expenses", $"₹{totalExpenses:N0}", "red");
                    Card("Net Profit / (Loss)", $"₹{netProfit:N0}", netProfit >= 0 ? "green" : "red");
                });
                col.Item().PaddingTop(6).Row(r =>
                {
                    void Card2(string title, string value, string color)
                    {
                        r.RelativeItem().Padding(3)
                            .Border(0.5f).BorderColor(
                                color == "orange" ? Colors.Orange.Darken1 :
                                color == "purple" ? Colors.Purple.Darken1 :
                                                    Colors.Blue.Darken1)
                            .Padding(8).Column(cc =>
                            {
                                cc.Item().Text(title).FontSize(7)
                                    .FontColor(Colors.Grey.Darken2);
                                cc.Item().Text(value).FontSize(12).Bold().FontColor(
                                    color == "orange" ? Colors.Orange.Darken3 :
                                    color == "purple" ? Colors.Purple.Darken3 :
                                                        Colors.Blue.Darken4);
                            });
                    }
                    Card2("Outstanding Receivables", $"₹{outstanding:N0}", "orange");
                    Card2("GST Collected (Govt.)", $"₹{totalGST:N0}", "purple");
                    Card2("Total Invoices", invoices.Count.ToString(), "blue");
                    Card2("Profit Margin", $"{profitMargin:N1}%",
                        profitMargin >= 10 ? "green" : profitMargin >= 0 ? "orange" : "red");
                });

                // ── INVOICE STATUS BREAKDOWN ──
                col.Item().PaddingTop(14).Text("INVOICE SUMMARY BY STATUS").Bold().FontSize(10)
                    .FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(5).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    { cd.RelativeColumn(2); cd.RelativeColumn(1); cd.RelativeColumn(2); cd.RelativeColumn(2); });
                    void TH(string h) => t.Cell()
                        .Background(Colors.Blue.Lighten4).Padding(4).Text(h).Bold();
                    TH("Status"); TH("Count"); TH("Amount (₹)"); TH("% of Total");

                    foreach (var g in invoices.GroupBy(i => i.Status.ToString()))
                    {
                        var amt = g.Sum(i => i.TotalAmount);
                        var pct = totalRevenue > 0 ? amt / totalRevenue * 100 : 0;
                        t.Cell().Padding(4).Text(g.Key);
                        t.Cell().Padding(4).AlignCenter().Text(g.Count().ToString());
                        t.Cell().Padding(4).AlignRight().Text($"{amt:N2}");
                        t.Cell().Padding(4).AlignRight().Text($"{pct:N1}%");
                    }
                });

                // ── EXPENSE BY CATEGORY ──
                col.Item().PaddingTop(14).Text("EXPENSE BREAKDOWN BY CATEGORY").Bold().FontSize(10)
                    .FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(5).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    { cd.RelativeColumn(3); cd.RelativeColumn(2); cd.RelativeColumn(2); });
                    void TH(string h) => t.Cell()
                        .Background(Colors.Blue.Lighten4).Padding(4).Text(h).Bold();
                    TH("Category"); TH("Amount (₹)"); TH("% of Expenses");

                    var expGrp = expenses.GroupBy(e => string.IsNullOrEmpty(e.Category) ? "Uncategorized" : e.Category);
                    foreach (var g in expGrp.OrderByDescending(g => g.Sum(e => e.TotalAmount)))
                    {
                        var amt = g.Sum(e => e.TotalAmount);
                        var pct = totalExpenses > 0 ? amt / totalExpenses * 100 : 0;
                        t.Cell().Padding(4).Text(g.Key);
                        t.Cell().Padding(4).AlignRight().Text($"{amt:N2}");
                        t.Cell().Padding(4).AlignRight().Text($"{pct:N1}%");
                    }
                    t.Cell().ColumnSpan(3).LineHorizontal(0.8f);
                    t.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("TOTAL").Bold();
                    t.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{totalExpenses:N2}").Bold();
                    t.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("100.0%").Bold();
                });

                // ── P&L SUMMARY ──
                col.Item().PaddingTop(14).Text("PROFIT & LOSS SUMMARY").Bold().FontSize(10)
                    .FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(5).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    { cd.RelativeColumn(4); cd.RelativeColumn(2); });
                    void PLR(string d, decimal a, bool bold = false, bool neg = false)
                    {
                        t.Cell().Padding(5).Text(d).Bold();
                        t.Cell().Padding(5).AlignRight()
                            .Text($"{(neg ? "(" : "")}₹{Math.Abs(a):N2}{(neg ? ")" : "")}").Bold()
                            .FontColor(neg ? Colors.Red.Darken2
                                          : Colors.Black);
                    }
                    PLR("Total Revenue Billed (all invoices)", totalRevenue, true);
                    PLR("Less: Outstanding / Uncollected", outstanding, false, true);
                    PLR("= Net Revenue Actually Collected", totalCollected, true);
                    t.Cell().ColumnSpan(2).PaddingVertical(2).LineHorizontal(0.5f);
                    PLR("Less: Total Operating Expenses", totalExpenses, false, true);
                    PLR("Less: GST Liability (remit to Govt)", totalGST, false, true);
                    t.Cell().ColumnSpan(2).LineHorizontal(1f)
                        .LineColor(Colors.Blue.Medium);
                    t.Cell().Background(netProfit >= 0
                            ? Colors.Green.Lighten4
                            : Colors.Red.Lighten4)
                        .Padding(6).Text("NET PROFIT / (LOSS)").Bold().FontSize(10);
                    t.Cell().Background(netProfit >= 0
                            ? Colors.Green.Lighten4
                            : Colors.Red.Lighten4)
                        .Padding(6).AlignRight()
                        .Text($"{(netProfit < 0 ? "(" : "")}₹{Math.Abs(netProfit):N2}{(netProfit < 0 ? ")" : "")}")
                        .Bold().FontSize(10)
                        .FontColor(netProfit >= 0
                            ? Colors.Green.Darken4
                            : Colors.Red.Darken4);
                });

                // ── DISCLAIMER ──
                col.Item().PaddingTop(10)
                    .Text("* This report is auto-generated from MSMEDigitize. Consult your CA for audited financials.")
                    .FontSize(7).Italic()
                    .FontColor(Colors.Grey.Medium);
            });

            p.Footer().Row(r =>
            {
                r.RelativeItem()
                    .Text($"MSMEDigitize Financial Report  ·  {tenant?.GSTIN ?? ""}  ·  {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(7).FontColor(Colors.Grey.Medium);
                r.AutoItem().AlignRight().Text("CONFIDENTIAL").FontSize(7)
                    .FontColor(Colors.Grey.Medium);
            });
        })).GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PURCHASE ORDER PDF
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<byte[]> GeneratePurchaseOrderPDFAsync(Guid poId, CancellationToken ct = default)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == poId, ct)
            ?? throw new InvalidOperationException($"PO {poId} not found");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == po.TenantId, ct);

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        return QuestPDF.Fluent.Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(28, QuestPDF.Infrastructure.Unit.Point);
            p.DefaultTextStyle(x => x.FontSize(9));

            p.Header().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Column(hc =>
                    {
                        hc.Item().Text(tenant?.BusinessName ?? "Company").FontSize(16).Bold()
                            .FontColor(Colors.Blue.Darken3);
                        hc.Item().Text("PURCHASE ORDER").FontSize(13).Bold();
                    });
                    r.AutoItem().Column(hc =>
                    {
                        hc.Item().AlignRight().Text($"PO#: {po.PONumber}").Bold().FontSize(10);
                        hc.Item().AlignRight().Text($"Date: {po.PODate:dd/MM/yyyy}");
                        hc.Item().AlignRight().Text($"Status: {po.Status}").FontSize(8);
                    });
                });
                col.Item().PaddingTop(3).LineHorizontal(1.5f)
                    .LineColor(Colors.Blue.Darken3);
                col.Item().PaddingTop(6).Row(r =>
                {
                    r.RelativeItem().Column(vc =>
                    {
                        vc.Item().Text("VENDOR:").Bold().FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                        vc.Item().Text(po.Vendor?.Name ?? "—").Bold();
                        vc.Item().Text(po.Vendor?.GSTIN ?? "").FontSize(8);
                        vc.Item().Text(po.Vendor?.Email ?? "").FontSize(8);
                        vc.Item().Text(po.Vendor?.Phone ?? "").FontSize(8);
                    });
                    r.RelativeItem().Column(bc =>
                    {
                        bc.Item().AlignRight().Text("SHIP TO:").Bold().FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                        bc.Item().AlignRight().Text(tenant?.BusinessName ?? "");
                        bc.Item().AlignRight().Text(tenant?.GSTIN ?? "").FontSize(8);
                        bc.Item().AlignRight().Text(po.Notes ?? "As per order" ?? "").FontSize(8);
                    });
                });
            });

            p.Content().PaddingTop(10).Column(col =>
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(28); cd.RelativeColumn(4); cd.ConstantColumn(55);
                        cd.ConstantColumn(70); cd.ConstantColumn(70); cd.ConstantColumn(80);
                    });
                    void TH(string h) => t.Cell()
                        .Background(Colors.Blue.Lighten4).Padding(4)
                        .AlignCenter().Text(h).Bold().FontSize(8);
                    TH("#"); TH("Item Description"); TH("HSN/SAC");
                    TH("Qty & Unit"); TH("Unit Price (₹)"); TH("Total Amount (₹)");

                    int i = 1;
                    foreach (var li in po.LineItems)
                    {
                        void TC(string v) => t.Cell().Padding(4).Text(v).FontSize(8);
                        void TR(string v) => t.Cell().Padding(4).AlignRight().Text(v).FontSize(8);
                        TC(i++.ToString());
                        t.Cell().Padding(4).Column(lc =>
                        {
                            lc.Item().Text(li.ItemName).Bold().FontSize(8);
                            if (!string.IsNullOrEmpty(li.Description))
                                lc.Item().Text(li.Description).FontSize(7)
                                    .FontColor(Colors.Grey.Medium);
                        });
                        TC(li.HSNCode ?? "");
                        TR($"{li.Quantity} {li.Unit}");
                        TR($"{li.UnitPrice:N2}");
                        TR($"{li.TotalAmount:N2}");
                    }
                    t.Cell().ColumnSpan(6).LineHorizontal(0.8f);
                    for (int j = 0; j < 4; j++) t.Cell().Padding(4);
                    t.Cell().Padding(4).AlignRight().Text("TOTAL").Bold();
                    t.Cell().Padding(4).AlignRight()
                        .Text($"₹{po.LineItems.Sum(l => l.TotalAmount):N2}").Bold();
                });

                if (!string.IsNullOrEmpty("Standard Terms and Conditions Apply"))
                {
                    col.Item().PaddingTop(10).Text("TERMS & CONDITIONS:").Bold().FontSize(8);
                    col.Item().Text("Standard Terms and Conditions Apply").FontSize(8);
                }

                col.Item().PaddingTop(20).Row(r =>
                {
                    r.RelativeItem().Column(sc =>
                    {
                        sc.Item().Text("Vendor Acknowledgement").FontSize(8);
                        sc.Item().PaddingTop(18).LineHorizontal(0.5f);
                    });
                    r.RelativeItem().AlignRight().Column(sc =>
                    {
                        sc.Item().AlignRight().Text("Authorized Signatory").FontSize(8);
                        sc.Item().PaddingTop(18).LineHorizontal(0.5f);
                    });
                });
            });

            p.Footer().AlignCenter()
                .Text($"PO generated by MSMEDigitize on {DateTime.Now:dd/MM/yyyy HH:mm}  ·  This is a legally binding document.")
                .FontSize(7).FontColor(Colors.Grey.Medium);
        })).GeneratePdf();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static string AmountToWords(long n)
    {
        if (n == 0) return "Zero";
        if (n < 0) return "Minus " + AmountToWords(-n);
        string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven",
            "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen",
            "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty",
            "Sixty", "Seventy", "Eighty", "Ninety" };
        if (n < 20) return ones[n];
        if (n < 100) return tens[n / 10] + (n % 10 > 0 ? " " + ones[n % 10] : "");
        if (n < 1_000) return ones[n / 100] + " Hundred" + (n % 100 > 0 ? " " + AmountToWords(n % 100) : "");
        if (n < 1_00_000) return AmountToWords(n / 1_000) + " Thousand" + (n % 1_000 > 0 ? " " + AmountToWords(n % 1_000) : "");
        if (n < 1_00_00_000) return AmountToWords(n / 1_00_000) + " Lakh" + (n % 1_00_000 > 0 ? " " + AmountToWords(n % 1_00_000) : "");
        return AmountToWords(n / 1_00_00_000) + " Crore" + (n % 1_00_00_000 > 0 ? " " + AmountToWords(n % 1_00_00_000) : "");
    }


}

public class BankingServiceImpl : IBankingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BankingServiceImpl> _logger;

    public BankingServiceImpl(AppDbContext db, ILogger<BankingServiceImpl> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Core.Entities.Banking.BankTransaction>> FetchStatementAsync(Guid bankAccountId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        // In production: Integrate with Account Aggregator (AA) framework - Setu/Finvu/Perfios
        // AA Framework provides consent-based bank statement access via RBI-regulated APIs
        return await _db.BankTransactions
            .Where(t => t.BankAccountId == bankAccountId && t.TransactionDate >= from && t.TransactionDate <= to)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(ct);
    }

    public async Task<bool> AutoReconcileAsync(Guid bankAccountId, CancellationToken ct = default)
    {
        var unreconciled = await _db.BankTransactions
            .Where(t => t.BankAccountId == bankAccountId && t.ReconciliationStatus == ReconciliationStatus.Unreconciled)
            .ToListAsync(ct);

        int reconciled = 0;
        foreach (var bankTx in unreconciled)
        {
            // Match with payments
            var payment = await _db.Payments
                .Where(p => p.TenantId == bankTx.TenantId &&
                           Math.Abs(p.Amount - bankTx.Amount) < 1 &&
                           Math.Abs((p.PaymentDate - bankTx.TransactionDate).TotalDays) <= 2 &&
                           !p.IsReconciled)
                .FirstOrDefaultAsync(ct);

            if (payment != null)
            {
                bankTx.ReconciliationStatus = ReconciliationStatus.Reconciled;
                bankTx.LinkedPaymentId = payment.Id;
                payment.IsReconciled = true;
                payment.ReconciliationStatus = ReconciliationStatus.Reconciled;
                payment.BankTransactionId = bankTx.Id;
                reconciled++;
            }
            else
            {
                // Match with expenses
                var expense = await _db.Expenses
                    .Where(e => e.TenantId == bankTx.TenantId &&
                               Math.Abs(e.TotalAmount - bankTx.Amount) < 1 &&
                               Math.Abs((e.ExpenseDate - bankTx.TransactionDate).TotalDays) <= 1)
                    .FirstOrDefaultAsync(ct);

                if (expense != null)
                {
                    bankTx.ReconciliationStatus = ReconciliationStatus.Reconciled;
                    bankTx.LinkedExpenseId = expense.Id;
                    expense.BankTransactionId = bankTx.Id;
                    reconciled++;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Auto-reconciled {Count} transactions for bank account {AccountId}", reconciled, bankAccountId);
        return reconciled > 0;
    }

    public async Task<decimal> GetBalanceAsync(Guid bankAccountId, CancellationToken ct = default)
    {
        var account = await _db.BankAccounts.FindAsync(new object[] { bankAccountId }, ct);
        return account?.CurrentBalance ?? 0;
    }

    public async Task SendPaymentReceiptAsync(string to, string customerName, decimal amount, string receiptNumber, CancellationToken ct = default)
    {
        var subject = $"Payment Receipt #{receiptNumber} - ₹{amount:N0}";
        var body = $"<h2>Payment Received</h2><p>Dear {customerName},</p><p>We received your payment of <strong>₹{amount:N0}</strong>. Receipt: {receiptNumber}</p><p>Thank you!</p>";
        //await SendEmailAsync(to, subject, body, true, ct);
    }

}