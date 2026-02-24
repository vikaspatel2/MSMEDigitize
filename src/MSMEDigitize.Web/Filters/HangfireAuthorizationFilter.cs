using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace MSMEDigitize.Web.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all
        var httpContext = context.GetHttpContext();
        if (httpContext == null) return false;
        return httpContext.User?.Identity?.IsAuthenticated == true 
               && httpContext.User.IsInRole("SuperAdmin");
    }
}
