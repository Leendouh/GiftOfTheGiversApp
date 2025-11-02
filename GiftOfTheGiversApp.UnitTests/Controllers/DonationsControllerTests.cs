using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Controllers;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GiftOfTheGiversApp.UnitTests.Controllers
{
    public class DonationsControllerTests : IDisposable
    {
        private readonly DonationsController _controller;
        private readonly ApplicationDbContext _context;

        public DonationsControllerTests()
        {
            // Setup in-memory database with proper configuration
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_Donations_" + Guid.NewGuid())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ApplicationDbContext(options);

            // Initialize database before creating controller
            InitializeDatabase().Wait();

            _controller = new DonationsController(_context);

            // Setup default user context
            SetupUserContext("test-user-id", "test@example.com");
        }

        private async Task InitializeDatabase()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();

            // Always add a test resource
            var resource = new Resource
            {
                Id = 1,
                Name = "Water",
                Description = "Test Water",
                UnitOfMeasure = "liters",
                CurrentQuantity = 100
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();
        }

        private void SetupUserContext(string userId, string email, string role = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, email)
            };

            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfDonations()
        {
            // Arrange
            SetupUserContext("admin-user-id", "admin@example.com", "Admin");

            // Clear and add fresh donation
            _context.Donations.RemoveRange(_context.Donations);
            await _context.SaveChangesAsync();

            var donation = new Donation
            {
                Id = 1,
                DonorId = "test-user-id",
                ResourceId = 1,
                Quantity = 5,
                Status = "Pending",
                Notes = "Test notes",
                DonationDate = DateTime.Now
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            // Clear change tracker
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Donation>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Donations_ReturnsUserSpecificDonations()
        {
            // Arrange
            // Clear any existing data
            _context.Donations.RemoveRange(_context.Donations);
            await _context.SaveChangesAsync();

            var currentUserDonation = new Donation
            {
                Id = 1,
                DonorId = "test-user-id", // Current user
                ResourceId = 1,
                Quantity = 5,
                Status = "Pending",
                Notes = "Test notes",
                DonationDate = DateTime.Now
            };

            var otherUserDonation = new Donation
            {
                Id = 2,
                DonorId = "other-user-id", // Different user
                ResourceId = 1,
                Quantity = 3,
                Status = "Pending",
                Notes = "Test notes",
                DonationDate = DateTime.Now
            };

            _context.Donations.Add(currentUserDonation);
            _context.Donations.Add(otherUserDonation);
            await _context.SaveChangesAsync();

            // Clear change tracker
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Donations();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Donation>>(viewResult.Model);
            Assert.Single(model);
            Assert.All(model, d => Assert.Equal("test-user-id", d.DonorId));
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenDonationNotFound()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsView_WhenDonationExists()
        {
            // Arrange
            // Clear and add fresh donation
            _context.Donations.RemoveRange(_context.Donations);
            await _context.SaveChangesAsync();

            var donation = new Donation
            {
                Id = 1,
                DonorId = "test-user-id", // Must match current user
                ResourceId = 1,
                Quantity = 5,
                Status = "Pending",
                Notes = "Test notes",
                DonationDate = DateTime.Now
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            // Clear change tracker
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Donation>(viewResult.Model);
            Assert.Equal(1, model.Id);
        }

        [Fact]
        public async Task Create_Get_ReturnsViewWithResources()
        {
            // Arrange - Ensure resources exist
            _context.Resources.RemoveRange(_context.Resources);
            await _context.SaveChangesAsync();

            var resource = new Resource
            {
                Id = 1,
                Name = "Water",
                Description = "Test",
                UnitOfMeasure = "kg",
                CurrentQuantity = 100
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DonationCreateViewModel>(viewResult.Model);
            Assert.Single(model.Resources);
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesDonationAndUpdatesResource()
        {
            // Arrange
            _context.Resources.RemoveRange(_context.Resources);
            _context.Donations.RemoveRange(_context.Donations);
            await _context.SaveChangesAsync();

            var resource = new Resource
            {
                Id = 1,
                Name = "Water",
                Description = "Test",
                UnitOfMeasure = "kg",
                CurrentQuantity = 100
            };
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            var viewModel = new DonationCreateViewModel
            {
                ResourceId = 1,
                Quantity = 10,
                Notes = "Test donation"
            };

            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Donations", redirectResult.ActionName);

            // Verify donation was created
            var donation = await _context.Donations.FirstOrDefaultAsync();
            Assert.NotNull(donation);
            Assert.Equal("test-user-id", donation.DonorId);
            Assert.Equal("Pending", donation.Status);

            // Verify resource quantity was updated
            var updatedResource = await _context.Resources.FindAsync(1);
            Assert.Equal(110, updatedResource.CurrentQuantity);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithResources()
        {
            // Arrange
            // Clear existing resources first
            _context.Resources.RemoveRange(_context.Resources);
            await _context.SaveChangesAsync();

            var resource = new Resource
            {
                Id = 1,
                Name = "Water",
                Description = "Test",
                UnitOfMeasure = "kg",
                CurrentQuantity = 100
            };
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            // Clear change tracker to avoid conflicts
            _context.ChangeTracker.Clear();

            var viewModel = new DonationCreateViewModel
            {
                ResourceId = 1,
                Quantity = 0, // Invalid quantity
                Notes = "Test donation"
            };

            // Manually add model error to simulate validation failure
            _controller.ModelState.AddModelError("Quantity", "Quantity must be at least 1");

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DonationCreateViewModel>(viewResult.Model);
            Assert.NotEmpty(model.Resources);
        }

        [Fact]
        public async Task UpdateStatus_AsAdmin_UpdatesDonationStatus()
        {
            // Arrange
            SetupUserContext("admin-user-id", "admin@example.com", "Admin");

            _context.Donations.RemoveRange(_context.Donations);
            await _context.SaveChangesAsync();

            var donation = new Donation
            {
                Id = 1,
                DonorId = "test-user-id",
                ResourceId = 1,
                Quantity = 5,
                Status = "Pending",
                Notes = "Test notes",
                DonationDate = DateTime.Now
            };
            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.UpdateStatus(1, "Received");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedDonation = await _context.Donations.FindAsync(1);
            Assert.Equal("Received", updatedDonation.Status);
        }

        [Fact]
        public async Task UpdateStatus_NonExistentDonation_ReturnsNotFound()
        {
            // Arrange - Setup admin user
            SetupUserContext("admin-user-id", "admin@example.com", "Admin");

            // Act
            var result = await _controller.UpdateStatus(999, "Received");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_AsAdmin_ReturnsView()
        {
            // Arrange
            SetupUserContext("admin-user-id", "admin@example.com", "Admin");

            _context.Donations.RemoveRange(_context.Donations);
            await _context.SaveChangesAsync();

            var donation = new Donation
            {
                Id = 1,
                DonorId = "test-user-id",
                ResourceId = 1,
                Quantity = 5,
                Status = "Pending",
                Notes = "Test notes",
                DonationDate = DateTime.Now
            };
            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Donation>(viewResult.Model);
            Assert.Equal(1, model.Id);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesDonation()
        {
            // Arrange
            SetupUserContext("admin-user-id", "admin@example.com", "Admin");

            _context.Donations.RemoveRange(_context.Donations);
            await _context.SaveChangesAsync();

            var originalDonation = new Donation
            {
                Id = 1,
                DonorId = "test-user-id",
                ResourceId = 1,
                Quantity = 5,
                Status = "Pending",
                Notes = "Original notes",
                DonationDate = DateTime.Now
            };
            _context.Donations.Add(originalDonation);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var updatedDonation = new Donation
            {
                Id = 1,
                DonorId = "test-user-id",
                ResourceId = 1,
                Quantity = 8,
                Status = "Received",
                Notes = "Updated notes",
                DonationDate = DateTime.Now
            };

            // Act
            var result = await _controller.Edit(1, updatedDonation);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var savedDonation = await _context.Donations.FindAsync(1);
            Assert.Equal("Received", savedDonation.Status);
            Assert.Equal("Updated notes", savedDonation.Notes);
            Assert.Equal(8, savedDonation.Quantity);
        }

        [Fact]
        public async Task DonationExistence_CanBeVerifiedThroughContext()
        {
            // Arrange
            _context.Donations.RemoveRange(_context.Donations);
            await _context.SaveChangesAsync();

            var donation = new Donation
            {
                Id = 1,
                DonorId = "test-user-id",
                ResourceId = 1,
                Quantity = 5,
                Status = "Pending",
                Notes = "Test notes",
                DonationDate = DateTime.Now
            };
            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            // Act & Assert
            Assert.True(await _context.Donations.AnyAsync(e => e.Id == 1));
            Assert.False(await _context.Donations.AnyAsync(e => e.Id == 999));
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }
    }
}