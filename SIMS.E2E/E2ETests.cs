using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace SIMS.E2E;

public class E2ETests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;
    private const string AdminEmail = "admin@sims.local";
    private const string AdminPassword = "admin123";

    public E2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UnauthenticatedUserRedirectsToLogin()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("/Students");
        Assert.Contains("/Identity/Account/Login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticationE2ETests_AdminCanLogin()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await page.WaitForSelectorAsync("text=Student Information Management System");
        Assert.Contains("Dashboard", await page.TitleAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticationE2ETests_AdminLogout()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await context.ClearCookiesAsync();
        await page.GotoAsync("/Identity/Account/Login");
        await page.WaitForSelectorAsync("input[name='Input.Identifier']");
    }

    [Fact]
    public async Task AdminLoginFlowTests_LoginRedirectsToDashboard()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await page.WaitForSelectorAsync("text=Student Information Management System");
        Assert.Contains("/", page.Url);
    }

    [Fact]
    public async Task AdminLoginFlowTests_InvalidPasswordShowsError()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Identity/Account/Login");
        await page.FillAsync("input[name='Input.Identifier']", AdminEmail);
        await page.FillAsync("input[name='Input.Password']", "wrongpass");
        await page.ClickAsync("button[type='submit']");
        await page.WaitForSelectorAsync("text=Invalid login");
    }

    [Fact]
    public async Task NavigationAndUIFlowTests_NavLinksVisibleForAdmin()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await page.WaitForSelectorAsync("a:has-text('Manage students')");
        await page.WaitForSelectorAsync("a:has-text('Manage courses')");
        await page.WaitForSelectorAsync("a:has-text('Manage enrollments')");
    }

    [Fact]
    public async Task NavigationAndUIFlowTests_DashboardMetricsRender()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await page.WaitForSelectorAsync("text=Students");
        await page.WaitForSelectorAsync("text=Courses");
        await page.WaitForSelectorAsync("text=Enrollments");
    }

    [Fact]
    public async Task NavigationAndUIFlowTests_PrivacyPageAccessible()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Home/Privacy");
        await page.WaitForSelectorAsync("text=Privacy");
    }

    [Fact]
    public async Task StudentsPageE2ETests_AdminCanOpenStudentsIndex()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await page.ClickAsync("a:has-text('Manage students')");
        await page.WaitForURLAsync("**/Students");
        Assert.Contains("/Students", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StudentsPageE2ETests_StudentsIndexHasHeading()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await page.GotoAsync("/Students");
        await page.WaitForSelectorAsync("text=Students");
    }

    [Fact]
    public async Task ReportsE2ETests_ReportPageLoads()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await page.GotoAsync("/Reports");
        await page.WaitForSelectorAsync("text=Reports");
    }

    [Fact]
    public async Task ReportsE2ETests_CanClickSystemSummaryCsv()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsAdminAsync(page);
        await page.GotoAsync("/Reports");
        var response = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("a:has-text('System Summary CSV')"),
            resp => resp.Url.Contains("Reports/SystemSummaryCsv", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(200, response.Status);
    }

    private static async Task LoginAsAdminAsync(IPage page)
    {
        await page.GotoAsync("/Identity/Account/Login");
        await page.FillAsync("input[name='Input.Identifier']", AdminEmail);
        await page.FillAsync("input[name='Input.Password']", AdminPassword);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForSelectorAsync("text=Student Information Management System");
    }
}
