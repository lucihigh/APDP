
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
// If a hosting platform provides PORT, bind to it; otherwise let launchSettings/applicationUrl pick the port
var port = Environment.GetEnvironmentVariable("PORT");
var databaseProvider = builder.Configuration["DatabaseProvider"];
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
if (string.Equals(builder.Environment.EnvironmentName, "IntegrationTesting", StringComparison.OrdinalIgnoreCase))
{
    // Use shared in-memory SQLite for integration tests
    builder.Services.AddSingleton<SqliteConnection>(_ =>
    {
        var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");
        conn.Open();
        return conn;
    });
    builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    {
        var conn = sp.GetRequiredService<SqliteConnection>();
        options.UseSqlite(conn);
    });
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        if (string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            options.UseNpgsql(connectionString);
        }
        else
        {
            options.UseSqlServer(connectionString);
        }
    });
}
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    // Relax password rules for demo/admin default credentials
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddScoped<SIMS.Services.INotificationService, SIMS.Services.NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Seed roles and an admin user (skip during IntegrationTesting to avoid provider conflicts)
if (!string.Equals(app.Environment.EnvironmentName, "IntegrationTesting", StringComparison.OrdinalIgnoreCase))
{
    await SIMS.Data.SeedData.InitializeAsync(app.Services);
}
else
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    foreach (var role in new[] { "Admin", "Faculty", "Student" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
    var admin = await userManager.FindByNameAsync("admin");
    if (admin == null)
    {
        admin = new IdentityUser { UserName = "admin", Email = "admin@sims.local", EmailConfirmed = true };
        await userManager.CreateAsync(admin, "admin123");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    if (!db.Courses.Any())
    {
        db.Courses.AddRange(
            new SIMS.Models.Course { Code = "CS101", Name = "Intro to Computer Science", Credits = 3, Department = "CS" },
            new SIMS.Models.Course { Code = "CS201", Name = "Data Structures", Credits = 4, Department = "CS" },
            new SIMS.Models.Course { Code = "MATH101", Name = "Calculus I", Credits = 4, Department = "Math" }
        );
        await db.SaveChangesAsync();
    }
}

app.Run();

// Expose Program for WebApplicationFactory in tests
public partial class Program { }
