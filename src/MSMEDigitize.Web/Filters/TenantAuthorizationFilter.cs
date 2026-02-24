using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute : Attribute, IFilterMetadata { }

public class TenantAuthorizationFilter : IAuthorizationFilter
{
    private readonly ICurrentUserService _currentUser;

    public TenantAuthorizationFilter(ICurrentUserService currentUser) => _currentUser = currentUser;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Skip for super admin
        if (_currentUser.IsSuperAdmin) return;
        // Skip for non-authenticated areas
        if (!_currentUser.IsAuthenticated) return;
        
        var hasRequireTenant = context.ActionDescriptor.FilterDescriptors
            .Any(fd => fd.Filter is RequireTenantAttribute);
        
        if (hasRequireTenant && !_currentUser.TenantId.HasValue)
        {
            context.Result = new RedirectToActionResult("SelectTenant", "Account", null);
        }
    }
}
