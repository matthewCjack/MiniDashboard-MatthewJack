using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace MiniDashboard.Api.IntegrationTests
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private readonly string _tempDataFolder;

        public CustomWebApplicationFactory()
        {
            _tempDataFolder = Path.Combine(Path.GetTempPath(), $"MiniDashboard_TestData_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDataFolder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            // make the app actually run under the temp folder
            builder.UseContentRoot(_tempDataFolder);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Make sure the folder is fresh
            if (Directory.Exists(_tempDataFolder))
                Directory.Delete(_tempDataFolder, true);
            Directory.CreateDirectory(_tempDataFolder);

            return base.CreateHost(builder);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                if (Directory.Exists(_tempDataFolder))
                    Directory.Delete(_tempDataFolder, true);
            }
            catch { /* ignore cleanup errors */ }
        }
    }
}
