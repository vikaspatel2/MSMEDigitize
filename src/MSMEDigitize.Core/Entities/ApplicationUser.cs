using Microsoft.AspNetCore.Identity;

namespace MSMEDigitize.Core.Entities;

/// <summary>
/// ASP.NET Identity user - handles authentication for the platform.
/// Business/tenant-specific data lives in TenantUser.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName   { get; set; }
    public string? LastName    { get; set; }
    public bool    IsActive    { get; set; } = true;
    public bool    IsSuperAdmin { get; set; } = false;
    public string  FullName    => $"{FirstName} {LastName}".Trim();

    // Refresh token support
    public string?   RefreshToken        { get; set; }
    public DateTime? RefreshTokenExpiry  { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
