using SubscriptionStatus = MSMEDigitize.Core.Enums.SubscriptionStatus;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
using MSMEDigitize.Infrastructure.Data;
using MSMEDigitize.Web.ViewModels;

namespace MSMEDigitize.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly AppDbContext _db;

    public AccountController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm,
        IUnitOfWork uow, IEmailService email, AppDbContext db)
    {
        _userManager = um; _signInManager = sm; _uow = uow; _emailService = email; _db = db;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View(model);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut) ModelState.AddModelError("", "Account locked. Try after 15 minutes.");
            else ModelState.AddModelError("", "Invalid credentials.");
            return View(model);
        }

        // Get tenant
        //var tenantUser = await _uow.TenantUsers.FirstOrDefaultAsync(tu => tu.UserId == user.Id && tu.IsActive);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("FirstName", user.FirstName),
        };

        if (user.IsSuperAdmin) claims.Add(new(ClaimTypes.Role, "SuperAdmin"));
        //if (tenantUser != null)
        //{
        //    claims.Add(new("TenantId", tenantUser.TenantId.ToString()));
        //    claims.Add(new(ClaimTypes.Role, tenantUser.Role.ToString()));
        //}

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl) : RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register() => View();

    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Register(RegisterViewModel model)
    //{
    //    if (!ModelState.IsValid) return View(model);

    //    var existing = await _userManager.FindByEmailAsync(model.Email);
    //    if (existing != null) { ModelState.AddModelError("Email", "Email already registered."); return View(model); }

    //    await _uow.BeginTransactionAsync();
    //    try
    //    {
    //        var user = new ApplicationUser
    //        {
    //            UserName = model.Email,
    //            Email = model.Email,
    //            FirstName = model.FirstName,
    //            LastName = model.LastName,
    //            EmailConfirmed = true
    //        };
    //        var result = await _userManager.CreateAsync(user, model.Password);
    //        if (!result.Succeeded)
    //        {
    //            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
    //            return View(model);
    //        }

    //        // Create tenant
    //        var tenant = new Tenant
    //        {
    //            BusinessName = model.BusinessName,
    //            BusinessType = model.BusinessType,
    //            PrimaryContactEmail = model.Email,
    //            PrimaryContactPhone = model.Phone,
    //            //Industry = model.Industry,
    //            GSTNumber = model.GSTNumber,
    //            Status = TenantStatus.Active,
    //            Slug = model.BusinessName.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString()[..6]
    //        };
    //        await _uow.Tenants.AddAsync(tenant);
    //        await _uow.SaveChangesAsync();

    //        //var tenantUser = new TenantUser { TenantId = tenant.Id, UserId = user.Id, Role = TenantRole.Owner, IsOwner = true };
    //        //await _uow.TenantUsers.AddAsync(tenantUser);

    //        // Create default trial subscription
    //        var starterPlan = await _uow.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Starter");
    //        if (starterPlan != null)
    //        {
    //            var subscription = new Subscription
    //            {
    //                TenantId = tenant.Id,
    //                PlanId = starterPlan.Id,
    //               // Status = SubscriptionStatus.Trialing,
    //                BillingCycle = BillingCycle.Monthly,
    //                Amount = starterPlan.MonthlyPrice,
    //                FinalAmount = 0,
    //                StartDate = DateTime.UtcNow,
    //                EndDate = DateTime.UtcNow.AddDays(14),
    //                TrialEndDate = DateTime.UtcNow.AddDays(14),
    //                // IsTrialPeriod auto-computed from Status=Trial
    //            };
    //            await _uow.Subscriptions.AddAsync(subscription);
    //        }

    //        // Default chart of accounts
    //        await SeedDefaultAccountsAsync(tenant.Id);

    //        await _uow.SaveChangesAsync();
    //        await _uow.CommitTransactionAsync();

    //        await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName, tenant.BusinessName);

    //        TempData["Success"] = "Account created! Start your 14-day free trial.";
    //        return RedirectToAction("Login");
    //    }
    //    catch (Exception)
    //    {
    //        await _uow.RollbackTransactionAsync();
    //        throw;
    //    }
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError("Email", "Email already registered.");
            return View(model);
        }

        try
        {
            // 1️⃣ Create Identity User
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(model);
            }

            // 2️⃣ Create Tenant
            var tenant = new Tenant
            {
                BusinessName = model.BusinessName,
                BusinessType = model.BusinessType,
                PrimaryContactEmail = model.Email,
                PrimaryContactPhone = model.Phone,
                GSTNumber = model.GSTNumber,
                Status = TenantStatus.Active,
                Slug = model.BusinessName.ToLower().Replace(" ", "-") +
                       "-" + Guid.NewGuid().ToString()[..6]
            };

            await _uow.Tenants.AddAsync(tenant);
            await _uow.SaveChangesAsync();

            // 3️⃣ Create TenantUser Mapping (CRITICAL FIX)
            var tenantUser = new TenantUser
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = TenantRole.Owner,
                IsOwner = true,
                IsActive = true
            };

            await _uow.TenantUsers.AddAsync(tenantUser);

            // 4️⃣ Seed default accounts
            await SeedDefaultAccountsAsync(tenant.Id);

            await _uow.SaveChangesAsync();

            // 5️⃣ Auto Sign-In with TenantId Claim
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim("FirstName", user.FirstName),
            new Claim("TenantId", tenant.Id.ToString()),
            new Claim(ClaimTypes.Role, TenantRole.Owner.ToString())
        };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            await _emailService.SendWelcomeEmailAsync(
                user.Email!,
                user.FirstName,
                tenant.BusinessName);

            return RedirectToAction("Index", "Dashboard");
        }
        catch
        {
            ModelState.AddModelError("", "Something went wrong.");
            return View(model);
        }
    }

    private async Task SeedDefaultAccountsAsync(Guid tenantId)
    {
        var accounts = new List<ChartOfAccount>
        {
            new() { TenantId = tenantId, AccountCode = "1001", AccountName = "Cash in Hand", AccountType = AccountType.Asset, AccountGroup = AccountGroup.CurrentAsset, IsSystemAccount = true },
            new() { TenantId = tenantId, AccountCode = "1002", AccountName = "Bank Account", AccountType = AccountType.Asset, AccountGroup = AccountGroup.CurrentAsset, IsSystemAccount = true },
            new() { TenantId = tenantId, AccountCode = "1101", AccountName = "Accounts Receivable", AccountType = AccountType.Asset, AccountGroup = AccountGroup.CurrentAsset, IsSystemAccount = true },
            new() { TenantId = tenantId, AccountCode = "2001", AccountName = "Accounts Payable", AccountType = AccountType.Liability, AccountGroup = AccountGroup.CurrentLiability, IsSystemAccount = true },
            new() { TenantId = tenantId, AccountCode = "2101", AccountName = "GST Payable", AccountType = AccountType.Liability, AccountGroup = AccountGroup.CurrentLiability, IsSystemAccount = true },
            new() { TenantId = tenantId, AccountCode = "4001", AccountName = "Sales Revenue", AccountType = AccountType.Income, AccountGroup = AccountGroup.Revenue, IsSystemAccount = true },
            new() { TenantId = tenantId, AccountCode = "5001", AccountName = "Purchase Expenses", AccountType = AccountType.Expense, AccountGroup = AccountGroup.DirectExpense, IsSystemAccount = true },
            new() { TenantId = tenantId, AccountCode = "5101", AccountName = "Salary Expenses", AccountType = AccountType.Expense, AccountGroup = AccountGroup.IndirectExpense, IsSystemAccount = true },
        };
        await _uow.ChartOfAccounts.AddRangeAsync(accounts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) { TempData["Message"] = "If email exists, reset link sent."; return View(); }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);
        await _emailService.SendEmailAsync(model.Email, "Reset Password - MSME Digitize",
            $"<p>Click to reset: <a href='{resetUrl}'>Reset Password</a></p><p>Link expires in 24 hours.</p>");
        TempData["Message"] = "Password reset link sent to your email.";
        return View();
    }
}