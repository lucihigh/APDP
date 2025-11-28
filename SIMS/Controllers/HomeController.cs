using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SIMS.Models;
using SIMS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace SIMS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _logger = logger;
        _db = db;
        _userManager = userManager;
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var studentUsers = await _userManager.GetUsersInRoleAsync("Student");
        var facultyUsers = await _userManager.GetUsersInRoleAsync("Faculty");
        var studentUserIds = new HashSet<string>(studentUsers.Select(u => u.Id), StringComparer.OrdinalIgnoreCase);

        var vm = new DashboardViewModel
        {
            Students = await _db.Students.CountAsync(s => s.UserId != null && studentUserIds.Contains(s.UserId)),
            Courses = await _db.Courses.CountAsync(),
            Enrollments = await _db.Enrollments.CountAsync(),
            Faculty = facultyUsers.Count
        };
        return View(vm);
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
