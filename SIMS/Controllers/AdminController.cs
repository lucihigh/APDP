using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services;
using System.Text;

namespace SIMS.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var repaired = await UserIdRepairService.FixStudentAndFacultyAsync(_db, _userManager);
        if (repaired > 0)
        {
            TempData["Success"] = $"Updated {repaired} account IDs to BH/GV format.";
        }

        var studentUsers = await _userManager.GetUsersInRoleAsync("Student");
        var facultyUsers = await _userManager.GetUsersInRoleAsync("Faculty");
        var studentIds = new HashSet<string>(studentUsers.Select(u => u.Id), StringComparer.OrdinalIgnoreCase);

        var vm = new AdminDashboardViewModel
        {
            Students = await _db.Students.CountAsync(s => s.UserId != null && studentIds.Contains(s.UserId)),
            Courses = await _db.Courses.CountAsync(),
            Enrollments = await _db.Enrollments.CountAsync(),
            Faculty = await _db.FacultyProfiles.CountAsync()
        };
        return View(vm);
    }

    // List faculty accounts
    public async Task<IActionResult> Faculty()
    {
        var faculty = await _db.FacultyProfiles
            .Include(f => f.User)
            .OrderBy(f => f.Email)
            .ToListAsync();
        return View(faculty);
    }

    // Create Student Account
    public IActionResult CreateStudent()
    {
        // Use the unified student creation flow
        return RedirectToAction("Create", "Students");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudent(CreateStudentInput input)
    {
        // Forward POSTs to the unified flow (keeps old links from breaking)
        return RedirectToAction("Create", "Students");
    }

    // Reset Student Password
    public IActionResult ResetPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string email, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User not found");
            return View();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var res = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors)
            {
                ModelState.AddModelError(string.Empty, e.Description);
            }
            return View();
        }

        TempData["Success"] = "Password reset successfully";
        return RedirectToAction(nameof(Index));
    }

    // Import Students from CSV (Email,FirstName,LastName,Program,Year)
    public IActionResult ImportStudents() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportStudents(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Select a CSV file");
            return View();
        }

        // Clean up orphaned student rows whose UserId no longer exists
        var existingUserIds = new HashSet<string>(_db.Users.Select(u => u.Id), StringComparer.OrdinalIgnoreCase);
        var orphans = await _db.Students
            .Where(s => s.UserId != null && !existingUserIds.Contains(s.UserId))
            .ToListAsync();
        if (orphans.Any())
        {
            _db.Students.RemoveRange(orphans);
            await _db.SaveChangesAsync();
        }

        using var reader = new StreamReader(file.OpenReadStream());
        int created = 0, updated = 0;
        string? line;
        bool header = true;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (header)
            {
                header = false;
                continue;
            }

            var cols = line.Split(',');
            if (cols.Length < 5) continue;

            var email = cols[0].Trim();
            var first = cols[1].Trim();
            var last = cols[2].Trim();
            var program = cols[3].Trim();
            _ = int.TryParse(cols[4].Trim(), out var year);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var userId = await UserIdGenerator.GenerateForRoleAsync(_userManager, "Student");
                user = new IdentityUser
                {
                    Id = userId,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                await _userManager.CreateAsync(user, "Student#12345");
                await _userManager.AddToRoleAsync(user, "Student");
                created++;
            }
            else
            {
                if (!UserIdGenerator.IsFormatted(user.Id, "Student"))
                {
                    user = await UserIdRepairService.RepairSingleStudentAsync(_db, _userManager, user, "Student");
                }
                updated++;
            }

            var student =
                await _db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id)
                ?? new Student { UserId = user.Id, Email = email };

            student.FirstName = first;
            student.LastName = last;
            student.Program = program;
            student.Year = year;
            student.Email = email;

            _db.Update(student);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Imported: {created} created, {updated} users updated";
        return RedirectToAction(nameof(Index));
    }

    // Export Students CSV
    public async Task<FileResult> ExportStudents()
    {
        var rows = await _db.Students.AsNoTracking().ToListAsync();
        var sw = new StringWriter();
        sw.WriteLine("Email,FirstName,LastName,Program,Year,GPA");
        foreach (var s in rows)
        {
            sw.WriteLine($"{s.Email},{s.FirstName},{s.LastName},{s.Program},{s.Year},{s.GPA}");
        }

        return File(Encoding.UTF8.GetBytes(sw.ToString()), "text/csv", "students.csv");
    }
}

public class CreateStudentInput
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    public string? Program { get; set; }

    public int? Year { get; set; }

    public string? Password { get; set; }
}
