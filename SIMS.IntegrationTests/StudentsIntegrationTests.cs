using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SIMS.IntegrationTests;

public class StudentsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StudentsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact(Skip = "Replaced by StudentControllerIntegrationTests suite")]
    public async Task Home_Index_ReturnsOk() => await Task.CompletedTask;

    [Fact(Skip = "Replaced by StudentControllerIntegrationTests suite")]
    public async Task Students_Index_AsAdmin_ReturnsOk() => await Task.CompletedTask;
}
