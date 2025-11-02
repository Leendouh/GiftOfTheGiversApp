using Microsoft.AspNetCore.Mvc.Testing;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGiversApp.IntegrationTests.StressTests
{
    public class ApiStressTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ApiStressTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("StressTestDb");
                    });
                });
            });
        }

        [Fact]
        public async Task Multiple_Concurrent_API_Requests()
        {
            // Arrange
            var client = _factory.CreateClient();
            var concurrentRequests = 50;
            var tasks = new List<Task<HttpResponseMessage>>();
            var stopwatch = new System.Diagnostics.Stopwatch();

            // Act
            stopwatch.Start();

            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(client.GetAsync("/Disasters"));
            }

            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successfulResponses = responses.Count(r => r.IsSuccessStatusCode || r.StatusCode == System.Net.HttpStatusCode.Redirect);
            var failedResponses = responses.Count(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.Redirect);

            Console.WriteLine($"Concurrent Requests: {concurrentRequests}");
            Console.WriteLine($"Total Time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Successful: {successfulResponses}");
            Console.WriteLine($"Failed: {failedResponses}");
            Console.WriteLine($"Average Response Time: {stopwatch.ElapsedMilliseconds / (double)concurrentRequests:F2}ms");

            // Should handle at least 80% of requests successfully
            Assert.True(successfulResponses >= concurrentRequests * 0.8,
                $"Only {successfulResponses}/{concurrentRequests} requests succeeded");

            // Should complete within reasonable time
            Assert.True(stopwatch.ElapsedMilliseconds < 10000,
                $"Requests took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}