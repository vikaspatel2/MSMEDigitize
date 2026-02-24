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
        ViewBag.Customers = customers;
        ViewBag.Products = products;
        return View(new InvoiceViewModel { InvoiceDate = DateTime.Today, DueDate = DateTime.Today.AddDays(30) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var invoice = new Invoice
        {
            TenantId = TenantId,
            //CustomerId = model.CustomerId,
            //InvoiceDate = model.InvoiceDate,
            //DueDate = model.DueDate,
            //Notes = model.Notes,
            //TermsAndConditions = model.TermsAndConditions,
            //PlaceOfSupply = model.PlaceOfSupply,
            //IsInterState = model.IsInterState,
            //DiscountAmount = model.DiscountAmount,
            Type = model.Type,
            Status = InvoiceStatus.Draft
        };

        //var lineItems = model.LineItems.Select((li, idx) => new InvoiceLineItem
        //{
        //    ProductId = li.ProductId,
        //    Description = li.Description,
        //    HSNSACCode = li.HSNSACCode,
        //    Quantity = li.Quantity,
        //    Unit = li.Unit,
        //    UnitPrice = li.UnitPrice,
        //    DiscountPercent = li.DiscountPercent,
        //    GSTRate = li.GSTRate,
        //    CessRate = li.CessRate,
        //    SortOrder = idx
        //}).ToList();

        // var created = await _invoiceService.CreateInvoiceAsync(invoice, lineItems);
        //TempData["Success"] = $"Invoice {created.InvoiceNumber} created successfully!";
        return RedirectToAction("Details");//, new { id = created.Id });
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
        var result = await _invoiceService.SendInvoiceAsync(id, TenantId);
        //TempData[result ? "Success" : "Error"] = result ? "Invoice sent!" : "Failed to send.";
        return RedirectToAction("Details", new { id });
    }

    //[HttpPost]
    //public async Task<IActionResult> RecordPayment(Guid id, decimal amount, PaymentMethod method, string? reference)
    //{
    //    var result = await _invoiceService.RecordPaymentAsync(id, amount, method, reference);
    //    TempData[result ? "Success" : "Error"] = result ? $"Payment ₹{amount:N0} recorded!" : "Payment failed.";
    //    return RedirectToAction("Details", new { id });
    //}
    [HttpPost]
    public async Task<IActionResult> RecordPayment(
    Guid id,
    decimal amount,
    CancellationToken ct)
    {
        var result = await _invoiceService.RecordPaymentAsync(id, amount, ct);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Payment ₹{amount:N0} recorded!"
                : result.Error;

        return RedirectToAction("Details", new { id });
    }

    public async Task<IActionResult> Download(Guid id)
    {
        var pdf = await _invoiceService.GeneratePDFAsync(id, TenantId);
        var invoice = await _invoiceService.GetInvoiceAsync(id, TenantId);
        return File(pdf, "application/pdf");//, $"Invoice-{invoice?.InvoiceNumber}.pdf");
    }

    public async Task<IActionResult> GSTReport(int month, int year)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        var summary = await _invoiceService.GetGSTSummaryAsync(TenantId, month, year);
        return View(summary);
    }
}