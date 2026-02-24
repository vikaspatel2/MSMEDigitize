using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using Newtonsoft.Json;

namespace MSMEDigitize.Web.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IPaymentGatewayService _payment;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;

    public SubscriptionController(IUnitOfWork uow, IPaymentGatewayService payment, ICurrentUserService cu, IEmailService email)
    { _uow = uow; _payment = payment; _currentUser = cu; _emailService = email; }

    [AllowAnonymous]
    public async Task<IActionResult> Plans()
    {
        var allPlans = await _uow.SubscriptionPlans.GetAllAsync();
        var plans = allPlans.Where(p => p.IsActive);
        return View(plans.OrderBy(p => p.MonthlyPrice));
    }

    public async Task<IActionResult> Current()
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;
        var allSubs  = await _uow.Subscriptions.GetAllAsync();
        var sub      = allSubs.FirstOrDefault(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active);
        return View(sub);
    }

    public async Task<IActionResult> Upgrade(Guid planId)
    {
        var plan = await _uow.SubscriptionPlans.GetByIdAsync(planId);
        if (plan == null) return NotFound();

        var order = await _payment.CreateOrderAsync(plan.MonthlyPrice, "INR", $"SUB-{_currentUser.TenantId ?? Guid.Empty}");
        ViewBag.Order = order;
        ViewBag.Plan  = plan;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyPayment(string orderId, string paymentId, string signature, Guid planId)
    {
        var valid = await _payment.VerifyPaymentAsync(orderId, paymentId, signature);
        if (!valid) { TempData["Error"] = "Payment verification failed."; return RedirectToAction("Plans"); }

        var plan = await _uow.SubscriptionPlans.GetByIdAsync(planId);
        if (plan == null) return NotFound();

        var tenantId = _currentUser.TenantId!.Value;

        // Expire current subscription
        var current = await _uow.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active);
        if (current != null) current.Status = SubscriptionStatus.Cancelled;

        // Create new subscription using correct entity name: TenantSubscription
        //var newSub = new TenantSubscription
        //{
        //    TenantId     = tenantId,
        //    PlanId       = plan.Id,
        //    Status       = SubscriptionStatus.Active,
        //    BillingCycle = BillingCycle.Monthly,
        //    Amount       = plan.MonthlyPrice,
        //    FinalAmount  = plan.MonthlyPrice,
        //    StartDate    = DateTime.UtcNow,
        //    EndDate      = DateTime.UtcNow.AddMonths(1),
        //    AutoRenew    = true
        //};
        //await _uow.Subscriptions.AddAsync(newSub);

        // Record payment
        await _uow.Payments.AddAsync(new Payment
        {
            TenantId           = tenantId,
            //SubscriptionId     = newSub.Id,
            RazorpayOrderId    = orderId,
            RazorpayPaymentId  = paymentId,
            RazorpaySignature  = signature,
            Amount             = plan.MonthlyPrice,
            Status             = PaymentStatus.Captured,
            Method             = PaymentMethod.UPI,
            PaidAt             = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"Upgraded to {plan.Name} plan successfully!";
        return RedirectToAction("Current");
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var body    = await new StreamReader(Request.Body).ReadToEndAsync();
        var payload = JsonConvert.DeserializeObject<dynamic>(body);
        // Handle payment.captured, subscription events etc.
        return Ok();
    }
}
