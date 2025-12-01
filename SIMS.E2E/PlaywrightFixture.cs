using System;
using System.Diagnostics;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace SIMS.E2E;

public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; }
    private Process? _appProcess;

    public PlaywrightFixture()
    {
        BaseUrl = Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "https://localhost:7146";
    }

    public async Task InitializeAsync()
    {
        await StartAppAsync();
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser != null) await Browser.CloseAsync();
        Playwright?.Dispose();
        if (_appProcess != null && !_appProcess.HasExited)
        {
            try { _appProcess.Kill(entireProcessTree: true); } catch { /* ignore */ }
        }
    }

    public Task<IBrowserContext> NewContextAsync() =>
        Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        });

    private async Task StartAppAsync()
    {
        // If user already runs the app, try to ping it first
        if (await IsUpAsync())
        {
            return;
        }

        var workingDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SIMS"));
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --urls https://localhost:7146;http://localhost:5260",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        _appProcess = Process.Start(psi);
        await WaitForAppAsync();
    }

    private async Task WaitForAppAsync()
    {
        var timeout = TimeSpan.FromSeconds(60);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (await IsUpAsync()) return;
            await Task.Delay(1000);
        }
        throw new TimeoutException($"App did not start within {timeout.TotalSeconds} seconds at {BaseUrl}");
    }

    private async Task<bool> IsUpAsync()
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            client.Timeout = TimeSpan.FromSeconds(5);
            var resp = await client.GetAsync("/");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
