using System;
using SIMS.Services;
using Xunit;

namespace SIMS.Tests;

public class GeneralUtilityTests
{
    [Fact]
    public void StudentPasswordGenerator_RemovesDiacriticsAndBuildsPattern()
    {
        var pwd = StudentPasswordGenerator.Generate("Đỗ", "Thái", new DateOnly(2001, 12, 25));
        Assert.StartsWith("Thai", pwd);
        Assert.Contains("@2001", pwd);
    }

    [Fact]
    public void StudentPasswordGenerator_FallbacksWhenNameMissing()
    {
        var pwd = StudentPasswordGenerator.Generate("", "", new DateOnly(2000, 1, 1));
        Assert.StartsWith("Student#", pwd);
        Assert.Equal(16, pwd.Length); // Student# + 8 chars
    }
}
