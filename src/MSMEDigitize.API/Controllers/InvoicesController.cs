using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSMEDigitize.Core.DTOs;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;
    private readonly ICurrentUserService _currentUser;

    public InvoicesController(IInvoiceService svc, ICurrentUserService cu) { _service = svc; _currentUser = cu; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string? search = null, [FromQuery] InvoiceStatus? status = null)
    {
        if (!_currentUser.TenantId.HasValue) return Unauthorized();
        var result = await _service.GetInvoicesAsync(_currentUser.TenantId.Value, page, size, search, status);
        return Ok(ApiResponse<PagedResult<object>>.Ok(new PagedResult<object>
        {
            Items = result.Items.Cast<object>().ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!_currentUser.TenantId.HasValue) return Unauthorized();
        var invoice = await _service.GetInvoiceAsync(id, _currentUser.TenantId.Value);
        return invoice == null ? NotFound() : Ok(ApiResponse<object>.Ok(invoice));
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        if (!_currentUser.TenantId.HasValue) return Unauthorized();
        var metrics = await _service.GetDashboardMetricsAsync(_currentUser.TenantId.Value);
        return Ok(ApiResponse<DashboardMetricsDto>.Ok(metrics));
    }

    [HttpGet("gst-summary")]
    public async Task<IActionResult> GSTSummary([FromQuery] int month = 0, [FromQuery] int year = 0)
    {
        if (!_currentUser.TenantId.HasValue) return Unauthorized();
        if (month == 0) month = DateTime.Now.Month;
        if (year == 0) year = DateTime.Now.Year;
        var summary = await _service.GetGSTSummaryAsync(_currentUser.TenantId.Value, month, year);
        return Ok(ApiResponse<object>.Ok(summary));
    }
}