using GiftOfTheGiversApp.Controllers;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GiftOfTheGiversApp.UnitTests.Controllers
{
    public class VolunteersControllerTests : IDisposable
    {
        private readonly VolunteersController _controller;
        private readonly ApplicationDbContext _context;
        private readonly ApplicationUser _testUser;

        public VolunteersControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;

            _context = new ApplicationDbContext(options);

            // Ensure database is created
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

            // Add user to context and save
            _context.Users.Add(_testUser);
            _context.SaveChanges();

            _controller = new VolunteersController(_context);

            // Setup user context with proper TempData
            SetupControllerContext("test-user-id");
        }

        private void SetupControllerContext(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            // Create HttpContext with proper TempData
            var httpContext = new DefaultHttpContext();
            httpContext.User = user;

            // Setup TempData
            var tempDataProvider = new MockTempDataProvider();
            var tempDataDictionary = new TempDataDictionary(httpContext, tempDataProvider);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            _controller.TempData = tempDataDictionary;
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesVolunteer()
        {
            try
            {
                // Arrange - Clear any existing volunteers for this user
                var existingVolunteers = _context.Volunteers.Where(v => v.UserId == "test-user-id");
                _context.Volunteers.RemoveRange(existingVolunteers);
                await _context.SaveChangesAsync();

                var viewModel = new VolunteerViewModel
                {
                    Skills = "First Aid, Logistics",
                    AvailabilityStatus = "Available",
                    Address = "123 Test St",
                    EmergencyContact = "123-456-7890"
                };

                // Clear ModelState completely
                _controller.ModelState.Clear();

                // Act
                var result = await _controller.Create(viewModel);

                // Debug: Check what's happening
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

                    // Check if there are any validation issues with the viewModel itself
                    var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(viewModel);
                    var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                    bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(viewModel, validationContext, validationResults, true);
                    Console.WriteLine($"ViewModel validation: {isValid}");
                    foreach (var validationResult in validationResults)
                    {
                        Console.WriteLine($"  Validation Error: {validationResult.ErrorMessage}");
                    }
                }

                // Assert - Should be redirect
                Assert.IsType<RedirectToActionResult>(result);
                var redirectResult = result as RedirectToActionResult;
                Assert.Equal("Details", redirectResult.ActionName);

                // Verify volunteer was created
                var volunteer = await _context.Volunteers
                    .FirstOrDefaultAsync(v => v.UserId == "test-user-id");
                Assert.NotNull(volunteer);
                Assert.Equal("First Aid, Logistics", volunteer.Skills);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        [Fact]
        public async Task Create_Post_ExistingVolunteer_RedirectsToDetails()
        {
            // Arrange - Clear any existing volunteers first
            var existingVolunteers = _context.Volunteers.Where(v => v.UserId == "test-user-id");
            _context.Volunteers.RemoveRange(existingVolunteers);
            await _context.SaveChangesAsync();

            // Create existing volunteer
            var existingVolunteer = new Volunteer
            {
                UserId = "test-user-id",
                Skills = "Existing Skills",
                AvailabilityStatus = "Available",
                Address = "Existing Address",
                EmergencyContact = "000-111-2222",
                DateRegistered = DateTime.Now
            };

            _context.Volunteers.Add(existingVolunteer);
            await _context.SaveChangesAsync();

            var existingVolunteerId = existingVolunteer.Id;

            var viewModel = new VolunteerViewModel
            {
                Skills = "New Skills",
                AvailabilityStatus = "Busy",
                Address = "New Address",
                EmergencyContact = "333-444-5555"
            };

            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Create(viewModel);

            // Debug if needed
            if (result is ViewResult viewResult)
            {
                Console.WriteLine("Unexpected ViewResult:");
                Console.WriteLine($"ModelState isValid: {_controller.ModelState.IsValid}");

                foreach (var key in _controller.ModelState.Keys)
                {
                    var state = _controller.ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"  {key}: {error.ErrorMessage}");
                    }
                }

                // Check TempData for messages
                if (_controller.TempData.ContainsKey("InfoMessage"))
                {
                    Console.WriteLine($"InfoMessage: {_controller.TempData["InfoMessage"]}");
                }
                if (_controller.TempData.ContainsKey("ErrorMessage"))
                {
                    Console.WriteLine($"ErrorMessage: {_controller.TempData["ErrorMessage"]}");
                }
            }

            // Assert - Should redirect to existing volunteer details
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(existingVolunteerId, redirectResult.RouteValues["id"]);

            // Verify no new volunteer was created
            var volunteerCount = await _context.Volunteers
                .CountAsync(v => v.UserId == "test-user-id");
            Assert.Equal(1, volunteerCount);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfVolunteers()
        {
            // Arrange - Clear existing data
            _context.Volunteers.RemoveRange(_context.Volunteers);
            await _context.SaveChangesAsync();

            // Create test volunteers with COMPLETE data
            var volunteer1 = new Volunteer
            {
                UserId = "test-user-id",
                Skills = "Test Skills 1",
                AvailabilityStatus = "Available",
                Address = "Address 1",
                EmergencyContact = "111-222-3333",
                DateRegistered = DateTime.Now
            };

            // Create another user for second volunteer
            var anotherUser = new ApplicationUser
            {
                Id = "another-user-id",
                UserName = "another@example.com",
                Email = "another@example.com",
                FirstName = "Another",
                LastName = "User"
            };
            _context.Users.Add(anotherUser);

            var volunteer2 = new Volunteer
            {
                UserId = "another-user-id",
                Skills = "Test Skills 2",
                AvailabilityStatus = "Busy",
                Address = "Address 2",
                EmergencyContact = "444-555-6666",
                DateRegistered = DateTime.Now
            };

            _context.Volunteers.AddRange(volunteer1, volunteer2);
            await _context.SaveChangesAsync();

            // Clear context to ensure fresh read
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Volunteer>>(viewResult.Model);
            var volunteerList = model.ToList();

            Assert.Equal(2, volunteerList.Count);
            Assert.Contains(volunteerList, v => v.Skills == "Test Skills 1");
            Assert.Contains(volunteerList, v => v.Skills == "Test Skills 2");
        }

        [Fact]
        public async Task Details_ReturnsView_WhenVolunteerExists()
        {
            // Arrange
            var volunteer = new Volunteer
            {
                UserId = "test-user-id",
                Skills = "Test Skills",
                AvailabilityStatus = "Available",
                Address = "123 Test Street",
                EmergencyContact = "444-555-6666",
                DateRegistered = DateTime.Now
            };

            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            var volunteerId = volunteer.Id;

            // Clear context to ensure fresh read
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Details(volunteerId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Volunteer>(viewResult.Model);
            Assert.Equal("Test Skills", model.Skills);
            Assert.Equal("Available", model.AvailabilityStatus);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenVolunteerDoesNotExist()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void User_FindFirstValue_WorksInTest()
        {
            // Act & Assert
            var userId = _controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Assert.Equal("test-user-id", userId);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var viewModel = new VolunteerViewModel
            {
                Skills = "",
                AvailabilityStatus = "",
                EmergencyContact = ""
            };

            // Manually add model error to simulate validation failure
            _controller.ModelState.AddModelError("Skills", "Skills are required");

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    // Mock TempData provider
    public class MockTempDataProvider : ITempDataProvider
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