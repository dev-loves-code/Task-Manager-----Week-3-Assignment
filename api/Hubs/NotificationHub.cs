using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public async Task SendPrivateMessage(string userId, string message)
        {
            try
            {
                await Clients.User(userId).SendAsync("ReceiveMessage", message);
                _logger.LogInformation("Message sent to user {UserId}: {Message}", userId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to user {UserId}", userId);
                throw;
            }
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = Context.User?.FindFirst(ClaimTypes.GivenName)?.Value
                    ?? Context.User?.FindFirst("given_name")?.Value;

                _logger.LogInformation("User connected - UserId: {UserId}, UserName: {UserName}, ConnectionId: {ConnectionId}",
                    userId, userName, Context.ConnectionId);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync for ConnectionId: {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

    }
}