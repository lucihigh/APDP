using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;

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
        var data = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sw.ToString())).ToArray();
        return File(data, "text/csv; charset=utf-8", $"roster_{course?.Code}.csv");
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
        var data = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sw.ToString())).ToArray();
        return File(data, "text/csv; charset=utf-8", $"gradebook_{course?.Code}.csv");
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
        var data = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sw.ToString())).ToArray();
        return File(data, "text/csv; charset=utf-8", "summary.csv");
    }
}
