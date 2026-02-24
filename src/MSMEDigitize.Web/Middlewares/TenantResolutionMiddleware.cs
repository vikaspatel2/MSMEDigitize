using System.Security.Claims;

namespace MSMEDigitize.Web.Middlewares;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = context.User.FindFirstValue("TenantId");
            if (!string.IsNullOrEmpty(tenantId))
            {
                context.Items["TenantId"] = Guid.Parse(tenantId);
            }
        }
        await _next(context);
    }
}
