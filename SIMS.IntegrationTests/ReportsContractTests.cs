using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIMS.Data;
using SIMS.Models;
using Xunit;

namespace SIMS.IntegrationTests;

public class ReportsContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsContractTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task CourseRosterCsv_HasStableHeadersAndRowShape()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var course = await db.Courses.OrderBy(c => c.Id).FirstAsync();

        var student = new Student
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Program = "CS",
            Year = 1
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();
        db.Enrollments.Add(new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            Semester = "2025S1",
            Grade = "A"
        });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/Reports/CourseRosterCsv?courseId={course.Id}");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.StartsWith("Course,", lines[0]);
        Assert.Equal("Email,FirstName,LastName,Program,Year", lines[1]);
        Assert.Contains($"{student.Email},{student.FirstName},{student.LastName},{student.Program},{student.Year}", lines[2]);
    }

    [Fact]
    public async Task SystemSummaryCsv_MatchesContract()
    {
        var response = await _client.GetAsync("/Reports/SystemSummaryCsv");
        response.EnsureSuccessStatusCode();

        var csv = await response.Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Equal("Metric,Value", lines[0]);
        Assert.Contains("Students,", csv);
        Assert.Contains("Courses,", csv);
        Assert.Contains("Enrollments,", csv);
    }

    [Fact]
    public async Task GradebookCsv_HasContentTypeAndHeader()
    {
        var response = await _client.GetAsync("/Reports/GradebookCsv?courseId=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email,Name,Semester,Grade", text);
    }
}
