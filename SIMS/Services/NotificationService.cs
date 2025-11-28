using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    public NotificationService(ApplicationDbContext db) { _db = db; }

    public async Task NotifyUserAsync(string userId, string message, string? link = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });
        await _db.SaveChangesAsync();
    }

    public async Task NotifyUsersAsync(IEnumerable<string> userIds, string message, string? link = null)
    {
        var items = userIds.Distinct().Select(id => new Notification
        {
            UserId = id,
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });
        await _db.Notifications.AddRangeAsync(items);
        await _db.SaveChangesAsync();
    }
}

