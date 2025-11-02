using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;

namespace GiftOfTheGiversApp.UnitTests.StressTests
{
    public class DatabaseStressTests
    {
        private ApplicationDbContext CreateInMemoryDbContext(string databaseName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName ?? "StressTestDb_" + Guid.NewGuid())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Database_Concurrent_Create_Operations()
        {
            // Arrange - Use a shared database name so all contexts use the same in-memory database
            var sharedDatabaseName = "ConcurrentCreateDb_" + Guid.NewGuid();
            var concurrentTasks = 10;
            var operationsPerTask = 20;
            var tasks = new List<Task<List<string>>>(); // Return created disaster names

            for (int i = 0; i < concurrentTasks; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var createdNames = new List<string>();
                    // Each task gets its own context but shares the same database
                    using var taskContext = CreateInMemoryDbContext(sharedDatabaseName);

                    for (int j = 0; j < operationsPerTask; j++)
                    {
                        var disasterName = $"Stress Test Disaster {Guid.NewGuid()}";
                        var disaster = new Disaster
                        {
                            Name = disasterName,
                            Location = "Test Location",
                            Description = "Stress Test Description",
                            DisasterType = "Flood",
                            SeverityLevel = "High",
                            Status = "Active",
                            ReportedById = "stress-test-user",
                            StartDate = DateTime.Now
                        };

                        taskContext.Disasters.Add(disaster);
                        await taskContext.SaveChangesAsync();
                        createdNames.Add(disasterName);
                    }

                    return createdNames;
                }));
            }

            // Wait for all tasks to complete and get results
            var results = await Task.WhenAll(tasks);

            // Verify using a new context connected to the same database
            using var verificationContext = CreateInMemoryDbContext(sharedDatabaseName);
            var totalDisasters = await verificationContext.Disasters.CountAsync();
            var expectedTotal = concurrentTasks * operationsPerTask;

            Console.WriteLine($"=== Concurrent Create Results ===");
            Console.WriteLine($"Expected: {expectedTotal}, Actual: {totalDisasters}");
            Console.WriteLine($"Tasks completed: {results.Length}");
            Console.WriteLine($"Total created names reported: {results.Sum(r => r.Count)}");

            Assert.Equal(expectedTotal, totalDisasters);
        }

        [Fact]
        public async Task Database_Massive_Data_Insert_Performance()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var batchSize = 500;
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
                    Address = "123 Test Street", // Required field
                    EmergencyContact = "123-456-7890", // Required field
                    DateRegistered = DateTime.Now
                };

                context.Volunteers.Add(volunteer);

                // Save in batches to avoid memory issues
                if (i % 50 == 0 && i > 0)
                {
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                    Console.WriteLine($"Saved batch up to {i} volunteers");
                }
            }

            // Save any remaining entities
            await context.SaveChangesAsync();
            stopwatch.Stop();

            // Assert
            var totalVolunteers = await context.Volunteers.CountAsync();

            Console.WriteLine($"=== Massive Insert Results ===");
            Console.WriteLine($"Inserted {batchSize} volunteers in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average time per insert: {stopwatch.ElapsedMilliseconds / (double)batchSize:F2}ms");
            Console.WriteLine($"Total volunteers in database: {totalVolunteers}");

            Assert.Equal(batchSize, totalVolunteers);
            Assert.True(stopwatch.ElapsedMilliseconds < 10000,
                $"Mass insert took too long: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Database_Concurrent_Read_Write_Operations()
        {
            // Arrange - Use shared database for initial data
            var sharedDatabaseName = "ReadWriteDb_" + Guid.NewGuid();

            // Create initial data in a separate context
            using var initialContext = CreateInMemoryDbContext(sharedDatabaseName);
            var initialDisasters = 50;

            for (int i = 0; i < initialDisasters; i++)
            {
                initialContext.Disasters.Add(new Disaster
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
            await initialContext.SaveChangesAsync();

            var tasks = new List<Task>();
            var readCount = 0;
            var writeCount = 0;
            var lockObject = new object();

            // Act - Create reader tasks (each gets its own context)
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var readContext = CreateInMemoryDbContext(sharedDatabaseName);
                    for (int j = 0; j < 5; j++)
                    {
                        try
                        {
                            var disasters = await readContext.Disasters.Take(5).ToListAsync();
                            lock (lockObject)
                            {
                                readCount += disasters.Count;
                                Console.WriteLine($"Reader found {disasters.Count} disasters, total reads: {readCount}");
                            }
                            await Task.Delay(10);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Read error: {ex.Message}");
                        }
                    }
                }));
            }

            // Act - Create writer tasks (each gets its own context)
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var writerContext = CreateInMemoryDbContext(sharedDatabaseName);
                    for (int j = 0; j < 3; j++)
                    {
                        try
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

                            writerContext.Disasters.Add(disaster);
                            await writerContext.SaveChangesAsync();

                            lock (lockObject)
                            {
                                writeCount++;
                                Console.WriteLine($"Writer created disaster, total writes: {writeCount}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Write error: {ex.Message}");
                        }
                    }
                }));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Verify final state with a new context
            using var finalContext = CreateInMemoryDbContext(sharedDatabaseName);
            var totalDisasters = await finalContext.Disasters.CountAsync();
            var expectedWrites = 3 * 3; // 3 writers * 3 writes each

            Console.WriteLine($"=== Concurrent Read/Write Results ===");
            Console.WriteLine($"Read operations processed: {readCount}");
            Console.WriteLine($"Write operations completed: {writeCount}");
            Console.WriteLine($"Expected writes: {expectedWrites}");
            Console.WriteLine($"Initial disasters: {initialDisasters}");
            Console.WriteLine($"Total disasters in database: {totalDisasters}");
            Console.WriteLine($"Expected total: {initialDisasters + expectedWrites}");

            // Assert
            Assert.True(readCount > 0, $"No read operations completed. Read count: {readCount}");
            Assert.True(writeCount > 0, $"No write operations completed. Write count: {writeCount}");
            Assert.Equal(expectedWrites, writeCount);
            Assert.Equal(initialDisasters + expectedWrites, totalDisasters);
        }

        [Fact]
        public async Task Database_Query_Performance_Under_Load()
        {
            // Arrange - Use shared database
            var sharedDatabaseName = "QueryPerfDb_" + Guid.NewGuid();
            using var setupContext = CreateInMemoryDbContext(sharedDatabaseName);

            var dataSize = 500; // Reduced for stability
            var queryCount = 50;

            // Pre-populate with test data
            for (int i = 0; i < dataSize; i++)
            {
                setupContext.Disasters.Add(new Disaster
                {
                    Name = $"Query Test Disaster {i}",
                    Location = $"Location {i % 10}",
                    Description = "Description for performance testing",
                    DisasterType = i % 2 == 0 ? "Flood" : "Earthquake",
                    SeverityLevel = i % 3 == 0 ? "High" : (i % 3 == 1 ? "Medium" : "Low"),
                    Status = "Active",
                    ReportedById = "query-user",
                    StartDate = DateTime.Now.AddDays(-i % 30) // Spread over 30 days
                });
            }
            await setupContext.SaveChangesAsync();

            var stopwatch = new System.Diagnostics.Stopwatch();
            var successfulQueries = 0;
            var lockObj = new object();

            // Act
            stopwatch.Start();

            var tasks = new List<Task>();
            for (int i = 0; i < queryCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var queryContext = CreateInMemoryDbContext(sharedDatabaseName);
                    try
                    {
                        // Different types of queries to simulate real usage
                        var floodCount = await queryContext.Disasters
                            .Where(d => d.DisasterType == "Flood")
                            .CountAsync();

                        var highSeverity = await queryContext.Disasters
                            .Where(d => d.SeverityLevel == "High")
                            .OrderByDescending(d => d.StartDate)
                            .Take(5)
                            .ToListAsync();

                        var recentDisasters = await queryContext.Disasters
                            .Where(d => d.StartDate > DateTime.Now.AddDays(-7))
                            .ToListAsync();

                        var byLocation = await queryContext.Disasters
                            .Where(d => d.Location.Contains("Location 1"))
                            .CountAsync();

                        lock (lockObj)
                        {
                            successfulQueries++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Query failed: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Console.WriteLine($"=== Query Performance Results ===");
            Console.WriteLine($"Total queries attempted: {queryCount}");
            Console.WriteLine($"Successful queries: {successfulQueries}");
            Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average time per query: {stopwatch.ElapsedMilliseconds / (double)queryCount:F2}ms");
            Console.WriteLine($"Queries per second: {queryCount / (stopwatch.ElapsedMilliseconds / 1000.0):F2}");

            Assert.True(successfulQueries >= queryCount * 0.8,
                $"Only {successfulQueries}/{queryCount} queries succeeded");
            Assert.True(stopwatch.ElapsedMilliseconds < 5000,
                $"Queries took too long: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Database_Connection_Stress_Test()
        {
            // Test creating and disposing many contexts
            var sharedDatabaseName = "ConnectionTestDb_" + Guid.NewGuid();
            var contextCount = 30; // Reduced for stability
            var tasks = new List<Task>();
            var successfulOperations = 0;
            var lockObj = new object();

            // Add some initial data
            using var initialContext = CreateInMemoryDbContext(sharedDatabaseName);
            initialContext.Disasters.Add(new Disaster
            {
                Name = "Initial Disaster",
                Location = "Test Location",
                Description = "Initial data",
                DisasterType = "Flood",
                SeverityLevel = "Medium",
                Status = "Active",
                ReportedById = "initial-user",
                StartDate = DateTime.Now
            });
            await initialContext.SaveChangesAsync();

            for (int i = 0; i < contextCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var context = CreateInMemoryDbContext(sharedDatabaseName);

                        // Perform read operation
                        var count = await context.Disasters.CountAsync();

                        // Perform write operation
                        var disaster = new Disaster
                        {
                            Name = $"Connection Test {Guid.NewGuid()}",
                            Location = "Test Location",
                            Description = "Connection stress test",
                            DisasterType = "Earthquake",
                            SeverityLevel = "High",
                            Status = "Active",
                            ReportedById = "connection-user",
                            StartDate = DateTime.Now
                        };

                        context.Disasters.Add(disaster);
                        await context.SaveChangesAsync();

                        lock (lockObj)
                        {
                            successfulOperations++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Connection operation failed: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Verify final state
            using var verificationContext = CreateInMemoryDbContext(sharedDatabaseName);
            var totalDisasters = await verificationContext.Disasters.CountAsync();
            var expectedDisasters = 1 + contextCount; // Initial + one per operation

            Console.WriteLine($"=== Connection Stress Test Results ===");
            Console.WriteLine($"Attempted operations: {contextCount}");
            Console.WriteLine($"Successful operations: {successfulOperations}");
            Console.WriteLine($"Success rate: {(successfulOperations * 100.0 / contextCount):F2}%");
            Console.WriteLine($"Expected disasters: {expectedDisasters}");
            Console.WriteLine($"Actual disasters: {totalDisasters}");

            Assert.True(successfulOperations >= contextCount * 0.8,
                $"Only {successfulOperations}/{contextCount} operations succeeded");
            Assert.Equal(expectedDisasters, totalDisasters);
        }

        [Fact]
        public async Task Database_Mixed_Workload_Stress_Test()
        {
            // Simulate mixed read/write workload
            var sharedDatabaseName = "MixedWorkloadDb_" + Guid.NewGuid();
            var operationCount = 100;
            var tasks = new List<Task>();
            var successfulOperations = 0;
            var lockObj = new object();

            // Add initial data
            using var initialContext = CreateInMemoryDbContext(sharedDatabaseName);
            for (int i = 0; i < 100; i++)
            {
                initialContext.Disasters.Add(new Disaster
                {
                    Name = $"Base Disaster {i}",
                    Location = $"City {i % 5}",
                    Description = "Base data for mixed workload test",
                    DisasterType = i % 3 == 0 ? "Flood" : (i % 3 == 1 ? "Earthquake" : "Fire"),
                    SeverityLevel = i % 4 == 0 ? "High" : "Medium",
                    Status = "Active",
                    ReportedById = "base-user",
                    StartDate = DateTime.Now.AddDays(-i % 20)
                });
            }
            await initialContext.SaveChangesAsync();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < operationCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var context = CreateInMemoryDbContext(sharedDatabaseName);
                    try
                    {
                        // Mix of read and write operations
                        if (i % 3 == 0)
                        {
                            // Read-heavy operation
                            var highSeverity = await context.Disasters
                                .Where(d => d.SeverityLevel == "High")
                                .CountAsync();

                            var recent = await context.Disasters
                                .Where(d => d.StartDate > DateTime.Now.AddDays(-7))
                                .ToListAsync();
                        }
                        else if (i % 3 == 1)
                        {
                            // Write operation
                            var disaster = new Disaster
                            {
                                Name = $"Mixed Workload Disaster {Guid.NewGuid()}",
                                Location = "Mixed Location",
                                Description = "Created during mixed workload test",
                                DisasterType = "Flood",
                                SeverityLevel = "Medium",
                                Status = "Active",
                                ReportedById = "mixed-user",
                                StartDate = DateTime.Now
                            };
                            context.Disasters.Add(disaster);
                            await context.SaveChangesAsync();
                        }
                        else
                        {
                            // Mixed operation
                            var floods = await context.Disasters
                                .Where(d => d.DisasterType == "Flood")
                                .Take(5)
                                .ToListAsync();

                            // Update one
                            if (floods.Any())
                            {
                                var toUpdate = floods.First();
                                toUpdate.Status = "Updated";
                                context.Disasters.Update(toUpdate);
                                await context.SaveChangesAsync();
                            }
                        }

                        lock (lockObj)
                        {
                            successfulOperations++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Mixed workload operation failed: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Console.WriteLine($"=== Mixed Workload Results ===");
            Console.WriteLine($"Total operations: {operationCount}");
            Console.WriteLine($"Successful operations: {successfulOperations}");
            Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Operations per second: {operationCount / (stopwatch.ElapsedMilliseconds / 1000.0):F2}");

            Assert.True(successfulOperations >= operationCount * 0.8,
                $"Only {successfulOperations}/{operationCount} operations succeeded");
        }
    }
}