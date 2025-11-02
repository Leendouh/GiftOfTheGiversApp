using GiftOfTheGiversApp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GiftOfTheGiversApp.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Create a dedicated service collection for testing
            var testServices = new ServiceCollection();

            // Add minimal required services
            testServices.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
            });

            // Create a service provider for the test services
            var testServiceProvider = testServices.BuildServiceProvider();

            // Configure the main host builder
            builder.ConfigureServices(services =>
            {
                // Completely replace the service collection
                // This is a more aggressive approach
            });

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove any existing DbContext registrations
                var descriptors = services
                    .Where(d => d.ServiceType.Name.Contains("DbContext") ||
                               d.ServiceType.Namespace?.Contains("Microsoft.EntityFrameworkCore") == true)
                    .ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add our test DbContext
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });
            });
        }
    }
}