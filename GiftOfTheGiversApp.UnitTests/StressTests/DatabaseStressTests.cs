using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;

namespace GiftOfTheGiversApp.UnitTests.StressTests
{
    public class DatabaseStressTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "StressTestDb_" + Guid.NewGuid())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Database_Concurrent_Create_Operations()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var concurrentTasks = 20;
            var operationsPerTask = 50;

            // Act & Assert
            var tasks = new List<Task>();

            for (int i = 0; i < concurrentTasks; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < operationsPerTask; j++)
                    {
                        var disaster = new Disaster
                        {
                            Name = $"Stress Test Disaster {Guid.NewGuid()}",
                            Location = "Test Location",
                            Description = "Stress Test Description",
                            DisasterType = "Flood",
                            SeverityLevel = "High",
                            Status = "Active",
                            ReportedById = "stress-test-user",
                            StartDate = DateTime.Now
                        };

                        context.Disasters.Add(disaster);
                        await context.SaveChangesAsync();

                        // Verify we can read it back
                        var retrieved = await context.Disasters
                            .FirstOrDefaultAsync(d => d.Name == disaster.Name);
                        Assert.NotNull(retrieved);
                    }
                }));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Verify total count
            var totalDisasters = await context.Disasters.CountAsync();
            Assert.Equal(concurrentTasks * operationsPerTask, totalDisasters);
        }

        [Fact]
        public async Task Database_Massive_Data_Insert_Performance()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var batchSize = 1000;
            var stopwatch = new System.Diagnostics.Stopwatch();

            // Act
            stopwatch.Start();

            for (int i = 0; i < batchSize; i++)
            {
                var volunteer = new Volunteer
                {
                    UserId = $"user-{i}",
                    Skills = $"Skills {i}",
                    AvailabilityStatus = "Available",
                    DateRegistered = DateTime.Now
                };

                context.Volunteers.Add(volunteer);

                // Save in batches to avoid memory issues
                if (i % 100 == 0)
                {
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear(); // Clear context to simulate fresh requests
                }
            }

            await context.SaveChangesAsync();
            stopwatch.Stop();

            // Assert
            var totalVolunteers = await context.Volunteers.CountAsync();
            Assert.Equal(batchSize, totalVolunteers);

            Console.WriteLine($"Inserted {batchSize} volunteers in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average time per insert: {stopwatch.ElapsedMilliseconds / (double)batchSize:F2}ms");

            // Performance assertion - adjust based on your requirements
            Assert.True(stopwatch.ElapsedMilliseconds < 5000,
                $"Mass insert took too long: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Database_Concurrent_Read_Write_Operations()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var initialDisasters = 100;
            var readerTasks = 10;
            var writerTasks = 5;

            // Add initial data
            for (int i = 0; i < initialDisasters; i++)
            {
                context.Disasters.Add(new Disaster
                {
                    Name = $"Initial Disaster {i}",
                    Location = "Location",
                    Description = "Description",
                    DisasterType = "Flood",
                    SeverityLevel = "Medium",
                    Status = "Active",
                    ReportedById = "system",
                    StartDate = DateTime.Now
                });
            }
            await context.SaveChangesAsync();

            var tasks = new List<Task>();
            var readCount = 0;
            var writeCount = 0;
            var lockObject = new object();

            // Act - Create reader tasks
            for (int i = 0; i < readerTasks; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var disasters = await context.Disasters.Take(10).ToListAsync();
                        lock (lockObject) readCount += disasters.Count;
                        await Task.Delay(10); // Simulate processing time
                    }
                }));
            }

            // Act - Create writer tasks
            for (int i = 0; i < writerTasks; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 5; j++)
                    {
                        var disaster = new Disaster
                        {
                            Name = $"Concurrent Disaster {Guid.NewGuid()}",
                            Location = "Location",
                            Description = "Description",
                            DisasterType = "Earthquake",
                            SeverityLevel = "High",
                            Status = "Active",
                            ReportedById = "concurrent-user",
                            StartDate = DateTime.Now
                        };

                        context.Disasters.Add(disaster);
                        await context.SaveChangesAsync();
                        lock (lockObject) writeCount++;
                    }
                }));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Assert
            Console.WriteLine($"Read operations: {readCount}");
            Console.WriteLine($"Write operations: {writeCount}");
            var totalDisasters = await context.Disasters.CountAsync();
            Console.WriteLine($"Total disasters in database: {totalDisasters}");

            Assert.True(readCount > 0);
            Assert.True(writeCount > 0);
            Assert.Equal(initialDisasters + writeCount, totalDisasters);
        }

        [Fact]
        public async Task Database_Transaction_Performance()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var transactionCount = 100;
            var stopwatch = new System.Diagnostics.Stopwatch();

            // Act
            stopwatch.Start();

            for (int i = 0; i < transactionCount; i++)
            {
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Create a disaster
                    var disaster = new Disaster
                    {
                        Name = $"Transaction Disaster {i}",
                        Location = "Location",
                        Description = "Description",
                        DisasterType = "Fire",
                        SeverityLevel = "Critical",
                        Status = "Active",
                        ReportedById = "transaction-user",
                        StartDate = DateTime.Now
                    };

                    context.Disasters.Add(disaster);
                    await context.SaveChangesAsync();

                    // Create a related volunteer
                    var volunteer = new Volunteer
                    {
                        UserId = $"transaction-user-{i}",
                        Skills = "Emergency Response",
                        AvailabilityStatus = "Available",
                        DateRegistered = DateTime.Now
                    };

                    context.Volunteers.Add(volunteer);
                    await context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            stopwatch.Stop();

            // Assert
            var totalDisasters = await context.Disasters.CountAsync();
            var totalVolunteers = await context.Volunteers.CountAsync();

            Console.WriteLine($"Completed {transactionCount} transactions in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average time per transaction: {stopwatch.ElapsedMilliseconds / (double)transactionCount:F2}ms");
            Console.WriteLine($"Total disasters: {totalDisasters}");
            Console.WriteLine($"Total volunteers: {totalVolunteers}");

            Assert.Equal(transactionCount, totalDisasters);
            Assert.Equal(transactionCount, totalVolunteers);
            Assert.True(stopwatch.ElapsedMilliseconds < 10000, "Transactions took too long");
        }
    }
}