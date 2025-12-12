using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services;
using System.Text;
using System.Globalization;

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

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Only CSV files are supported. Please use the provided template.");
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
        var errors = new List<string>();
        var parsedRows = new List<(string Email, string First, string Last, string Program, string Phone, string Address, int? Year, double? Gpa, DateOnly? Dob)>();

        string? headerLine;
        try
        {
            headerLine = await reader.ReadLineAsync();
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Unable to read the file. Please ensure it is a valid CSV encoded in UTF-8.");
            return View();
        }

        if (string.IsNullOrWhiteSpace(headerLine))
        {
            ModelState.AddModelError(string.Empty, "CSV file is empty");
            return View();
        }

        var headerMap = BuildHeaderIndex(headerLine);
        if (!headerMap.ContainsKey("email"))
        {
            ModelState.AddModelError(string.Empty, "CSV must include an Email column");
            return View();
        }

        string? line;
        try
        {
            var lineNumber = 2; // header is line 1
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    lineNumber++;
                    continue;
                }

                var cols = ParseCsvLine(line);
                string Get(string key) => TryGetColumn(cols, headerMap, key);

                var email = Get("email");
                if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                {
                    errors.Add($"Line {lineNumber}: Missing/invalid email.");
                    lineNumber++;
                    continue;
                }

                var first = Get("firstname");
                var last = Get("lastname");
                var program = Get("program");
                var phone = Get("phone");
                var address = Get("address");

                int? year = null;
                var yearText = Get("year");
                if (!string.IsNullOrWhiteSpace(yearText))
                {
                    if (int.TryParse(yearText, out var parsedYear))
                    {
                        year = parsedYear;
                    }
                    else
                    {
                        errors.Add($"Line {lineNumber}: Year must be a number.");
                    }
                }

                double? gpa = null;
                var rawGpa = Get("gpa");
                if (!string.IsNullOrWhiteSpace(rawGpa))
                {
                    var gpaText = rawGpa.Replace(',', '.');
                    if (double.TryParse(gpaText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedGpa))
                    {
                        gpa = parsedGpa;
                    }
                    else
                    {
                        errors.Add($"Line {lineNumber}: GPA is not a valid number.");
                    }
                }

                DateOnly? dob = null;
                var dobText = Get("dateofbirth");
                if (string.IsNullOrWhiteSpace(dobText))
                {
                    dobText = Get("dob");
                }
                if (!string.IsNullOrWhiteSpace(dobText))
                {
                    if (DateTime.TryParse(dobText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDob))
                    {
                        dob = DateOnly.FromDateTime(parsedDob);
                    }
                    else
                    {
                        errors.Add($"Line {lineNumber}: Date of birth is not a valid date.");
                    }
                }

                parsedRows.Add((email, first, last, program, phone, address, year, gpa, dob));
                lineNumber++;
            }
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "The file format is invalid. Please ensure the CSV columns follow the template and values are well-formed.");
            return View();
        }

        if (errors.Any())
        {
            ModelState.AddModelError(string.Empty, "Invalid data found: " + string.Join(" | ", errors.Take(5)) + (errors.Count > 5 ? $" (+{errors.Count - 5} more)" : string.Empty));
            return View();
        }

        // handle duplicate emails in the uploaded file
        var distinctRows = parsedRows
            .GroupBy(r => r.Email, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                if (g.Count() > 1)
                {
                    errors.Add($"Duplicate email in file: {g.Key}");
                }
                return g.First();
            })
            .ToList();

        if (errors.Any())
        {
            ModelState.AddModelError(string.Empty, "Invalid data found: " + string.Join(" | ", errors.Take(5)) + (errors.Count > 5 ? $" (+{errors.Count - 5} more)" : string.Empty));
            return View();
        }

        try
        {
            foreach (var row in distinctRows)
            {
                var (email, first, last, program, phone, address, year, gpa, dob) = row;
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
                student.Phone = phone;
                student.Address = address;
                student.GPA = gpa;
                student.DateOfBirth = dob;

                _db.Students.Update(student);
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Invalid file or data. Please check the template and try again.");
            return View();
        }
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
        var utf8Bom = Encoding.UTF8.GetPreamble();
        var data = utf8Bom.Concat(Encoding.UTF8.GetBytes(sw.ToString())).ToArray();
        return File(data, "text/csv; charset=utf-8", "students.csv");
    }

    private static Dictionary<string, int> BuildHeaderIndex(string headerLine)
    {
        var headers = ParseCsvLine(headerLine);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
        {
            var key = NormalizeHeader(headers[i]);
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
            {
                map[key] = i;
            }
        }

        return map;
    }

    private static string NormalizeHeader(string header)
    {
        return header.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    private static string TryGetColumn(IReadOnlyList<string> cols, Dictionary<string, int> headerMap, string key)
    {
        if (headerMap.TryGetValue(NormalizeHeader(key), out var idx) && idx >= 0 && idx < cols.Count)
        {
            return cols[idx].Trim();
        }

        return string.Empty;
    }

    // Minimal CSV parser that supports quoted values and commas inside quotes
    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
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
