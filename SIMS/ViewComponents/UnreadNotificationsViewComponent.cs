using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;

namespace SIMS.ViewComponents;

public class UnreadNotificationsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public UnreadNotificationsViewComponent(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Content(string.Empty);
        }

        var unread = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .CountAsync();

        return View(unread);
    }
}
