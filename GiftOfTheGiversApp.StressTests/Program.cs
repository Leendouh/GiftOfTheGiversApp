using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GiftOfTheGiversApp.StressTests
{
    public class Program
    {
        private static HttpClient? _httpClient;
        private static int _successfulRequests = 0;
        private static int _failedRequests = 0;
        private static readonly object _lockObject = new object();
        private static readonly List<long> _responseTimes = new List<long>();

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Gift of the Givers Load & Stress Test ===");
            Console.WriteLine("Testing application performance...\n");

            // Use the correct URL for your application
            var workingUrl = "https://localhost:7173";

            // Test connection first
            Console.WriteLine($"Testing connection to: {workingUrl}");
            if (!await TestConnection(workingUrl))
            {
                Console.WriteLine("❌ Cannot connect to application. Please make sure it's running.");
                Console.WriteLine("   Your app should be at: https://localhost:7173");
                Console.WriteLine("   Make sure to run: cd GiftOfTheGiversApp && dotnet run");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"✅ Connected to: {workingUrl}");
            Console.WriteLine("Starting load tests...\n");

            // Run different test scenarios
            var testScenarios = new[]
            {
                new { Name = "Smoke Test", Users = 2, Duration = 10 },
                new { Name = "Load Test", Users = 5, Duration = 20 },
                new { Name = "Stress Test", Users = 10, Duration = 30 }
            };

            foreach (var scenario in testScenarios)
            {
                Console.WriteLine($"🚀 {scenario.Name}: {scenario.Users} users for {scenario.Duration}s");
                await RunLoadTest(workingUrl, scenario.Users, scenario.Duration, scenario.Name);
                Console.WriteLine();

                // Reset for next test
                ResetMetrics();
                await Task.Delay(2000);
            }

            Console.WriteLine("🎉 All tests completed!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task<bool> TestConnection(string baseUrl)
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

                var response = await _httpClient.GetAsync($"{baseUrl}/");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Success! Application is running at {baseUrl}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ HTTP Error: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Connection failed: {ex.Message}");
                return false;
            }
        }

        private static async Task RunLoadTest(string baseUrl, int users, int duration, string testName)
        {
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task>();
            var startTime = DateTime.Now;

            // Progress display
            var progressTask = DisplayProgress(duration, startTime);

            // Create user tasks
            for (int i = 0; i < users; i++)
            {
                tasks.Add(SimulateUser(baseUrl, i, duration));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();
            await progressTask;

            GenerateReport(testName, stopwatch.Elapsed.TotalSeconds, users);
        }

        private static async Task SimulateUser(string baseUrl, int userId, int durationSeconds)
        {
            var random = new Random(userId);
            var endpoints = new[] {
                "/",
                "/Disasters",
                "/Volunteers",
                "/Missions",
                "/Donations",
                "/Assignments"
            };

            var endTime = DateTime.Now.AddSeconds(durationSeconds);

            while (DateTime.Now < endTime)
            {
                var endpoint = endpoints[random.Next(endpoints.Length)];
                await ExecuteRequest(baseUrl, endpoint, userId);
                await Task.Delay(random.Next(500, 2000)); // Think time
            }
        }

        private static async Task ExecuteRequest(string baseUrl, string endpoint, int userId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (_httpClient == null) return;

                var response = await _httpClient.GetAsync($"{baseUrl}{endpoint}");
                stopwatch.Stop();

                lock (_lockObject)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        _successfulRequests++;
                        _responseTimes.Add(stopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        _failedRequests++;
                        Console.WriteLine($"   User {userId} got {response.StatusCode} on {endpoint}");
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                lock (_lockObject)
                {
                    _failedRequests++;
                }
                Console.WriteLine($"   User {userId} error on {endpoint}: {ex.Message}");
            }
        }

        private static async Task DisplayProgress(int durationSeconds, DateTime startTime)
        {
            for (int i = 0; i < durationSeconds; i++)
            {
                await Task.Delay(1000);
                var elapsed = DateTime.Now - startTime;
                lock (_lockObject)
                {
                    var avgResponse = _responseTimes.Any() ? _responseTimes.Average() : 0;
                    var currentRps = _successfulRequests + _failedRequests;
                    Console.WriteLine($"  {elapsed.TotalSeconds:F0}s | ✅ {_successfulRequests} | ❌ {_failedRequests} | ⏱️ {avgResponse:F0}ms");
                }
            }
        }

        private static void GenerateReport(string testName, double totalTime, int users)
        {
            var totalRequests = _successfulRequests + _failedRequests;
            var successRate = totalRequests > 0 ? (_successfulRequests * 100.0 / totalRequests) : 0;

            Console.WriteLine($"📊 {testName} Results:");
            Console.WriteLine($"   Time: {totalTime:F1}s | Users: {users}");
            Console.WriteLine($"   Requests: {totalRequests} | RPS: {totalRequests / totalTime:F1}/s");
            Console.WriteLine($"   Success: {_successfulRequests} | Failed: {_failedRequests}");
            Console.WriteLine($"   Success Rate: {successRate:F1}%");

            if (_responseTimes.Any())
            {
                Console.WriteLine($"   Avg Response: {_responseTimes.Average():F0}ms");
                Console.WriteLine($"   Min Response: {_responseTimes.Min()}ms");
                Console.WriteLine($"   Max Response: {_responseTimes.Max()}ms");

                // Calculate 95th percentile
                var sortedTimes = _responseTimes.OrderBy(t => t).ToList();
                var percentile95 = sortedTimes[(int)(sortedTimes.Count * 0.95)];
                Console.WriteLine($"   95th Percentile: {percentile95}ms");
            }

            // Pass/Fail indicator
            if (successRate >= 95)
                Console.WriteLine("   ✅ EXCELLENT - Application handles load well");
            else if (successRate >= 80)
                Console.WriteLine("   ⚠️  ACCEPTABLE - Minor issues under load");
            else
                Console.WriteLine("   ❌ NEEDS IMPROVEMENT - Significant performance issues");
        }

        private static void ResetMetrics()
        {
            lock (_lockObject)
            {
                _successfulRequests = 0;
                _failedRequests = 0;
                _responseTimes.Clear();
            }
        }
    }
}