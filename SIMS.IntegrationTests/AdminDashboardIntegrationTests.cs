using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SIMS.IntegrationTests;

public class AdminDashboardIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminDashboardIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Dashboard_ShowsTitle()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("Student Information Management System", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_ShowsMetrics()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("Students", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Courses", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Enrollments", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_ShowsRoleBadge()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("Role:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_HasNavigationLinks()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("Manage students", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Manage courses", html, StringComparison.OrdinalIgnoreCase);
    }
}
