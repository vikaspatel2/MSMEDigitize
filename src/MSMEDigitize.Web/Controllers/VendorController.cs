using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
public class VendorController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    public VendorController(IUnitOfWork uow, ICurrentUserService cu) { _uow = uow; _currentUser = cu; }
    private Guid TenantId => _currentUser.TenantId!.Value;

    public async Task<IActionResult> Index()
    {
        var vendors = await _uow.Vendors.FindAsync(v => v.TenantId == TenantId && v.IsActive);
        return View(vendors.OrderBy(v => v.Name));
    }
    [HttpGet] public IActionResult Create() => View(new Vendor());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Vendor model)
    {
        if (!ModelState.IsValid) return View(model);
        model.TenantId = TenantId;
        await _uow.Vendors.AddAsync(model);
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Vendor added!";
        return RedirectToAction("Index");
    }
}
