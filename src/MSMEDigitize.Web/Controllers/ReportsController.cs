using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSMEDigitize.Core.Entities;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IInvoiceService _invoiceService;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(IUnitOfWork uow, IInvoiceService inv, ICurrentUserService cu)
    { _uow = uow; _invoiceService = inv; _currentUser = cu; }
    private Guid TenantId => _currentUser.TenantId!.Value;

    public IActionResult Index() => View();

    public async Task<IActionResult> SalesReport(DateTime? from, DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.AddMonths(-1);
        var end = to ?? DateTime.UtcNow;
        var invoices = await _uow.Invoices.Query()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId && i.InvoiceDate >= start && i.InvoiceDate <= end)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
        ViewBag.From = start; ViewBag.To = end;
        return View(invoices);
    }

    public async Task<IActionResult> GSTReport(int month, int year)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        var summary = await _invoiceService.GetGSTSummaryAsync(TenantId, month, year);
        return View(summary);
    }

    public async Task<IActionResult> StockReport()
    {
        var products = await _uow.Products.Query()
            .Include(p => p.Category)
            .Where(p => p.TenantId == TenantId && p.IsActive && p.TrackInventory)
            .OrderBy(p => p.Name)
            .ToListAsync();
        return View(products);
    }

    public async Task<IActionResult> ProfitLoss(int month, int year)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var revenue = await _uow.Invoices.Query()
            .Where(i => i.TenantId == TenantId && i.InvoiceDate >= start && i.InvoiceDate < end)
            .SumAsync(i => i.TaxableAmount);
        var ledgers = await _uow.LedgerEntries.Query()
            .Include(l => l.Account)
            .Where(l => l.TenantId == TenantId && l.EntryDate >= start && l.EntryDate < end
                && (l.Account.AccountType == AccountType.Expense || l.Account.AccountType == AccountType.Income))
            .ToListAsync();
        ViewBag.Revenue = revenue; ViewBag.Month = month; ViewBag.Year = year;
        return View(ledgers);
    }

    public async Task<IActionResult> ExportSalesExcel(DateTime? from, DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.AddMonths(-1);
        var end = to ?? DateTime.UtcNow;
        var invoices = await _uow.Invoices.Query()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId && i.InvoiceDate >= start && i.InvoiceDate <= end)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales Report");
        ws.Cell(1, 1).Value = "Invoice No"; ws.Cell(1, 2).Value = "Date";
        ws.Cell(1, 3).Value = "Customer"; ws.Cell(1, 4).Value = "Sub Total";
        ws.Cell(1, 5).Value = "GST"; ws.Cell(1, 6).Value = "Total"; ws.Cell(1, 7).Value = "Status";
        ws.Row(1).Style.Font.Bold = true;

        for (int r = 0; r < invoices.Count; r++)
        {
            var inv = invoices[r];
            ws.Cell(r + 2, 1).Value = inv.InvoiceNumber;
            ws.Cell(r + 2, 2).Value = inv.InvoiceDate.ToString("dd/MM/yyyy");
            ws.Cell(r + 2, 3).Value = inv.Customer.Name;
            ws.Cell(r + 2, 4).Value = (double)inv.SubTotal;
            ws.Cell(r + 2, 5).Value = (double)(inv.CGSTAmount + inv.SGSTAmount + inv.IGSTAmount);
            ws.Cell(r + 2, 6).Value = (double)inv.TotalAmount;
            ws.Cell(r + 2, 7).Value = inv.Status.ToString();
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"SalesReport_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx");
    }

    // ─── Report Actions (view links from Index) ──────────────────────────────

    public async Task<IActionResult> Sales(DateTime? from, DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.AddMonths(-1);
        var end = to ?? DateTime.UtcNow;
        var invoices = await _uow.Invoices.Query()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId && i.InvoiceDate >= start && i.InvoiceDate <= end)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
        ViewBag.From = start; ViewBag.To = end;
        return View("SalesReport", invoices);
    }

    public async Task<IActionResult> SalesExcel(DateTime? from, DateTime? to) => await ExportSalesExcel(from, to);

    public IActionResult Ledger() => View();
    public IActionResult CashFlow() => View();
    public IActionResult CashFlowForecast() => View();

    public async Task<IActionResult> GSTR1(int month = 0, int year = 0)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        var summary = await _invoiceService.GetGSTSummaryAsync(TenantId, month, year);
        ViewBag.Month = month; ViewBag.Year = year;
        return View(summary);
    }

    public async Task<IActionResult> GSTR3B(int month = 0, int year = 0)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        var summary = await _invoiceService.GetGSTSummaryAsync(TenantId, month, year);
        ViewBag.Month = month; ViewBag.Year = year;
        return View(summary);
    }

    public IActionResult ITCReconciliation() => View();
    public IActionResult HSNSummary() => View();
    public IActionResult GSTFilingHistory() => View();
    public IActionResult GSTPayables() => View();

    public async Task<IActionResult> StockStatus()
        => await StockReport();

    public async Task<IActionResult> StockMovement()
    {
        var movements = await _uow.StockMovements.Query()
            .Include(m => m.Product)
            .Where(m => m.TenantId == TenantId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(500)
            .ToListAsync();
        return View(movements);
    }

    public IActionResult PayrollSummary(int month = 0, int year = 0)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        ViewBag.Month = month; ViewBag.Year = year;
        return View();
    }

    public async Task<IActionResult> Attendance(int month = 0, int year = 0)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        ViewBag.Month = month; ViewBag.Year = year;
        var records = await _uow.Attendances.Query()
            .Where(a => a.TenantId == TenantId && a.Date.Month == month && a.Date.Year == year)
            .ToListAsync();
        // Load employee names separately since Attendance has no nav property
        var empIds = records.Select(a => a.EmployeeId).Distinct().ToList();
        var employees = await _uow.Employees.Query()
            .Where(e => empIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FullName);
        ViewBag.EmployeeNames = employees;
        return View(records);
    }

    public async Task<IActionResult> AccountsPayable()
    {
        var vendors = await _uow.Vendors.Query()
            .Where(v => v.TenantId == TenantId && v.IsActive && v.CurrentOutstanding > 0)
            .OrderByDescending(v => v.CurrentOutstanding)
            .ToListAsync();
        return View(vendors);
    }

    public IActionResult TDSReport() => View();

    // ─── PDF Download Actions ────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> DownloadPayslipPdf(Guid id, [FromServices] IPDFService pdfService)
    {
        try
        {
            var bytes = await pdfService.GeneratePayslipPDFAsync(id);
            if (bytes == null || bytes.Length == 0)
                return NotFound("Payslip not found or PDF generation failed.");
            return File(bytes, "application/pdf", $"Payslip_{id:N}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not generate payslip PDF: {ex.Message}";
            return RedirectToAction("Index", "Employee");
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadGSTReturnPdf(Guid id, [FromServices] IPDFService pdfService)
    {
        try
        {
            var bytes = await pdfService.GenerateGSTReturnPDFAsync(id);
            if (bytes == null || bytes.Length == 0)
                return NotFound("GST Return not found or PDF generation failed.");
            return File(bytes, "application/pdf", $"GSTReturn_{id:N}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not generate GST Return PDF: {ex.Message}";
            return RedirectToAction("Index", "Reports");
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadFinancialPdf(DateTime? from, DateTime? to, [FromServices] IPDFService pdfService)
    {
        var start = from ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var end = to ?? DateTime.Now;
        try
        {
            var bytes = await pdfService.GenerateFinancialReportPDFAsync(TenantId, start, end);
            if (bytes == null || bytes.Length == 0)
                return NotFound("No data found for the selected period.");
            return File(bytes, "application/pdf", $"FinancialReport_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not generate Financial Report PDF: {ex.Message}";
            return RedirectToAction("Index", "Reports");
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id, [FromServices] IPDFService pdfService)
    {
        try
        {
            var bytes = await pdfService.GenerateInvoicePDFAsync(id);
            if (bytes == null || bytes.Length == 0)
                return NotFound("Invoice not found or PDF generation failed.");
            return File(bytes, "application/pdf", $"Invoice_{id:N}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not generate Invoice PDF: {ex.Message}";
            return RedirectToAction("Index", "Invoice");
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPurchaseOrderPdf(Guid id, [FromServices] IPDFService pdfService)
    {
        try
        {
            var bytes = await pdfService.GeneratePurchaseOrderPDFAsync(id);
            if (bytes == null || bytes.Length == 0)
                return NotFound("Purchase Order not found or PDF generation failed.");
            return File(bytes, "application/pdf", $"PurchaseOrder_{id:N}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not generate Purchase Order PDF: {ex.Message}";
            return RedirectToAction("Index", "PurchaseOrder");
        }
    }
}