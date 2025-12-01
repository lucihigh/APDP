using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Controllers;
using SIMS.Data;
using SIMS.Models;
using Xunit;

namespace SIMS.Tests;

public class CsvReportHelperTests
{
    private static ApplicationDbContext BuildDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static (ApplicationDbContext db, ReportsController controller, Course course, Student student) SeedCourseWithStudent()
    {
        var db = BuildDb();
        var course = new Course { Code = "CS101", Name = "Intro", Credits = 3, Department = "CS" };
        var student = new Student { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Program = "CS", Year = 1 };
        db.Courses.Add(course);
        db.Students.Add(student);
        db.SaveChanges();
        db.Enrollments.Add(new Enrollment { CourseId = course.Id, StudentId = student.Id, Semester = "2025S1", Grade = "A" });
        db.SaveChanges();
        var controller = new ReportsController(db);
        return (db, controller, course, student);
    }

    [Fact]
    public async Task CourseRosterCsv_ReturnsExpectedHeadersAndRow()
    {
        var (db, controller, course, student) = SeedCourseWithStudent();
        var result = await controller.CourseRosterCsv(course.Id);
        var file = Assert.IsType<FileContentResult>(result);
        var text = Encoding.UTF8.GetString(file.FileContents);

        Assert.Contains("Course,CS101,Intro", text);
        Assert.Contains("Email,FirstName,LastName,Program,Year", text);
        Assert.Contains($"{student.Email},{student.FirstName},{student.LastName},{student.Program},{student.Year}", text);
        db.Dispose();
    }

    [Fact]
    public async Task GradebookCsv_ReturnsGradesForEnrolledStudents()
    {
        var (db, controller, course, student) = SeedCourseWithStudent();
        var result = await controller.GradebookCsv(course.Id);
        var file = Assert.IsType<FileContentResult>(result);
        var text = Encoding.UTF8.GetString(file.FileContents);

        Assert.Contains("Email,Name,Semester,Grade", text);
        Assert.Contains($"{student.Email}", text);
        Assert.Contains("A", text);
        db.Dispose();
    }

    [Fact]
    public async Task SystemSummaryCsv_ReportsCounts()
    {
        using var db = BuildDb();
        db.Courses.Add(new Course { Code = "MATH101", Name = "Calc", Credits = 4 });
        db.Students.Add(new Student { FirstName = "Alan", LastName = "Turing", Email = "alan@example.com" });
        db.Enrollments.Add(new Enrollment { CourseId = 1, StudentId = 1, Semester = "2025S1" });
        db.SaveChanges();

        var controller = new ReportsController(db);
        var result = await controller.SystemSummaryCsv();
        var file = Assert.IsType<FileContentResult>(result);
        var text = Encoding.UTF8.GetString(file.FileContents);

        Assert.Contains("Metric,Value", text);
        Assert.Contains("Students,1", text);
        Assert.Contains("Courses,1", text);
        Assert.Contains("Enrollments,1", text);
    }

    [Fact]
    public async Task CourseRosterCsv_ContentTypeIsCsv()
    {
        var (db, controller, course, _) = SeedCourseWithStudent();
        var result = await controller.CourseRosterCsv(course.Id);
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        db.Dispose();
    }
}
