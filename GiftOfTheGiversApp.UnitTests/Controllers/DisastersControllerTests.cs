using GiftOfTheGiversApp.Controllers;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.Services;
using GiftOfTheGiversApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace GiftOfTheGiversApp.UnitTests.Controllers
{
    public class DisastersControllerTests : IDisposable
    {
        private readonly DisastersController _controller;
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly PermissionService _permissionService;
        private readonly ApplicationUser _testUser;

        public DisastersControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // Create test user
            _testUser = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "test@example.com",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            _context.Users.Add(_testUser);
            _context.SaveChanges();

            // Setup Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _permissionService = new PermissionService(_mockUserManager.Object);

            // Setup user manager responses for admin user
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Admin" });

            _controller = CreateControllerWithUser("test-user-id", new[] { "Admin" });
        }

        private DisastersController CreateControllerWithUser(string userId, string[] roles = null, Action<Mock<UserManager<ApplicationUser>>> userManagerSetup = null)
        {
            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };

            // Add role claims
            if (roles != null)
            {
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            // Create HttpContext with proper setup
            var httpContext = new DefaultHttpContext();
            httpContext.User = user;

            // Setup TempData using a simple dictionary approach
            var tempDataDictionary = new TempDataDictionary(httpContext, new DisastersTempDataProvider());

            // Create a new UserManager mock for this specific controller
            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            // Setup default user manager responses
            var testUser = new ApplicationUser { Id = userId, UserName = "test@example.com" };
            userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(testUser);

            // Apply custom setup if provided
            userManagerSetup?.Invoke(userManagerMock);

            // If no custom setup, default to admin role
            if (userManagerSetup == null)
            {
                userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                    .ReturnsAsync(new List<string> { "Admin" });
            }

            var permissionService = new PermissionService(userManagerMock.Object);

            var controller = new DisastersController(_context, permissionService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = tempDataDictionary
            };

            return controller;
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesDisaster()
        {
            try
            {
                // Arrange
                var viewModel = new DisasterViewModel
                {
                    Name = "Test Disaster",
                    Location = "Test Location",
                    Description = "Test Description",
                    DisasterType = "Earthquake",
                    SeverityLevel = "High",
                    EstimatedAffected = 1000
                };

                // Clear ModelState
                _controller.ModelState.Clear();

                // Act
                var result = await _controller.Create(viewModel);

                // Debug if ViewResult
                if (result is ViewResult viewResult)
                {
                    Console.WriteLine("Unexpected ViewResult - ModelState Errors:");
                    foreach (var key in _controller.ModelState.Keys)
                    {
                        var state = _controller.ModelState[key];
                        foreach (var error in state.Errors)
                        {
                            Console.WriteLine($"  {key}: {error.ErrorMessage}");
                        }
                    }
                }

                // Assert - Should redirect to Details
                var redirectResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Details", redirectResult.ActionName);
                Assert.NotNull(redirectResult.RouteValues["id"]);

                // Verify disaster was created
                var disaster = await _context.Disasters
                    .FirstOrDefaultAsync(d => d.Name == "Test Disaster");
                Assert.NotNull(disaster);
                Assert.Equal("Test Disaster", disaster.Name);
                Assert.Equal("test-user-id", disaster.ReportedById);
                Assert.Equal("Active", disaster.Status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex}");
                throw;
            }
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var viewModel = new DisasterViewModel
            {
                // Missing required fields
                Name = "",
                Location = "",
                Description = ""
            };

            // Manually add model error
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Index_ReturnsView_WithDisasters()
        {
            // Arrange
            // Clear existing disasters
            _context.Disasters.RemoveRange(_context.Disasters);
            await _context.SaveChangesAsync();

            // Create test disasters
            var disaster1 = new Disaster
            {
                Name = "Disaster 1",
                Location = "Location 1",
                Description = "Description 1",
                DisasterType = "Flood",
                SeverityLevel = "High",
                ReportedById = "test-user-id",
                StartDate = DateTime.Now,
                Status = "Active"
            };

            var disaster2 = new Disaster
            {
                Name = "Disaster 2",
                Location = "Location 2",
                Description = "Description 2",
                DisasterType = "Earthquake",
                SeverityLevel = "Medium",
                ReportedById = "test-user-id",
                StartDate = DateTime.Now.AddDays(-1),
                Status = "Active"
            };

            _context.Disasters.AddRange(disaster1, disaster2);
            await _context.SaveChangesAsync();

            // Clear change tracker
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Disaster>>(viewResult.Model);
            var disasterList = model.ToList();

            Assert.Equal(2, disasterList.Count);
            Assert.Contains(disasterList, d => d.Name == "Disaster 1");
            Assert.Contains(disasterList, d => d.Name == "Disaster 2");
        }

        [Fact]
        public async Task Details_ReturnsView_WhenDisasterExists()
        {
            // Arrange
            var disaster = new Disaster
            {
                Name = "Test Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Fire",
                SeverityLevel = "High",
                ReportedById = "test-user-id",
                StartDate = DateTime.Now,
                Status = "Active"
            };

            _context.Disasters.Add(disaster);
            await _context.SaveChangesAsync();

            var disasterId = disaster.Id;

            // Clear change tracker
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Details(disasterId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Disaster>(viewResult.Model);
            Assert.Equal("Test Disaster", model.Name);
            Assert.Equal("Test Location", model.Location);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenDisasterDoesNotExist()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ReturnsView_WhenDisasterExists()
        {
            // Arrange
            var disaster = new Disaster
            {
                Name = "Edit Test Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Flood",
                SeverityLevel = "High",
                ReportedById = "test-user-id", // Same user as controller
                StartDate = DateTime.Now,
                Status = "Active"
            };

            _context.Disasters.Add(disaster);
            await _context.SaveChangesAsync();

            var disasterId = disaster.Id;

            // Clear change tracker
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Edit(disasterId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DisasterViewModel>(viewResult.Model);
            Assert.Equal("Edit Test Disaster", model.Name);
        }

        [Fact]
        public async Task Edit_Get_ReturnsRedirect_WhenUserNotAuthorized()
        {
            // Arrange - Create disaster with different user
            var disaster = new Disaster
            {
                Name = "Unauthorized Edit Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Flood",
                SeverityLevel = "High",
                ReportedById = "different-user-id", // Different user
                StartDate = DateTime.Now,
                Status = "Active"
            };

            _context.Disasters.Add(disaster);
            await _context.SaveChangesAsync();

            var disasterId = disaster.Id;

            // Create controller with non-admin user (regular User role)
            var nonAdminController = CreateControllerWithUser("test-user-id", new[] { "User" },
                userManagerMock =>
                {
                    // Setup for regular user (not admin)
                    userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                        .ReturnsAsync(new List<string> { "User" }); // Regular user, not admin
                });

            // Act
            var result = await nonAdminController.Edit(disasterId);

            // Debug info
            if (result is ViewResult viewResult)
            {
                Console.WriteLine("Got ViewResult instead of Redirect");
                Console.WriteLine($"ViewName: {viewResult.ViewName}");

                // Check if there's a TempData message
                if (nonAdminController.TempData.ContainsKey("ErrorMessage"))
                {
                    Console.WriteLine($"TempData ErrorMessage: {nonAdminController.TempData["ErrorMessage"]}");
                }

                // Check the actual permissions
                var permissions = await nonAdminController.GetPermissionsForCurrentUser(disaster.ReportedById);
                Console.WriteLine($"CanEditAllDisasters: {permissions.CanEditAllDisasters}");
                Console.WriteLine($"CanEditOwnDisasters: {permissions.CanEditOwnDisasters}");
            }

            // Assert - Should redirect to Details
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(disasterId, redirectResult.RouteValues["id"]);
        }

        [Fact]
        public async Task Edit_Get_ReturnsView_WhenUserIsAuthorized()
        {
            // Arrange - Create disaster with same user
            var disaster = new Disaster
            {
                Name = "Authorized Edit Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Flood",
                SeverityLevel = "High",
                ReportedById = "test-user-id", // Same user as controller
                StartDate = DateTime.Now,
                Status = "Active"
            };

            _context.Disasters.Add(disaster);
            await _context.SaveChangesAsync();

            var disasterId = disaster.Id;

            // Create controller with regular user role (should be able to edit their own disaster)
            var authorizedController = CreateControllerWithUser("test-user-id", new[] { "User" },
                userManagerMock =>
                {
                    userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                        .ReturnsAsync(new List<string> { "User" });
                });

            // Act
            var result = await authorizedController.Edit(disasterId);

            // Assert - Should return view for authorized user (editing their own disaster)
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DisasterViewModel>(viewResult.Model);
            Assert.Equal("Authorized Edit Disaster", model.Name);
        }

        [Fact]
        public async Task Resolve_Post_UpdatesStatus_ForAdmin()
        {
            // Arrange
            var disaster = new Disaster
            {
                Name = "Resolve Test Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Flood",
                SeverityLevel = "High",
                ReportedById = "test-user-id",
                StartDate = DateTime.Now,
                Status = "Active"
            };

            _context.Disasters.Add(disaster);
            await _context.SaveChangesAsync();

            var disasterId = disaster.Id;

            // Act
            var result = await _controller.Resolve(disasterId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(disasterId, redirectResult.RouteValues["id"]);

            // Verify status was updated
            var updatedDisaster = await _context.Disasters.FindAsync(disasterId);
            Assert.Equal("Resolved", updatedDisaster.Status);
        }

        [Fact]
        public void User_FindFirstValue_WorksInTest()
        {
            // Act & Assert
            var userId = _controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Assert.Equal("test-user-id", userId);
        }

        [Fact]
        public async Task Disaster_Creation_Logic_Works()
        {
            // Test the core disaster creation without the controller
            var disaster = new Disaster
            {
                Name = "Logic Test Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Flood",
                SeverityLevel = "High",
                Status = "Active",
                ReportedById = "test-user-id",
                StartDate = DateTime.Now
            };

            _context.Disasters.Add(disaster);
            await _context.SaveChangesAsync();

            var savedDisaster = await _context.Disasters.FirstOrDefaultAsync();
            Assert.NotNull(savedDisaster);
            Assert.Equal("Logic Test Disaster", savedDisaster.Name);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    // Extension method to check permissions for debugging
    public static class DisastersControllerExtensions
    {
        public static async Task<PermissionService.Permissions> GetPermissionsForCurrentUser(this DisastersController controller, string resourceOwnerId)
        {
            var permissionServiceProperty = controller.GetType().GetField("_permissionService",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (permissionServiceProperty != null)
            {
                var permissionService = permissionServiceProperty.GetValue(controller) as PermissionService;
                if (permissionService != null)
                {
                    return await permissionService.GetUserPermissionsAsync(controller.User, resourceOwnerId);
                }
            }

            return new PermissionService.Permissions();
        }
    }

    // Unique name for Disasters tests
    public class DisastersTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _tempData = new Dictionary<string, object>();

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return _tempData;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _tempData.Clear();
            if (values != null)
            {
                foreach (var item in values)
                {
                    _tempData[item.Key] = item.Value;
                }
            }
        }
    }
}