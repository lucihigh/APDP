using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using System.Globalization;
using System.Text;

namespace SIMS.Controllers;

[Authorize(Roles = "Admin,Faculty")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var courses = await _db.Courses
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, Label = $"{c.Code} - {c.Name}" })
            .ToListAsync();
        ViewBag.Courses = courses;
        return View();
    }

    public async Task<FileResult> CourseRosterCsv(int courseId)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null)
        {
            TempData["Error"] = "Course not found.";
            return File(Array.Empty<byte>(), "text/csv", "invalid_course.csv");
        }
        var rows = await _db.Enrollments
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Student)
            .Select(e => e.Student!)
            .ToListAsync();
        if (!rows.Any())
        {
            TempData["Error"] = "No enrollments found for this course.";
        }
        var sw = new StringWriter();
        sw.WriteLine($"Course,{course?.Code},{course?.Name}");
        sw.WriteLine("Email,FirstName,LastName,Program,Year");
        foreach (var s in rows) sw.WriteLine($"{s.Email},{s.FirstName},{s.LastName},{s.Program},{s.Year}");
        var payload = AddBom(sw.ToString());
        return File(payload, "text/csv; charset=utf-8", $"roster_{course?.Code}.csv");
    }

    public async Task<FileResult> GradebookCsv(int courseId)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null)
        {
            TempData["Error"] = "Course not found.";
            return File(Array.Empty<byte>(), "text/csv", "invalid_course.csv");
        }
        var rows = await _db.Enrollments
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Student)
            .ToListAsync();
        if (!rows.Any())
        {
            TempData["Error"] = "No enrollments found for this course.";
        }
        var sw = new StringWriter();
        sw.WriteLine($"Course,{course?.Code},{course?.Name}");
        sw.WriteLine("Email,Name,Semester,Grade");
        foreach (var e in rows) sw.WriteLine($"{e.Student?.Email},{e.Student?.FirstName} {e.Student?.LastName},{e.Semester},{e.Grade}");
        var payload = AddBom(sw.ToString());
        return File(payload, "text/csv; charset=utf-8", $"gradebook_{course?.Code}.csv");
    }

    [Authorize(Roles = "Admin")]
    public async Task<FileResult> SystemSummaryCsv()
    {
        var students = await _db.Students.CountAsync();
        var courses = await _db.Courses.CountAsync();
        var enrollments = await _db.Enrollments.CountAsync();
        var sw = new StringWriter();
        sw.WriteLine("Metric,Value");
        sw.WriteLine($"Students,{students}");
        sw.WriteLine($"Courses,{courses}");
        sw.WriteLine($"Enrollments,{enrollments}");
        var payload = AddBom(sw.ToString());
        return File(payload, "text/csv; charset=utf-8", "summary.csv");
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> CourseMetrics(int courseId)
    {
        var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var enrollments = await _db.Enrollments
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Student)
            .AsNoTracking()
            .ToListAsync();

        var total = enrollments.Count;
        var graded = enrollments.Where(e => !string.IsNullOrWhiteSpace(e.Grade)).ToList();
        var pending = total - graded.Count;

        var gradeValues = graded
            .Select(e => double.TryParse(e.Grade, NumberStyles.Any, CultureInfo.InvariantCulture, out var g) ? g : (double?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        double? average = gradeValues.Any() ? gradeValues.Average() : null;

        int Bucket(Func<double, bool> predicate) => gradeValues.Count(predicate);
        var distribution = new[]
        {
            new { label = "9 - 10", count = Bucket(g => g >= 9) },
            new { label = "8 - 8.9", count = Bucket(g => g >= 8 && g < 9) },
            new { label = "6 - 7.9", count = Bucket(g => g >= 6 && g < 8) },
            new { label = "< 6", count = Bucket(g => g < 6) },
        };

        var programs = enrollments
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Student?.Program) ? "Unknown" : e.Student!.Program!)
            .Select(g => new { label = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        var students = enrollments
            .Select(e => new
            {
                name = $"{e.Student!.FirstName} {e.Student.LastName}".Trim(),
                email = e.Student.Email,
                program = e.Student.Program,
                year = e.Student.Year,
                grade = e.Grade,
                semester = e.Semester
            })
            .ToList();

        return Json(new
        {
            course = new { course.Id, course.Code, course.Name },
            total,
            graded = graded.Count,
            pending,
            averageGrade = average,
            distribution,
            programs,
            students
        });
    }

    private static byte[] AddBom(string content)
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var preamble = utf8.GetPreamble();
        var body = utf8.GetBytes(content);
        var payload = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, payload, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, payload, preamble.Length, body.Length);
        return payload;
    }
}
