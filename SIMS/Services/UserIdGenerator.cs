using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace SIMS.Services;

/// <summary>
/// Creates short user identifiers with role-based prefixes.
/// </summary>
public static class UserIdGenerator
{
    private const int MaxAttempts = 20;

    public static Task<string> GenerateForRoleAsync(UserManager<IdentityUser> userManager, string roleName) =>
        GenerateAsync(userManager, GetPrefixForRole(roleName));

    public static string GetPrefixForRole(string roleName) =>
        string.Equals(roleName, "Faculty", StringComparison.OrdinalIgnoreCase) ? "GV" : "BH";

    public static bool IsFormatted(string? userId, string roleName)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var prefix = GetPrefixForRole(roleName);
        return Regex.IsMatch(userId, $"^{prefix}\\d{{5}}$", RegexOptions.CultureInvariant);
    }

    private static async Task<string> GenerateAsync(UserManager<IdentityUser> userManager, string prefix)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = $"{prefix}{RandomNumberGenerator.GetInt32(0, 100000):D5}";
            if (await userManager.FindByIdAsync(candidate) == null)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique user id after multiple attempts.");
    }
}
