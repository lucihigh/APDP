using SIMS.Models;

namespace SIMS.Services;

public interface INotificationService
{
    Task NotifyUserAsync(string userId, string message, string? link = null);
    Task NotifyUsersAsync(IEnumerable<string> userIds, string message, string? link = null);
}

