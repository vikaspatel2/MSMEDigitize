using Microsoft.AspNetCore.Mvc.Filters;
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
using MSMEDigitize.Infrastructure.Data;

namespace MSMEDigitize.Web.Filters;

public class AuditLogFilter : IAsyncActionFilter
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditLogFilter(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();
        
        // Log POST, PUT, DELETE
        var method = context.HttpContext.Request.Method;
        if (method is "POST" or "PUT" or "DELETE")
        {
            var audit = new AuditLog
            {
                TenantId = _currentUser.TenantId ?? Guid.Empty,
                UserId = _currentUser.UserId.ToString(),
                UserEmail = _currentUser.Email ?? "anonymous",
                Action = $"{method} {context.HttpContext.Request.Path}",
                EntityName = context.ActionDescriptor.DisplayName ?? "",
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow
            };
            _db.AuditLogs.Add(audit);
            await _db.SaveChangesAsync();
        }
    }
}
