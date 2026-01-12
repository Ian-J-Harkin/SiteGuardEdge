using Microsoft.EntityFrameworkCore;
using SiteGuardEdge.Infrastructure.Data;
using SiteGuardEdge.Infrastructure.Data.Repositories; // Add this line
using SiteGuardEdge.Domain.Interfaces; // Add this line
using SiteGuardEdge.Application; // Add this line
using System.Configuration;
using Microsoft.Extensions.DependencyInjection; // Add this line
using SiteGuardEdge.Infrastructure.AI; // Add this line
//using System.Windows.Forms; // Add this line

namespace SiteGuardEdge.UI;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static async Task Main() // Make Main method async
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);

        await using (var serviceProvider = services.BuildServiceProvider())
        {
            // Ensure database is migrated on startup
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SiteGuardEdgeDbContext>();
                await dbContext.Database.MigrateAsync();

                // Purge old logs on startup
                var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
                var detectionEventRepository = scope.ServiceProvider.GetRequiredService<IDetectionEventRepository>();
                var retentionPeriod = configService.GetLogRetentionPeriod();
                await detectionEventRepository.PurgeOldDetectionEventsAsync(retentionPeriod);
            }

            System.Windows.Forms.Application.Run(serviceProvider.GetRequiredService<Form1>());
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configure DbContext
        services.AddDbContext<SiteGuardEdgeDbContext>(options =>
            options.UseSqlServer(ConfigurationManager.ConnectionStrings["SiteGuardEdgeDB"].ConnectionString));

        // Register repositories and services
        services.AddTransient<IDetectionEventRepository, DetectionEventRepository>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Register AI detector
        services.AddSingleton(provider =>
        {
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "yolov8n.onnx");
            bool forceCpu = bool.Parse(ConfigurationManager.AppSettings["ForceCpu"] ?? "false");
            bool forceGpu = bool.Parse(ConfigurationManager.AppSettings["ForceGpu"] ?? "false");
            return new OnnxPpeDetector(modelPath, forceCpu: forceCpu, forceGpu: forceGpu);
        });

        // Register forms
        services.AddTransient<Form1>();
        services.AddTransient<LogViewerForm>();
        services.AddTransient<SettingsForm>();
    }
}