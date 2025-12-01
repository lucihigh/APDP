using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Services;
using Xunit;

namespace SIMS.Tests;

public class NotificationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _service = new NotificationService(_db);
    }

    [Fact]
    public async Task NotifyUserAsync_AddsSingleNotification()
    {
        await _service.NotifyUserAsync("user-1", "Hello", "/link");

        var saved = await _db.Notifications.SingleAsync();
        Assert.Equal("user-1", saved.UserId);
        Assert.Equal("Hello", saved.Message);
        Assert.Equal("/link", saved.Link);
        Assert.False(saved.IsRead);
        Assert.InRange(saved.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
    }

    [Fact]
    public async Task NotifyUsersAsync_DeduplicatesIds_AndPersistsAll()
    {
        await _service.NotifyUsersAsync(new[] { "u1", "u1", "u2" }, "Hi all");

        var saved = await _db.Notifications.OrderBy(n => n.UserId).ToListAsync();
        Assert.Equal(2, saved.Count);
        Assert.Equal(new[] { "u1", "u2" }, saved.Select(n => n.UserId));
        Assert.All(saved, n => Assert.Equal("Hi all", n.Message));
    }

    [Fact]
    public async Task NotifyUserAsync_AllowsNullLink_AndMarksUnread()
    {
        await _service.NotifyUserAsync("abc", "Msg", null);
        var saved = await _db.Notifications.SingleAsync();
        Assert.Null(saved.Link);
        Assert.False(saved.IsRead);
    }

    [Fact]
    public async Task NotifyUsersAsync_SetsCreatedAtUtc()
    {
        await _service.NotifyUsersAsync(new[] { "a", "b" }, "Test");
        var saved = await _db.Notifications.ToListAsync();
        Assert.Equal(2, saved.Count);
        Assert.All(saved, n => Assert.InRange(n.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow));
    }

    [Fact]
    public async Task NotifyUserAsync_MultipleCalls_AccumulatesRows()
    {
        await _service.NotifyUserAsync("u1", "First");
        await _service.NotifyUserAsync("u1", "Second");
        var saved = await _db.Notifications.OrderBy(n => n.Id).ToListAsync();
        Assert.Equal(2, saved.Count);
        Assert.Equal(new[] { "First", "Second" }, saved.Select(n => n.Message));
    }

    [Fact]
    public async Task NotifyUsersAsync_IgnoresEmptyInput()
    {
        await _service.NotifyUsersAsync(Array.Empty<string>(), "Nothing");
        Assert.Empty(_db.Notifications);
    }

    [Fact]
    public async Task NotifyUsersAsync_TrimsDuplicateIdsAcrossCalls()
    {
        await _service.NotifyUsersAsync(new[] { "x", "y", "x" }, "Hello");
        await _service.NotifyUsersAsync(new[] { "y", "z" }, "Hello");
        var saved = await _db.Notifications.ToListAsync();
        Assert.Equal(4, saved.Count); // per-call distinct: x,y then y,z
        Assert.Equal(2, saved.Count(n => n.UserId == "y"));
    }

    [Fact]
    public async Task NotifyUserAsync_PersistsLinkValue()
    {
        await _service.NotifyUserAsync("u-link", "Link test", "/foo/bar");
        var n = await _db.Notifications.SingleAsync();
        Assert.Equal("/foo/bar", n.Link);
    }

    public void Dispose() => _db.Dispose();
}
