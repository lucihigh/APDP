using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SIMS.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();

        // Remove orphaned user-role links to avoid FK violations when roles/users are recreated
        var existingUserIds = new HashSet<string>(await db.Users.Select(u => u.Id).ToListAsync(), StringComparer.OrdinalIgnoreCase);
        var orphanUserRoles = await db.UserRoles
            .Where(ur => !existingUserIds.Contains(ur.UserId))
            .ToListAsync();
        if (orphanUserRoles.Count > 0)
        {
            db.UserRoles.RemoveRange(orphanUserRoles);
            await db.SaveChangesAsync();
        }

        var roles = new[]
        {
            new IdentityRole { Id = RoleConstants.AdminId, Name = RoleConstants.AdminName, NormalizedName = RoleConstants.AdminName.ToUpperInvariant() },
            new IdentityRole { Id = RoleConstants.FacultyId, Name = RoleConstants.FacultyName, NormalizedName = RoleConstants.FacultyName.ToUpperInvariant() },
            new IdentityRole { Id = RoleConstants.StudentId, Name = RoleConstants.StudentName, NormalizedName = RoleConstants.StudentName.ToUpperInvariant() }
        };

        foreach (var role in roles)
        {
            var existing = await roleManager.FindByIdAsync(role.Id) ?? await roleManager.FindByNameAsync(role.Name);

            if (existing == null)
            {
                await roleManager.CreateAsync(role);
                continue;
            }

            var needsUpdate = false;
            if (!string.Equals(existing.Name, role.Name, StringComparison.OrdinalIgnoreCase))
            {
                existing.Name = role.Name;
                needsUpdate = true;
            }

            if (!string.Equals(existing.NormalizedName, role.NormalizedName, StringComparison.OrdinalIgnoreCase))
            {
                existing.NormalizedName = role.NormalizedName;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                existing.ConcurrencyStamp = Guid.NewGuid().ToString();
                await roleManager.UpdateAsync(existing);
            }
        }

        var adminEmail = "admin@sims.local";
        var adminUser = await userManager.FindByNameAsync("admin")
                         ?? await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminUser is null)
        {
            adminUser = new IdentityUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, "admin123");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else
        {
            // Ensure username/email and role are correct
            if (!string.Equals(adminUser.UserName, "admin", StringComparison.OrdinalIgnoreCase))
            {
                adminUser.UserName = "admin";
            }
            if (!string.Equals(adminUser.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                adminUser.Email = adminEmail;
            }
            adminUser.EmailConfirmed = true;
            await userManager.UpdateAsync(adminUser);
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            // Reset password to requested default
            var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            await userManager.ResetPasswordAsync(adminUser, token, "admin123");
        }

        // Remove legacy default faculty/student seed accounts if they still exist
        var legacyEmails = new[] { "faculty@sims.local", "student@sims.local" };
        foreach (var email in legacyEmails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id || s.Email == email);
                if (student != null)
                {
                    db.Students.Remove(student);
                }
                await userManager.DeleteAsync(user);
            }
        }

        if (!await db.Courses.AnyAsync())
        {
            db.Courses.AddRange(
                new Models.Course { Code = "CS101", Name = "Intro to Computer Science", Credits = 3, Department = "CS" },
                new Models.Course { Code = "CS201", Name = "Data Structures", Credits = 4, Department = "CS" },
                new Models.Course { Code = "MATH101", Name = "Calculus I", Credits = 4, Department = "Math" }
            );
            await db.SaveChangesAsync();
        }
    }
}
