using System.Diagnostics;
using Xunit;

namespace GiftOfTheGiversApp.UITests
{
    public class WebAppTestFixture : IAsyncLifetime
    {
        private Process _webAppProcess;
        public string BaseUrl { get; private set; } = "https://localhost:7173";

        public async Task InitializeAsync()
        {
            StartWebApplication();
            await WaitForWebApplication();
        }

        private void StartWebApplication()
        {
            var solutionDirectory = FindSolutionDirectory();
            var projectPath = Path.Combine(solutionDirectory, "GiftOfTheGiversApp");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --urls=https://localhost:7173",
                WorkingDirectory = projectPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _webAppProcess = Process.Start(processStartInfo);

            // Read output in background to prevent blocking
            Task.Run(() => ReadOutput(_webAppProcess.StandardOutput));
            Task.Run(() => ReadOutput(_webAppProcess.StandardError));
        }

        private async Task WaitForWebApplication()
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(2);

            var maxAttempts = 30; // Wait up to 60 seconds
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var response = await httpClient.GetAsync($"{BaseUrl}/");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Web application is ready at {BaseUrl}!");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {i + 1}/{maxAttempts}: {ex.Message}");
                    if (i == maxAttempts - 1)
                    {
                        throw new Exception($"Web application failed to start within {maxAttempts * 2} seconds. " +
                                          $"Make sure the project builds successfully and port 7173 is available.");
                    }
                }

                await Task.Delay(2000); // Wait 2 seconds before retry
            }
        }

        private string FindSolutionDirectory()
        {
            var directory = Directory.GetCurrentDirectory();
            while (directory != null)
            {
                if (Directory.GetFiles(directory, "*.sln").Any())
                    return directory;

                directory = Directory.GetParent(directory)?.FullName;
            }

            throw new InvalidOperationException("Solution directory not found");
        }

        private async void ReadOutput(StreamReader reader)
        {
            try
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                        Console.WriteLine($"[WebApp] {line}");
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore - process was disposed
            }
        }

        public async Task DisposeAsync()
        {
            try
            {
                if (_webAppProcess != null && !_webAppProcess.HasExited)
                {
                    _webAppProcess.Kill(true);

                    // FIXED: Use Task.Delay instead of WaitForExitAsync with milliseconds
                    var timeout = TimeSpan.FromMilliseconds(5000);
                    var stopwatch = Stopwatch.StartNew();

                    while (!_webAppProcess.HasExited && stopwatch.Elapsed < timeout)
                    {
                        await Task.Delay(100);
                    }

                    if (!_webAppProcess.HasExited)
                    {
                        Console.WriteLine("Web application process did not exit gracefully within timeout");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping web application: {ex.Message}");
            }
            finally
            {
                _webAppProcess?.Dispose();
            }
        }
    }
}