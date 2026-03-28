using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BizFlow.Api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var profileId = Context.User?.FindFirst("profileId")?.Value;
            if (!string.IsNullOrWhiteSpace(profileId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, BuildUserGroup(profileId));
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var profileId = Context.User?.FindFirst("profileId")?.Value;
            if (!string.IsNullOrWhiteSpace(profileId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildUserGroup(profileId));
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string BuildUserGroup(string userId)
        {
            return $"user:{userId}";
        }
    }
}