using GiftOfTheGiversApp.Services;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Security.Claims;

namespace GiftOfTheGiversApp.UnitTests.Services
{
    public class PermissionServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly PermissionService _permissionService;

        public PermissionServiceTests()
        {
            var store = Mock.Of<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null);

            _permissionService = new PermissionService(_mockUserManager.Object);
        }

        [Fact]
        public async Task GetUserPermissionsAsync_AdminUser_HasFullPermissions()
        {
            // Arrange
            var user = new ApplicationUser { Id = "1", UserName = "admin@test.com" };
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }));

            _mockUserManager.Setup(x => x.GetUserAsync(claims))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Admin" });

            // Act
            var permissions = await _permissionService.GetUserPermissionsAsync(claims);

            // Assert
            Assert.True(permissions.CanViewDisasters);
            Assert.True(permissions.CanCreateDisasters);
            Assert.True(permissions.CanEditAllDisasters);
            Assert.True(permissions.CanDeleteDisasters);
            Assert.True(permissions.CanManageUsers);
        }

        [Fact]
        public async Task GetUserPermissionsAsync_VolunteerUser_HasLimitedPermissions()
        {
            // Arrange
            var user = new ApplicationUser { Id = "1", UserName = "volunteer@test.com" };
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }));

            _mockUserManager.Setup(x => x.GetUserAsync(claims))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Volunteer" });

            // Act
            var permissions = await _permissionService.GetUserPermissionsAsync(claims, "1");

            // Assert
            Assert.True(permissions.CanViewDisasters);
            Assert.True(permissions.CanCreateDisasters);
            Assert.True(permissions.CanEditOwnDisasters);
            Assert.False(permissions.CanEditAllDisasters);
            Assert.False(permissions.CanDeleteDisasters);
        }
    }
}