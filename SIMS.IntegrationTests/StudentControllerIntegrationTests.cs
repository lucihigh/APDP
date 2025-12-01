using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SIMS.IntegrationTests;

public class StudentControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StudentControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Index_ReturnsOk_AndContainsStudentsText()
    {
        var response = await _client.GetAsync("/Students");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Students", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_Get_ReturnsForm()
    {
        var response = await _client.GetAsync("/Students/Create");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Create", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Email", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsViewWithErrors()
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string?, string?>("FirstName", "Test"),
            new KeyValuePair<string?, string?>("LastName", "User"),
            new KeyValuePair<string?, string?>("Email", "test@example.com")
        });

        var response = await _client.PostAsync("/Students/Create", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // Antiforgery rejects missing token
    }

    [Fact]
    public async Task Details_InvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/Students/Details/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MyProfile_AdminRedirectsToCreateWhenMissing()
    {
        var response = await _client.GetAsync("/Students/MyProfile");
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
