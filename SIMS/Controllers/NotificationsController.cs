using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;

namespace SIMS.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    public NotificationsController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db; _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var items = await _db.Notifications
            .Where(n => n.UserId == user!.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user!.Id);
        if (n != null) { n.IsRead = true; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}

