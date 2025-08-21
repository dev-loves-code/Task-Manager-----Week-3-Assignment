using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Hubs;
using api.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendPrivateMessageAsync(string userId, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Attempted to send message to null/empty userId");
                    return;
                }


                await _hubContext.Clients.User(userId).SendAsync("ReceiveMessage", message);
                _logger.LogInformation("SignalR message sent to user {UserId}: {Message}", userId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send private message to user {UserId}", userId);
                throw;
            }
        }


    }
}