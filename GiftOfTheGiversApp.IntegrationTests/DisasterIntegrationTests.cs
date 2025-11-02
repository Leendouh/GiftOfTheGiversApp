using System.Net;
using System.Net.Http.Json;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GiftOfTheGiversApp.Data;

namespace GiftOfTheGiversApp.IntegrationTests
{
    public class DisasterIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public DisasterIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing database context registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("IntegrationTestDb");
                    });

                    // Configure any other test services here
                });
            });
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Disasters_Index_ReturnsSuccess()
        {
            // Act
            var response = await _client.GetAsync("/Disasters");

            // Assert - May redirect to login if not authenticated
            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.Redirect);
        }

        [Fact]
        public async Task Disasters_Create_Flow_IntegrationTest()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Arrange - Create a disaster directly in database
            var disaster = new Disaster
            {
                Name = "Integration Test Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Flood",
                SeverityLevel = "High",
                Status = "Active",
                ReportedById = "test-user",
                StartDate = DateTime.Now
            };

            context.Disasters.Add(disaster);
            await context.SaveChangesAsync();

            // Act - Retrieve the disaster
            var savedDisaster = await context.Disasters
                .FirstOrDefaultAsync(d => d.Name == "Integration Test Disaster");

            // Assert
            Assert.NotNull(savedDisaster);
            Assert.Equal("Integration Test Disaster", savedDisaster.Name);
            Assert.Equal("Test Location", savedDisaster.Location);
        }

        [Fact]
        public async Task Disasters_CRUD_Operations_WorkCorrectly()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Create
            var disaster = new Disaster
            {
                Name = "CRUD Test Disaster",
                Location = "Test Location",
                Description = "Test Description",
                DisasterType = "Earthquake",
                SeverityLevel = "Medium",
                Status = "Active",
                ReportedById = "test-user",
                StartDate = DateTime.Now
            };

            context.Disasters.Add(disaster);
            await context.SaveChangesAsync();

            // Read
            var retrieved = await context.Disasters
                .FirstOrDefaultAsync(d => d.Name == "CRUD Test Disaster");
            Assert.NotNull(retrieved);

            // Update
            retrieved.Location = "Updated Location";
            context.Disasters.Update(retrieved);
            await context.SaveChangesAsync();

            // Verify Update
            var updated = await context.Disasters.FindAsync(retrieved.Id);
            Assert.Equal("Updated Location", updated.Location);

            // Delete
            context.Disasters.Remove(updated);
            await context.SaveChangesAsync();

            // Verify Delete
            var deleted = await context.Disasters.FindAsync(retrieved.Id);
            Assert.Null(deleted);
        }
    }
}