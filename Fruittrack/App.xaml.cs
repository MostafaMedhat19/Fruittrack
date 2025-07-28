using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System;

namespace Fruittrack
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e); // Call base first

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Add environment-specific settings if needed
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrEmpty(environment))
            {
                builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
            }

            var configuration = builder.Build();

            var services = new ServiceCollection();

            // Configure DbContext with explicit lifetime
            services.AddDbContext<FruitTrackDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                // Enable sensitive data logging only in development
#if DEBUG
                options.EnableSensitiveDataLogging();
#endif
            },
            ServiceLifetime.Transient); // Explicit transient lifetime

            // Register your main window
            services.AddSingleton<MainWindow>(); // Or whatever your main window is

            ServiceProvider = services.BuildServiceProvider();

            // Start the application
     
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up the service provider
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            base.OnExit(e);
        }
    }
}