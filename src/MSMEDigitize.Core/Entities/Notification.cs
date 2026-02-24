using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Core.Entities;

public class Notification : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? Icon { get; set; }
    public NotificationChannel Channel { get; set; }  // InApp, Email, SMS, WhatsApp

    public ApplicationUser User { get; set; } = null!;
}