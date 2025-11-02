using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.Data;

namespace GiftOfTheGiversApp.UnitTests.TestHelpers
{
    public static class TestHelpers
    {
        public static ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;

            return new ApplicationDbContext(options);
        }

        public static ApplicationDbContext CreateInMemoryDbContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            return new ApplicationDbContext(options);
        }

        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());

            // Setup common methods
            mgr.Setup(x => x.DeleteAsync(It.IsAny<TUser>()))
                .ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.CreateAsync(It.IsAny<TUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.UpdateAsync(It.IsAny<TUser>()))
                .ReturnsAsync(IdentityResult.Success);

            return mgr;
        }

        public static ClaimsPrincipal CreateClaimsPrincipal(string userId, string email, string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        public static ApplicationUser CreateTestUser(string id = "test-user-id", string email = "test@example.com")
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User"
            };
        }

        public static Disaster CreateTestDisaster(int id = 1, string reportedById = "test-user-id")
        {
            return new Disaster
            {
                Id = id,
                Name = "Test Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Flood",
                SeverityLevel = "High",
                Status = "Active",
                ReportedById = reportedById,
                StartDate = DateTime.Now
            };
        }

        public static Volunteer CreateTestVolunteer(int id = 1, string userId = "test-user-id")
        {
            return new Volunteer
            {
                Id = id,
                UserId = userId,
                Skills = "Test Skills",
                AvailabilityStatus = "Available",
                DateRegistered = DateTime.Now
            };
        }
    }
}