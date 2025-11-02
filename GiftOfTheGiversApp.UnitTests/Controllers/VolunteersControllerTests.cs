using GiftOfTheGiversApp.Controllers;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GiftOfTheGiversApp.UnitTests.Controllers
{
    public class VolunteersControllerTests
    {
        private readonly VolunteersController _controller;
        private readonly ApplicationDbContext _context;

        public VolunteersControllerTests()
        {
            // Setup in-memory database directly
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            _controller = new VolunteersController(_context);

            // Setup user context manually
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesVolunteer()
        {
            // Arrange
            var viewModel = new VolunteerViewModel
            {
                Skills = "First Aid, Logistics",
                AvailabilityStatus = "Available",
                Address = "123 Test St",
                EmergencyContact = "123-456-7890"
            };

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            // Clear context and verify volunteer was created
            _context.ChangeTracker.Clear();
            var volunteer = await _context.Volunteers.FirstOrDefaultAsync();
            Assert.NotNull(volunteer);
            Assert.Equal("test-user-id", volunteer.UserId);
            Assert.Equal("First Aid, Logistics", volunteer.Skills);
            Assert.Equal("Available", volunteer.AvailabilityStatus);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfVolunteers()
        {
            // Arrange
            // First create a user for the volunteer
            var user = new ApplicationUser
            {
                Id = "user1",
                UserName = "user1@test.com",
                FirstName = "John",
                LastName = "Doe"
            };

            // Add user to context first
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var volunteer = new Volunteer
            {
                Id = 1,
                UserId = "user1",
                Skills = "Test Skills",
                AvailabilityStatus = "Available",
                DateRegistered = DateTime.Now
            };

            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            // Clear context to ensure fresh read
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Volunteer>>(viewResult.Model);
            var volunteerList = model.ToList();
            Assert.Single(volunteerList);

            // Note: The User navigation property might be null due to in-memory DB limitations
            // We're mainly testing that the controller returns the view with data
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
        public async Task Details_ReturnsView_WhenVolunteerExists()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user1",
                UserName = "user1@test.com",
                FirstName = "John",
                LastName = "Doe"
            };

            // Add user to context first
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var volunteer = new Volunteer
            {
                Id = 1,
                UserId = "user1",
                Skills = "Test Skills",
                AvailabilityStatus = "Available",
                DateRegistered = DateTime.Now
            };

            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            // Clear context to ensure fresh read
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Volunteer>(viewResult.Model);
            Assert.Equal("Test Skills", model.Skills);
            Assert.Equal("Available", model.AvailabilityStatus);
        }

        // Debug test to verify database operations work
        [Fact]
        public async Task Debug_DirectDatabaseOperations()
        {
            // Test basic database operations
            var volunteer = new Volunteer
            {
                Id = 1,
                UserId = "test-user",
                Skills = "Debug Skills",
                AvailabilityStatus = "Available",
                DateRegistered = DateTime.Now
            };

            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var retrieved = await _context.Volunteers.FindAsync(1);
            Assert.NotNull(retrieved);
            Assert.Equal("Debug Skills", retrieved.Skills);
        }
    }
}