using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
using MSMEDigitize.Web.ViewModels;

namespace MSMEDigitize.Web.Controllers;

[Authorize]
public class InvoiceController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public InvoiceController(IInvoiceService svc, IUnitOfWork uow, ICurrentUserService cu)
    {
        _invoiceService = svc; _uow = uow; _currentUser = cu;
    }

    private Guid TenantId => _currentUser.TenantId ?? throw new UnauthorizedAccessException();

    public async Task<IActionResult> Index(int page = 1, string? search = null, InvoiceStatus? status = null)
    {
        var result = await _invoiceService.GetInvoicesAsync(TenantId, page, 20, search, status);
        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(result);
    }

    public async Task<IActionResult> Create()
    {
        var customers = await _uow.Customers.FindAsync(c => c.TenantId == TenantId && c.IsActive);
        var products = await _uow.Products.FindAsync(p => p.TenantId == TenantId && p.IsActive);
        var vm = new InvoiceViewModel
        {
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            Customers = customers.Select(c => new CustomerDropdown(c.Id, c.Name, c.GSTIN ?? "", c.BillingAddress?.State ?? "", c.Email ?? "", c.Phone, 30)).ToList(),
            Products = products.Select(p => new ProductDropdown(p.Id, p.Name, p.SellingPrice, p.GSTRate, p.HSNCode ?? "", p.Unit.ToString())).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var dto = new MSMEDigitize.Application.DTOs.CreateInvoiceDto
        {
            CustomerId = model.CustomerId ?? Guid.Empty,
            Type = model.Type,
            InvoiceDate = model.InvoiceDate,
            DueDate = model.DueDate ?? model.InvoiceDate.AddDays(30),
            IsInterState = model.IsInterState,
            Notes = model.Notes,
            PoNumber = model.PoNumber,
            TermsAndConditions = model.TermsAndConditions,
            LineItems = model.LineItems.Select(li => new MSMEDigitize.Application.DTOs.CreateInvoiceLineItemDto
            {
                ProductId = li.ProductId,
                ItemName = li.Description,
                Description = li.Description,
                HSNSACCode = li.HSNSACCode,
                Quantity = li.Quantity,
                Unit = li.Unit,
                UnitPrice = li.UnitPrice,
                DiscountPercent = li.DiscountAmount,
                GSTRate = li.GSTRate,
                CessRate = li.CessRate,
            }).ToList()
        };

        var result = await _invoiceService.CreateInvoiceAsync(TenantId, dto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Error ?? "Failed to create invoice.");
            return View(model);
        }

        var inv = result.Value as Invoice;
        TempData["Success"] = $"Invoice {inv?.InvoiceNumber ?? ""} created successfully!";
        return inv != null
            ? RedirectToAction("Details", new { id = inv.Id })
            : RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var invoice = await _invoiceService.GetInvoiceAsync(id, TenantId);
        if (invoice == null) return NotFound();
        return View(invoice);
    }

    [HttpPost]
    public async Task<IActionResult> Send(Guid id)
    {
        var sendResult = await _invoiceService.SendInvoiceAsync(id, TenantId);
        TempData[sendResult.IsSuccess ? "Success" : "Error"] = sendResult.IsSuccess ? "Invoice sent!" : "Failed to send.";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RecordPayment(Guid id, decimal amount, PaymentMethod method, string? reference)
    {
        var dto = new MSMEDigitize.Application.DTOs.RecordPaymentDto { InvoiceId = id, Amount = amount, Mode = (MSMEDigitize.Core.Enums.PaymentMode)(int)method, PaymentDate = DateTime.UtcNow, TransactionId = reference };
        var result = await _invoiceService.RecordPaymentAsync(TenantId, dto);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? $"Payment ₹{amount:N0} recorded!" : "Payment failed.";
        return RedirectToAction("Details", new { id });
    }

    public async Task<IActionResult> Download(Guid id)
    {
        var pdf = await _invoiceService.GeneratePDFAsync(id, TenantId);
        var invoice = await _invoiceService.GetInvoiceAsync(id, TenantId);
        var inv = invoice as MSMEDigitize.Core.Entities.Invoicing.Invoice;
        return File(pdf, "application/pdf", $"Invoice-{inv?.InvoiceNumber}.pdf");
    }

    public async Task<IActionResult> GSTReport(int month, int year)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        var summary = await _invoiceService.GetGSTSummaryAsync(TenantId, month, year);
        return View(summary);
    }
}