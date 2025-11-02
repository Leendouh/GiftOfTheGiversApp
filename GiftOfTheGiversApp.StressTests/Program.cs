using System.Diagnostics;

namespace GiftOfTheGiversApp.StressTests
{
    public class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static int _successfulRequests = 0;
        private static int _failedRequests = 0;
        private static readonly object _lockObject = new object();

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Gift of the Givers Stress Test ===");

            var baseUrl = "https://localhost:5001"; // Change to your app's URL
            var concurrentUsers = 50;
            var durationSeconds = 60;

            Console.WriteLine($"Testing with {concurrentUsers} concurrent users for {durationSeconds} seconds");
            Console.WriteLine("Starting stress test...\n");

            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task>();

            // Create concurrent user tasks
            for (int i = 0; i < concurrentUsers; i++)
            {
                tasks.Add(SimulateUser(baseUrl, i, durationSeconds));
            }

            // Display progress
            var progressTask = DisplayProgress(durationSeconds);

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            progressTask.Dispose();

            Console.WriteLine("\n=== Stress Test Results ===");
            Console.WriteLine($"Total Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"Successful Requests: {_successfulRequests}");
            Console.WriteLine($"Failed Requests: {_failedRequests}");
            Console.WriteLine($"Requests per Second: {_successfulRequests / stopwatch.Elapsed.TotalSeconds:F2}");
            Console.WriteLine($"Success Rate: {(_successfulRequests * 100.0 / (_successfulRequests + _failedRequests)):F2}%");
        }

        private static async Task SimulateUser(string baseUrl, int userId, int durationSeconds)
        {
            var random = new Random(userId);
            var endpoints = new[]
            {
                "/",
                "/Disasters",
                "/Volunteers",
                "/Missions"
            };

            var endTime = DateTime.Now.AddSeconds(durationSeconds);

            while (DateTime.Now < endTime)
            {
                try
                {
                    var endpoint = endpoints[random.Next(endpoints.Length)];
                    var response = await _httpClient.GetAsync($"{baseUrl}{endpoint}");

                    lock (_lockObject)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            _successfulRequests++;
                        }
                        else
                        {
                            _failedRequests++;
                        }
                    }

                    // Random delay between requests (0.1 - 2 seconds)
                    await Task.Delay(random.Next(100, 2000));
                }
                catch (Exception ex)
                {
                    lock (_lockObject)
                    {
                        _failedRequests++;
                    }
                    Console.WriteLine($"User {userId} error: {ex.Message}");
                }
            }
        }

        private static async Task DisplayProgress(int durationSeconds)
        {
            for (int i = 0; i < durationSeconds; i++)
            {
                await Task.Delay(1000);
                Console.WriteLine($"Elapsed: {i + 1}s | Success: {_successfulRequests} | Failed: {_failedRequests}");
            }
        }
    }
}