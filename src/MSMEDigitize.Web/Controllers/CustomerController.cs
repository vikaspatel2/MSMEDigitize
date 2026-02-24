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
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Web.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CustomerController(IUnitOfWork uow, ICurrentUserService cu) { _uow = uow; _currentUser = cu; }
    private Guid TenantId => _currentUser.TenantId!.Value;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = _uow.Customers.Query().Where(c => c.TenantId == TenantId);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || c.Phone.Contains(search) || (c.Email != null && c.Email.Contains(search)));
        var customers = query.OrderBy(c => c.Name).Skip((page - 1) * 20).Take(20).ToList();
        ViewBag.Search = search;
        return View(customers);
    }

    [HttpGet] public IActionResult Create() => View(new Customer());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model)
    {
        if (!ModelState.IsValid) return View(model);
        model.TenantId = TenantId;
        await _uow.Customers.AddAsync(model);
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Customer added!";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var c = await _uow.Customers.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
        if (c == null) return NotFound();
        return View(c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Customer model)
    {
        if (!ModelState.IsValid) return View(model);
        model.TenantId = TenantId;
        await _uow.Customers.UpdateAsync(model);
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Customer updated!";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var c = await _uow.Customers.Query()
            .Include(x => x.Invoices)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
        if (c == null) return NotFound();
        return View(c);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _uow.Customers.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
        if (c != null) { c.IsDeleted = true; await _uow.Customers.UpdateAsync(c); await _uow.SaveChangesAsync(); }
        return RedirectToAction("Index");
    }
}
