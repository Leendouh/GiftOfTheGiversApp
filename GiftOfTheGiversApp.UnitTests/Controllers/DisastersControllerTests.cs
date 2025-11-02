using GiftOfTheGiversApp.Controllers;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.Services;
using GiftOfTheGiversApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace GiftOfTheGiversApp.UnitTests.Controllers
{
    public class DisastersControllerTests
    {
        private readonly DisastersController _controller;
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly PermissionService _permissionService;

        public DisastersControllerTests()
        {
            // Setup in-memory database directly
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            // Create Mock UserManager manually
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _permissionService = new PermissionService(_mockUserManager.Object);

            // Setup user context FIRST, before creating controller
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = user };

            _controller = new DisastersController(_context, _permissionService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            // Setup mock user
            var mockUser = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "test@example.com",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(mockUser);
            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Admin" });
        }

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToDetails()
        {
            try
            {
                // Arrange
                var viewModel = new DisasterViewModel
                {
                    Name = "New Disaster",
                    Location = "Test Location",
                    Description = "Test Description",
                    DisasterType = "Earthquake",
                    SeverityLevel = "High",
                    EstimatedAffected = 1000
                };

                // Clear ModelState to ensure it's valid
                _controller.ModelState.Clear();

                Console.WriteLine($"ModelState is valid: {_controller.ModelState.IsValid}");
                Console.WriteLine($"User is authenticated: {_controller.User.Identity.IsAuthenticated}");
                Console.WriteLine($"User ID: {_controller.User.FindFirstValue(ClaimTypes.NameIdentifier)}");

                // Act
                var result = await _controller.Create(viewModel);

                // Debug: Check what we got
                Console.WriteLine($"Result type: {result?.GetType().Name}");

                if (result is ViewResult viewResult)
                {
                    Console.WriteLine("Got ViewResult instead of Redirect");
                    Console.WriteLine($"ViewName: {viewResult.ViewName}");
                    Console.WriteLine($"Model type: {viewResult.Model?.GetType().Name}");

                    // Check ModelState errors
                    foreach (var key in _controller.ModelState.Keys)
                    {
                        var state = _controller.ModelState[key];
                        foreach (var error in state.Errors)
                        {
                            Console.WriteLine($"ModelState Error - Key: {key}, Error: {error.ErrorMessage}");
                        }
                    }
                }
                else if (result is RedirectToActionResult redirectResult)
                {
                    Console.WriteLine($"Redirect to: {redirectResult.ActionName}, ID: {redirectResult.RouteValues?["id"]}");
                }

                // Assert - Should redirect to Details
                var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Details", redirectToActionResult.ActionName);
                Assert.NotNull(redirectToActionResult.RouteValues["id"]);

                // Verify disaster was created
                var disaster = await _context.Disasters.FirstOrDefaultAsync();
                Assert.NotNull(disaster);
                Assert.Equal("New Disaster", disaster.Name);
                Assert.Equal("test-user-id", disaster.ReportedById);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed with exception: {ex}");
                Console.WriteLine($"Inner exception: {ex.InnerException}");
                throw;
            }
        }

        [Fact]
        public async Task Create_Post_ValidModel_WithMockedUserFindFirst()
        {
            // This test uses a different approach to mock User.FindFirstValue

            // Arrange
            var viewModel = new DisasterViewModel
            {
                Name = "Mocked User Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Earthquake",
                SeverityLevel = "High"
            };

            // Create a mock user that properly implements FindFirstValue
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "mocked-user-id"),
                new Claim(ClaimTypes.Name, "mocked@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = user };
            var controllerContext = new ControllerContext { HttpContext = httpContext };

            // Create new controller instance with the mocked user context
            var testContext = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("TestDb_Create_" + Guid.NewGuid())
                    .Options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            var permissionService = new PermissionService(userManager.Object);

            var controller = new DisastersController(testContext, permissionService)
            {
                ControllerContext = controllerContext
            };

            // Act
            var result = await controller.Create(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            // Verify the disaster was created with the correct user ID
            var disaster = await testContext.Disasters.FirstOrDefaultAsync();
            Assert.NotNull(disaster);
            Assert.Equal("Mocked User Disaster", disaster.Name);
            Assert.Equal("mocked-user-id", disaster.ReportedById);
        }

        [Fact]
        public async Task Create_Post_ValidModel_AlternativeApproach()
        {
            // Alternative approach: Test the core logic directly

            // Arrange
            var viewModel = new DisasterViewModel
            {
                Name = "Direct Test Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Earthquake",
                SeverityLevel = "High"
            };

            // Create a mock HTTP context that properly supports FindFirstValue
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "direct-user-id"),
                new Claim(ClaimTypes.Name, "direct@example.com"),
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);

            // Use a real HttpContext with our claims principal
            var httpContext = new DefaultHttpContext();
            httpContext.User = user;

            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("TestDb_Direct_" + Guid.NewGuid())
                    .Options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            var permissionService = new PermissionService(userManager.Object);

            var controller = new DisastersController(context, permissionService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            // Clear ModelState
            controller.ModelState.Clear();

            // Act
            var result = await controller.Create(viewModel);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);

            var redirectResult = result as RedirectToActionResult;
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.NotNull(redirectResult.RouteValues["id"]);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var viewModel = new DisasterViewModel
            {
                Name = "" // Empty name should cause validation error
            };

            // Manually add model error to simulate validation failure
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        // Test to verify the User.FindFirstValue issue
        [Fact]
        public void User_FindFirstValue_WorksInTest()
        {
            // This test verifies that User.FindFirstValue works in our test setup
            var userId = _controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"User.FindFirstValue result: {userId ?? "NULL"}");

            Assert.NotNull(userId);
            Assert.Equal("test-user-id", userId);
        }

        // Test the actual disaster creation logic
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
    }
}