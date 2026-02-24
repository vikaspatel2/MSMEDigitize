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
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Web.Controllers;

[Authorize]
public class ProductController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public ProductController(IUnitOfWork uow, ICurrentUserService cu) { _uow = uow; _currentUser = cu; }
    private Guid TenantId => _currentUser.TenantId!.Value;

    public async Task<IActionResult> Index(string? search, bool? lowStock)
    {
        var query = _uow.Products.Query().Where(p => p.TenantId == TenantId);
        if (!string.IsNullOrEmpty(search)) query = query.Where(p => p.Name.Contains(search) || (p.SKU != null && p.SKU.Contains(search)));
        if (lowStock == true) query = query.Where(p => p.CurrentStock <= p.ReorderPoint);
        return View(query.OrderBy(p => p.Name).ToList());
    }

    [HttpGet] public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _uow.ProductCategories.FindAsync(c => c.TenantId == TenantId);
        return View(new Product());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model)
    {
        if (!ModelState.IsValid) return View(model);
        model.TenantId = TenantId;
        await _uow.Products.AddAsync(model);
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Product added!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> StockAdjustment(Guid productId, decimal qty, string notes)
    {
        var product = await _uow.Products.FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == TenantId);
        if (product == null) return NotFound();
        
        await _uow.StockMovements.AddAsync(new StockMovement
        {
            TenantId = TenantId,
            ProductId = productId,
            Type = StockMovementType.Adjustment,
            Quantity = qty,
            PreviousStock = product.CurrentStock,
            NewStock = product.CurrentStock + qty,
            Notes = notes
        });
        product.CurrentStock += qty;
        await _uow.Products.UpdateAsync(product);
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Stock adjusted!";
        return RedirectToAction("Index");
    }
}
