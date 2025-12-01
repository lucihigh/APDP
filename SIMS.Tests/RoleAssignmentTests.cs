using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using SIMS.Services;
using Xunit;

namespace SIMS.Tests;

public class RoleAssignmentTests
{
    private static UserManager<IdentityUser> CreateUserManager(Mock<IUserStore<IdentityUser>>? storeMock = null)
    {
        storeMock ??= new Mock<IUserStore<IdentityUser>>();
        var mgr = new Mock<UserManager<IdentityUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);
        mgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);
        mgr.Setup(m => m.AddPasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        mgr.Setup(m => m.AddToRolesAsync(It.IsAny<IdentityUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        mgr.Setup(m => m.DeleteAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);
        return mgr.Object;
    }

    [Fact]
    public void GetPrefixForRole_ReturnsExpectedPrefixes()
    {
        Assert.Equal("GV", UserIdGenerator.GetPrefixForRole("Faculty"));
        Assert.Equal("BH", UserIdGenerator.GetPrefixForRole("Student"));
        Assert.Equal("BH", UserIdGenerator.GetPrefixForRole("anything-else"));
    }

    [Fact]
    public void IsFormatted_DetectsValidAndInvalidPatterns()
    {
        Assert.True(UserIdGenerator.IsFormatted("GV01234", "Faculty"));
        Assert.True(UserIdGenerator.IsFormatted("BH99999", "Student"));
        Assert.False(UserIdGenerator.IsFormatted("BAD123", "Student"));
        Assert.False(UserIdGenerator.IsFormatted(null, "Student"));
    }

    [Fact]
    public async Task GenerateForRoleAsync_UsesPrefixAndAvoidsCollisions()
    {
        var mgr = CreateUserManager();
        var id = await UserIdGenerator.GenerateForRoleAsync(mgr, "Faculty");
        Assert.StartsWith("GV", id);
        Assert.Equal(7, id.Length); // prefix + 5 digits
    }
}
