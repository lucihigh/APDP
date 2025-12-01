$ErrorActionPreference = 'Stop'

$projects = @(
    "SIMS.Tests/SIMS.Tests.csproj",
    "SIMS.IntegrationTests/SIMS.IntegrationTests.csproj",
    "SIMS.E2E/SIMS.E2E.csproj"
)

Write-Host "=== Running all test suites (unit, integration/contract, E2E) ===" -ForegroundColor Cyan

foreach ($proj in $projects) {
    if (-not (Test-Path $proj)) {
        Write-Warning "Skipped: $proj (file not found)"
        continue
    }
    Write-Host "`n--- dotnet test $proj ---" -ForegroundColor Yellow
    dotnet test $proj --no-build
}

Write-Host "`nAll test runs completed. Capture this terminal output for the combined summary screenshot." -ForegroundColor Green
