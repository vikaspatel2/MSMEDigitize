using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IInvoiceService invoiceService, ICurrentUserService currentUser)
    {
        _invoiceService = invoiceService; _currentUser = currentUser;
    }

    //public async Task<IActionResult> Index()
    //{
    //    if (!_currentUser.TenantId.HasValue) return RedirectToAction("Register", "Account");
    //    var metrics = await _invoiceService.GetDashboardMetricsAsync(_currentUser.TenantId.Value);
    //    return View(metrics);
    //}
    public async Task<IActionResult> Index()
    {
        // Hardcode the TenantId value for testing purposes
        var tenantId = Guid.Parse("32D0BDA1-BCF3-4E05-BCB5-0A9D9B1C8BF8");

        // Call the service with the hardcoded TenantId
        var metrics = await _invoiceService.GetDashboardMetricsAsync(tenantId);

        // Return the view with the metrics
        return View(metrics);
    }
}