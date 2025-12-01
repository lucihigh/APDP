using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SIMS.IntegrationTests;

public class CourseEnrollmentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CourseEnrollmentIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task CoursesIndex_ReturnsOk()
    {
        var response = await _client.GetAsync("/Courses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EnrollmentsIndex_ReturnsOk()
    {
        var response = await _client.GetAsync("/Enrollments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EnrollmentsIndex_ContainsHeading()
    {
        var html = await _client.GetStringAsync("/Enrollments");
        Assert.Contains("Enrollments", html, StringComparison.OrdinalIgnoreCase);
    }
}
