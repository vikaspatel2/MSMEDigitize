using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSMEDigitize.Application.Commands.Customer;
using MSMEDigitize.Application.Commands.Invoice;
//using MSMEDigitize.Application.Commands.Invoice.MSMEDigitize.Application.Queries.Invoice;
//using MSMEDigitize.Application.Commands.Invoice.MSMEDigitize.Application.Queries.Invoice.MSMEDigitize.Application.Commands.Customer;
//using MSMEDigitize.Application.Commands.Invoice.MSMEDigitize.Application.Queries.Invoice.MSMEDigitize.Application.Commands.Customer.MSMEDigitize.Application.Queries.Customer;
//using MSMEDigitize.Application.Commands.Invoice.MSMEDigitize.Application.Queries.Invoice.MSMEDigitize.Application.Commands.Customer.MSMEDigitize.Application.Queries.Customer.MSMEDigitize.Application.Commands.Product.MSMEDigitize.Application.Queries.Dashboard;
//using MSMEDigitize.Application.Commands.Invoice.MSMEDigitize.Application.Queries.Invoice.MSMEDigitize.Application.Commands.Customer.MSMEDigitize.Application.Queries.Customer.MSMEDigitize.Application.Commands.Product.MSMEDigitize.Application.Queries.Dashboard.MSMEDigitize.Application.Queries.Analytics;
using MSMEDigitize.Application.Commands.Product;
using MSMEDigitize.Application.DTOs;
using MSMEDigitize.Application.Queries.Analytics;
using MSMEDigitize.Application.Queries.Customer;
using MSMEDigitize.Application.Queries.Dashboard;
using MSMEDigitize.Application.Queries.Invoice;
using MSMEDigitize.Core.DTOs;
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
using System.ComponentModel.DataAnnotations;
using ForgotPasswordDto = MSMEDigitize.Application.DTOs.ForgotPasswordDto;
using RefreshTokenDto = MSMEDigitize.Application.DTOs.RefreshTokenDto;
using ResetPasswordDto = MSMEDigitize.Application.DTOs.ResetPasswordDto;

namespace MSMEDigitize.Web.Controllers.Api;

[ApiController]
[Route("api/v1/invoices")]
[Authorize]
[Produces("application/json")]
public class InvoicesApiController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IPdfService _pdfService;

    public InvoicesApiController(IMediator mediator, ICurrentUserService currentUser, IPdfService pdfService)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _pdfService = pdfService;
    }

    /// <summary>Get paginated list of invoices with filters</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<InvoiceListDto>), 200)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetInvoicesQuery(
            _currentUser.TenantId ?? Guid.Empty, page, pageSize, status, customerId, fromDate, toDate, search));

        return result.IsSuccess
            ? Ok(new PagedApiResponse<InvoiceListDto>(result.Value!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Get invoice by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(_currentUser.TenantId ?? Guid.Empty, id));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : NotFound();
    }

    /// <summary>Create new invoice</summary>
    [HttpPost]
    [Authorize(Policy = "CanManageInvoices")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
    {
        var result = await _mediator.Send(new CreateInvoiceCommand(_currentUser.TenantId ?? Guid.Empty, _currentUser.UserId, dto));
        if (!result.IsSuccess) return BadRequest(ApiResponse.Fail(result.Error!));
        return CreatedAtAction(nameof(GetInvoice), new { id = result.Value!.Id }, ApiResponse.Ok(result.Value!));
    }

    /// <summary>Update draft invoice</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageInvoices")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), 200)]
    public async Task<IActionResult> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceDto dto)
    {
        var result = await _mediator.Send(new UpdateInvoiceCommand(_currentUser.TenantId ?? Guid.Empty, id, dto));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Send invoice to customer via email/WhatsApp</summary>
    [HttpPost("{id:guid}/send")]
    [Authorize(Policy = "CanManageInvoices")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> SendInvoice(Guid id, [FromBody] SendInvoiceRequest req)
    {
        var result = await _mediator.Send(new SendInvoiceCommand(_currentUser.TenantId ?? Guid.Empty, id, req.Message));
        return result.IsSuccess ? Ok(ApiResponse.Ok(true, "Invoice sent successfully")) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    ///// <summary>Record payment against invoice</summary>
    //[HttpPost("{id:guid}/payments")]
    //[Authorize(Policy = "CanManageInvoices")]
    //[ProducesResponseType(typeof(ApiResponse<PaymentDto>), 201)]
    //public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentDto dto)
    //{
    //    if (dto.InvoiceId != id) dto = dto with { InvoiceId = id };
    //    var result = await _mediator.Send(new RecordPaymentCommand(_currentUser.TenantId ?? Guid.Empty, dto));
    //    return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest(ApiResponse.Fail(result.Error!));
    //}

    /// <summary>Cancel an invoice</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "CanManageInvoices")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> CancelInvoice(Guid id, [FromBody] CancelInvoiceRequest req)
    {
        var result = await _mediator.Send(new CancelInvoiceCommand(_currentUser.TenantId ?? Guid.Empty, id, req.Reason));
        return result.IsSuccess ? Ok(ApiResponse.Ok(true, "Invoice cancelled")) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Download invoice as PDF</summary>
    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var result = await _mediator.Send(new GetInvoicePdfQuery(_currentUser.TenantId ?? Guid.Empty, id));
        if (!result.IsSuccess) return NotFound();
        return File(result.Value!, "application/pdf", $"invoice-{id}.pdf");
    }

    /// <summary>Generate E-Invoice (IRP integration)</summary>
    [HttpPost("{id:guid}/einvoice")]
    [Authorize(Policy = "CanManageInvoices")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> GenerateEInvoice(Guid id)
    {
        var result = await _mediator.Send(new GenerateEInvoiceCommand(_currentUser.TenantId ?? Guid.Empty, id));
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Value!, "E-Invoice generated successfully"))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Generate E-Way Bill</summary>
    [HttpPost("{id:guid}/ewaybill")]
    [Authorize(Policy = "CanManageInvoices")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> GenerateEWayBill(Guid id)
    {
        var result = await _mediator.Send(new GenerateEWayBillCommand(_currentUser.TenantId ?? Guid.Empty, id));
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Value!, "E-Way Bill generated"))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Bulk send payment reminders</summary>
    [HttpPost("bulk-reminders")]
    [Authorize(Policy = "CanManageInvoices")]
    public async Task<IActionResult> BulkSendReminders([FromBody] BulkReminderRequest req)
    {
        var result = await _mediator.Send(new BulkSendReminderCommand(_currentUser.TenantId ?? Guid.Empty, req.InvoiceIds));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!, $"{result.Value} reminders sent")) : BadRequest();
    }
}

// ─── Customer API ──────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/customers")]
[Authorize]
[Produces("application/json")]
public class CustomersApiController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public CustomersApiController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetCustomersQuery(_currentUser.TenantId ?? Guid.Empty, page, pageSize, search, isActive));
        return result.IsSuccess ? Ok(new PagedApiResponse<CustomerDto>(result.Value!)) : BadRequest();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(_currentUser.TenantId ?? Guid.Empty, id));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : NotFound();
    }

    [HttpPost]
    [Authorize(Policy = "CanManageInvoices")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        var result = await _mediator.Send(new CreateCustomerCommand(_currentUser.TenantId ?? Guid.Empty, dto));
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCustomer), new { id = result.Value!.Id }, ApiResponse.Ok(result.Value!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageInvoices")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        var result = await _mediator.Send(new UpdateCustomerCommand(_currentUser.TenantId ?? Guid.Empty, id, dto));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageInvoices")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(_currentUser.TenantId ?? Guid.Empty, id));
        return result.IsSuccess ? Ok() : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpGet("{id:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid id, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _mediator.Send(new GetCustomerStatementQuery(_currentUser.TenantId ?? Guid.Empty, id, from, to));
        return result.IsSuccess ? File(result.Value!, "application/pdf", $"statement-{id}.pdf") : NotFound();
    }
}

// ─── Dashboard API ─────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardApiController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public DashboardApiController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string period = "thisMonth")
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery(_currentUser.TenantId ?? Guid.Empty, period));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }

    [HttpGet("cashflow-forecast")]
    public async Task<IActionResult> GetCashFlowForecast([FromQuery] int days = 90)
    {
        var result = await _mediator.Send(new GetCashFlowForecastQuery(_currentUser.TenantId ?? Guid.Empty, days));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }

    [HttpGet("ai-insights")]
    public async Task<IActionResult> GetAIInsights([FromQuery] int limit = 10)
    {
        var result = await _mediator.Send(new GetAIInsightsQuery(_currentUser.TenantId ?? Guid.Empty, null, limit));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }
}

// ─── Analytics API ─────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/analytics")]
[Authorize(Policy = "CanViewReports")]
public class AnalyticsApiController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsApiController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueAnalytics(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] string groupBy = "month")
    {
        var result = await _mediator.Send(new GetRevenueAnalyticsQuery(_currentUser.TenantId ?? Guid.Empty, from, to, groupBy));
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }
}

// ─── Auth API ──────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthApiController : ApiBaseController
{
    private readonly IAuthService _authService;

    public AuthApiController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (!result.IsSuccess) return Unauthorized(ApiResponse.Fail(result.Error!));

        Response.Cookies.Append("access_token", result.Value!.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(24)
        });
        return Ok(ApiResponse.Ok(result.Value!));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 201)]
    public async Task<IActionResult> Register([FromBody] RegisterBusinessDto dto)
    {
        var result = await _authService.RegisterBusinessAsync(dto);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse.Ok(result.Value!, "Business registered successfully"))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    //[HttpPost("refresh")]
    //[AllowAnonymous]
    //public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    //{
    //    var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
    //    return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : Unauthorized();
    //}

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.SendPasswordResetEmailAsync(dto.Email);
        return Ok(ApiResponse.Ok(true, "If this email is registered, a reset link has been sent"));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
        return result.IsSuccess ? Ok() : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("access_token");
        await _authService.RevokeTokenAsync(User.FindFirst("sub")?.Value ?? "");
        return Ok();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var result = await _authService.ChangePasswordAsync(User.FindFirst("sub")?.Value!, dto);
        return result.IsSuccess ? Ok() : BadRequest(ApiResponse.Fail(result.Error!));
    }
}

// ─── GST API ───────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/gst")]
[Authorize(Policy = "CanManageInvoices")]
public class GSTApiController : ApiBaseController
{
    private readonly IGSTService _gstService;
    private readonly ICurrentUserService _currentUser;

    public GSTApiController(IGSTService gstService, ICurrentUserService currentUser)
    {
        _gstService = gstService;
        _currentUser = currentUser;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int month, [FromQuery] int year)
    {
        var result = await _gstService.GetSummaryAsync(_currentUser.TenantId ?? Guid.Empty, month, year);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }

    [HttpGet("gstr1")]
    public async Task<IActionResult> GetGSTR1([FromQuery] int month, [FromQuery] int year)
    {
        var result = await _gstService.GetGSTR1Async(_currentUser.TenantId ?? Guid.Empty, month, year);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }

    [HttpGet("gstr3b")]
    public async Task<IActionResult> GetGSTR3B([FromQuery] int month, [FromQuery] int year)
    {
        var result = await _gstService.GetGSTR3BAsync(_currentUser.TenantId ?? Guid.Empty, month, year);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }

    [HttpPost("validate-gstin")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateGSTIN([FromBody] ValidateGSTINRequest req)
    {
        var result = await _gstService.ValidateGSTINAsync(req.GSTIN);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("hsn-search")]
    public async Task<IActionResult> SearchHSN([FromQuery] string query)
    {
        var result = await _gstService.SearchHSNAsync(query);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("itc-reconciliation")]
    public async Task<IActionResult> GetITCReconciliation([FromQuery] int month, [FromQuery] int year)
    {
        var result = await _gstService.GetITCReconciliationAsync(_currentUser.TenantId ?? Guid.Empty, month, year);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }
}

// ─── Subscription API ──────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/subscriptions")]
[Authorize]
public class SubscriptionApiController : ApiBaseController
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ICurrentUserService _currentUser;

    public SubscriptionApiController(ISubscriptionService subscriptionService, ICurrentUserService currentUser)
    {
        _subscriptionService = subscriptionService;
        _currentUser = currentUser;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionService.GetPlansAsync();
        return Ok(ApiResponse.Ok(plans));
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var result = await _subscriptionService.GetStatusAsync(_currentUser.TenantId ?? Guid.Empty);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest();
    }

    [HttpPost("subscribe")]
    [Authorize(Policy = "TenantOwner")]
    public async Task<IActionResult> Subscribe([FromBody] CreateSubscriptionDto dto)
    {
        var result = await _subscriptionService.SubscribeAsync(_currentUser.TenantId ?? Guid.Empty, dto);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Value!)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPost("cancel")]
    [Authorize(Policy = "TenantOwner")]
    public async Task<IActionResult> Cancel()
    {
        var result = await _subscriptionService.CancelAsync(_currentUser.TenantId ?? Guid.Empty);
        return result.IsSuccess ? Ok() : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPost("razorpay-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> RazorpayWebhook([FromBody] object payload)
    {
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        await _subscriptionService.HandleRazorpayWebhookAsync(payload.ToString()!, signature);
        return Ok();
    }
}

// ─── Shared Request/Response Models ────────────────────────────────────────
public record SendInvoiceRequest(string? Message);
public record CancelInvoiceRequest([Required] string Reason);
public record BulkReminderRequest(List<Guid> InvoiceIds);
public record ValidateGSTINRequest([Required] string GSTIN);

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }

    public static ApiResponse<T> Ok<T>(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse Fail(string error) =>
        new() { Success = false, Error = error };
}

public class ApiResponse<T> : ApiResponse
{
    public new T? Data { get; set; }
}

public class PagedApiResponse<T> : ApiResponse
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }

    public PagedApiResponse(PagedResult<T> pagedResult)
    {
        Success = true;
        Items = pagedResult.Items;
        TotalCount = pagedResult.TotalCount;
        PageNumber = pagedResult.PageNumber;
        PageSize = pagedResult.PageSize;
        TotalPages = pagedResult.TotalPages;
        HasNextPage = pagedResult.HasNextPage;
        HasPreviousPage = pagedResult.HasPreviousPage;
    }
}

[ApiController]
public abstract class ApiBaseController : ControllerBase { }