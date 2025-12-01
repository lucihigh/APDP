using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;

namespace SIMS.Services;

/// <summary>
/// Repairs existing Identity users so their Id matches the required BH/GV pattern.
/// </summary>
public static class UserIdRepairService
{
    public static async Task<int> FixStudentAndFacultyAsync(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        var students = await db.Students
            .Include(s => s.User)
            .Where(s => s.UserId != null)
            .ToListAsync();

        var fixedCount = 0;
        foreach (var student in students)
        {
            var user = student.User ?? await userManager.FindByIdAsync(student.UserId!);
            if (user == null) continue;

            var role = await userManager.IsInRoleAsync(user, "Faculty") ? "Faculty" : "Student";
            if (UserIdGenerator.IsFormatted(user.Id, role)) continue;

            var repaired = await RepairUserAsync(db, userManager, user, role, student);
            if (repaired != null) fixedCount++;
        }

        return fixedCount;
    }

    public static async Task<IdentityUser> RepairSingleStudentAsync(ApplicationDbContext db, UserManager<IdentityUser> userManager, IdentityUser user, string role)
    {
        var repaired = await RepairUserAsync(db, userManager, user, role, student: null);
        return repaired ?? user;
    }

    private static async Task<IdentityUser?> RepairUserAsync(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IdentityUser user,
        string role,
        SIMS.Models.Student? student)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var originalEmail = user.Email ?? user.UserName ?? $"{Guid.NewGuid():N}@local";
        var originalUserName = user.UserName ?? originalEmail;
        var roles = await userManager.GetRolesAsync(user);

        if (UserIdGenerator.IsFormatted(user.Id, role))
        {
            await tx.RollbackAsync();
            return user;
        }

        // Free email/username from the old record so we can reuse it.
        var placeholderEmail = $"archived+{Guid.NewGuid():N}@local";
        user.Email = placeholderEmail;
        user.UserName = placeholderEmail;
        user.NormalizedEmail = placeholderEmail.ToUpperInvariant();
        user.NormalizedUserName = placeholderEmail.ToUpperInvariant();

        var updateOld = await userManager.UpdateAsync(user);
        if (!updateOld.Succeeded)
        {
            await tx.RollbackAsync();
            return null;
        }

        var newId = await UserIdGenerator.GenerateForRoleAsync(userManager, role);
        var newUser = new IdentityUser
        {
            Id = newId,
            UserName = originalUserName,
            Email = originalEmail,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            SecurityStamp = user.SecurityStamp,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        var create = await userManager.CreateAsync(newUser);
        if (!create.Succeeded)
        {
            await tx.RollbackAsync();
            return null;
        }

        var defaultPassword = string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase)
            ? "Faculty#12345"
            : "Student#12345";
        var passwordAdd = await userManager.AddPasswordAsync(newUser, defaultPassword);
        if (!passwordAdd.Succeeded)
        {
            await tx.RollbackAsync();
            await userManager.DeleteAsync(newUser);
            return null;
        }

        if (roles.Count > 0)
        {
            await userManager.AddToRolesAsync(newUser, roles);
        }

        // Move notifications to the new user id.
        var notifications = await db.Notifications
            .Where(n => n.UserId == user.Id)
            .ToListAsync();
        foreach (var n in notifications)
        {
            n.UserId = newUser.Id;
        }

        if (student != null)
        {
            student.UserId = newUser.Id;
        }
        else
        {
            var linkedStudent = await db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (linkedStudent != null)
            {
                linkedStudent.UserId = newUser.Id;
            }
        }

        var delete = await userManager.DeleteAsync(user);
        if (!delete.Succeeded)
        {
            await tx.RollbackAsync();
            return null;
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return newUser;
    }
}
