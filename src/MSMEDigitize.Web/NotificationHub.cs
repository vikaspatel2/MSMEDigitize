using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MSMEDigitize.Web;

[Authorize]
public class NotificationHub : Hub
{
    public async Task JoinTenantGroup(string tenantId) => await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
    public async Task LeaveGroup(string tenantId) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
}