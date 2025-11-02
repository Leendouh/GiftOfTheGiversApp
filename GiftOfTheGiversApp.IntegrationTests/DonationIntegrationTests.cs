using GiftOfTheGiversApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GiftOfTheGiversApp.Data;

namespace GiftOfTheGiversApp.IntegrationTests
{
    public class DonationIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceScope _scope;

        public DonationIntegrationTests()
        {
            // Create a fresh service provider for each test
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Create a new options instance using InMemory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("DonationIntegrationTestDb_" + Guid.NewGuid())
                .UseInternalServiceProvider(serviceProvider)
                .Options;

            _context = new ApplicationDbContext(options);

            // Ensure database is created
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task Donation_Create_Flow_IntegrationTest()
        {
            // Arrange - Create resource with all required properties
            var resource = new Resource
            {
                Id = 1,
                Name = "Test Resource",
                Description = "Test Description",
                UnitOfMeasure = "kg",
                CurrentQuantity = 100,
                ThresholdQuantity = 10
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            // Arrange - Create donation with all required properties
            var donation = new Donation
            {
                DonorId = "test-user",
                ResourceId = resource.Id,
                Quantity = 10,
                Status = "Pending",
                DonationDate = DateTime.Now,
                Notes = "Test donation"
            };

            _context.Donations.Add(donation);

            // Update resource quantity
            resource.CurrentQuantity += donation.Quantity;
            _context.Resources.Update(resource);

            await _context.SaveChangesAsync();

            // Act - Verify the flow
            var savedDonation = await _context.Donations
                .Include(d => d.Resource)
                .FirstOrDefaultAsync(d => d.ResourceId == resource.Id);

            // Assert
            Assert.NotNull(savedDonation);
            Assert.Equal(110, savedDonation.Resource.CurrentQuantity);
            Assert.Equal("Pending", savedDonation.Status);
        }

        [Fact]
        public async Task Donation_Create_ThroughControllerFlow_IntegrationTest()
        {
            // Arrange
            var resource = new Resource
            {
                Id = 2,
                Name = "Food Package",
                Description = "Emergency food supplies",
                UnitOfMeasure = "boxes",
                CurrentQuantity = 50,
                ThresholdQuantity = 5
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            // Act - Simulate donation creation (what the controller does)
            var initialResourceQuantity = resource.CurrentQuantity;
            var donationQuantity = 5;

            var donation = new Donation
            {
                DonorId = "integration-test-user",
                ResourceId = resource.Id,
                Quantity = donationQuantity,
                Status = "Pending",
                DonationDate = DateTime.Now,
                Notes = "Integration test donation"
            };

            _context.Donations.Add(donation);

            // Update resource (simulating what the controller does)
            resource.CurrentQuantity += donationQuantity;
            _context.Resources.Update(resource);

            await _context.SaveChangesAsync();

            // Assert
            var finalResource = await _context.Resources.FindAsync(resource.Id);
            var savedDonation = await _context.Donations
                .FirstOrDefaultAsync(d => d.DonorId == "integration-test-user");

            Assert.NotNull(finalResource);
            Assert.NotNull(savedDonation);
            Assert.Equal(initialResourceQuantity + donationQuantity, finalResource.CurrentQuantity);
            Assert.Equal("Pending", savedDonation.Status);
        }

        [Fact]
        public async Task Donations_ByUser_ReturnsCorrectDonations()
        {
            // Arrange
            var resource = new Resource
            {
                Id = 3,
                Name = "Medical Supplies",
                Description = "First aid kits",
                UnitOfMeasure = "kits",
                CurrentQuantity = 20,
                ThresholdQuantity = 2
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            // Create donations for different users
            var donations = new[]
            {
                new Donation
                {
                    Id = 1,
                    DonorId = "user1",
                    ResourceId = 3,
                    Quantity = 2,
                    Status = "Pending",
                    Notes = "User1 donation",
                    DonationDate = DateTime.Now
                },
                new Donation
                {
                    Id = 2,
                    DonorId = "user2",
                    ResourceId = 3,
                    Quantity = 3,
                    Status = "Pending",
                    Notes = "User2 donation",
                    DonationDate = DateTime.Now
                },
                new Donation
                {
                    Id = 3,
                    DonorId = "user1",
                    ResourceId = 3,
                    Quantity = 1,
                    Status = "Received",
                    Notes = "User1 second donation",
                    DonationDate = DateTime.Now
                }
            };

            _context.Donations.AddRange(donations);
            await _context.SaveChangesAsync();

            // Act - Get donations for user1
            var user1Donations = await _context.Donations
                .Where(d => d.DonorId == "user1")
                .ToListAsync();

            // Assert
            Assert.Equal(2, user1Donations.Count);
            Assert.All(user1Donations, d => Assert.Equal("user1", d.DonorId));
        }

        [Fact]
        public async Task Donation_ResourceQuantity_UpdatedCorrectly()
        {
            // Arrange
            var resource = new Resource
            {
                Id = 4,
                Name = "Blankets",
                Description = "Emergency blankets",
                UnitOfMeasure = "units",
                CurrentQuantity = 30,
                ThresholdQuantity = 5
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            var initialQuantity = resource.CurrentQuantity;

            // Act - Create multiple donations
            var donations = new[]
            {
                new Donation
                {
                    DonorId = "user1",
                    ResourceId = 4,
                    Quantity = 5,
                    Status = "Pending",
                    Notes = "First donation",
                    DonationDate = DateTime.Now
                },
                new Donation
                {
                    DonorId = "user2",
                    ResourceId = 4,
                    Quantity = 10,
                    Status = "Pending",
                    Notes = "Second donation",
                    DonationDate = DateTime.Now
                }
            };

            _context.Donations.AddRange(donations);

            // Update resource quantity for each donation
            foreach (var donation in donations)
            {
                resource.CurrentQuantity += donation.Quantity;
            }

            _context.Resources.Update(resource);
            await _context.SaveChangesAsync();

            // Assert
            var updatedResource = await _context.Resources.FindAsync(4);
            var totalDonations = await _context.Donations
                .Where(d => d.ResourceId == 4)
                .SumAsync(d => d.Quantity);

            Assert.NotNull(updatedResource);
            Assert.Equal(initialQuantity + totalDonations, updatedResource.CurrentQuantity);
            Assert.Equal(45, updatedResource.CurrentQuantity); // 30 + 5 + 10
        }

        [Fact]
        public async Task Donation_Status_CanBeUpdated()
        {
            // Arrange
            var donation = new Donation
            {
                DonorId = "test-user",
                ResourceId = 1,
                Quantity = 5,
                Status = "Pending",
                DonationDate = DateTime.Now,
                Notes = "Test donation"
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            // Act - Update status
            donation.Status = "Received";
            _context.Donations.Update(donation);
            await _context.SaveChangesAsync();

            // Assert
            var updatedDonation = await _context.Donations.FindAsync(donation.Id);
            Assert.NotNull(updatedDonation);
            Assert.Equal("Received", updatedDonation.Status);
        }

        [Fact]
        public async Task Donation_WithResourceCategory_IntegrationTest()
        {
            // Arrange - Create complete donation flow with category
            var category = new ResourceCategory
            {
                Id = 1,
                CategoryName = "Food & Water",
                Description = "Food and water supplies"
            };

            var resource = new Resource
            {
                Id = 5,
                Name = "Bottled Water",
                Description = "Purified drinking water",
                CategoryId = 1,
                UnitOfMeasure = "bottles",
                CurrentQuantity = 100,
                ThresholdQuantity = 20
            };

            var donation = new Donation
            {
                DonorId = "test-donor",
                ResourceId = 5,
                Quantity = 25,
                Status = "Pending",
                DonationDate = DateTime.Now,
                Notes = "Water donation for flood victims"
            };

            // Act
            _context.ResourceCategories.Add(category);
            _context.Resources.Add(resource);
            _context.Donations.Add(donation);

            // Update resource quantity
            resource.CurrentQuantity += donation.Quantity;

            await _context.SaveChangesAsync();

            // Assert - Verify complete flow
            var savedDonation = await _context.Donations
                .Include(d => d.Resource)
                .ThenInclude(r => r.Category)
                .FirstOrDefaultAsync(d => d.Id == donation.Id);

            Assert.NotNull(savedDonation);
            Assert.NotNull(savedDonation.Resource);
            Assert.NotNull(savedDonation.Resource.Category);
            Assert.Equal("Food & Water", savedDonation.Resource.Category.CategoryName);
            Assert.Equal(125, savedDonation.Resource.CurrentQuantity);
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
            _scope?.Dispose();
        }
    }
}