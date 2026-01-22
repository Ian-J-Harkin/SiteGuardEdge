using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // Add this line
using SiteGuardEdge.Application; // Add this line
using SiteGuardEdge.Domain.Interfaces; // Add this line
using SiteGuardEdge.Infrastructure.AI; // Add this line
using SiteGuardEdge.Infrastructure.Data;
using SiteGuardEdge.Infrastructure.Data.Repositories; // Add this line
using System.Configuration;
using System.Windows.Forms; // Add this line  System.Windows.Forms

namespace SiteGuardEdge.UI;


static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() // Make Main method 
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);

        //using (var serviceProvider = services.BuildServiceProvider())
        //{
        //    // Ensure database is migrated on startup
        //    using (var scope = serviceProvider.CreateScope())
        //    {
        //        var dbContext = scope.ServiceProvider.GetRequiredService<SiteGuardEdgeDbContext>();
        //        dbContext.Database.Migrate();  // synchronous

        //        // Purge old logs on startup
        //        var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
        //        var detectionEventRepository = scope.ServiceProvider.GetRequiredService<IDetectionEventRepository>();
        //        var retentionPeriod = configService.GetLogRetentionPeriod();
        //        var repo = scope.ServiceProvider.GetRequiredService<IDetectionEventRepository>();
        //        repo.PurgeOldDetectionEventsAsync(retentionPeriod).GetAwaiter().GetResult();    // (retentionPeriod);
        //    }

        //    System.Windows.Forms.Application.Run(serviceProvider.GetRequiredService<Form1>());
        //}
        using (var serviceProvider = services.BuildServiceProvider())
        {
            // 2. Run Startup Work in a controlled Blocking Block
            // We use Task.Run + GetResult to ensure the async state machine 
            // doesn't "break" our Main thread's apartment state.
            try
            {
                InitializeApplication(serviceProvider).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup Error: {ex.Message}", "Fatal Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 3. Start the UI loop on the original STA Thread
            System.Windows.Forms.Application.Run(serviceProvider.GetRequiredService<Form1>());
        }
    
    }

    // Separate logic for clarity and easier debugging
    private static async Task InitializeApplication(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            // Database migration (Synchronous)
            var dbContext = scope.ServiceProvider.GetRequiredService<SiteGuardEdgeDbContext>();
            dbContext.Database.Migrate();

            // Async Startup Logic
            var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
            var repo = scope.ServiceProvider.GetRequiredService<IDetectionEventRepository>();

            var retentionPeriod = configService.GetLogRetentionPeriod();

            // Use await naturally here
            await repo.PurgeOldDetectionEventsAsync(retentionPeriod);
        }
    }
  
    private static void ConfigureServices(IServiceCollection services)
    {
        //// Configure DbContext
        //services.AddDbContext<SiteGuardEdgeDbContext>(options =>
        //    options.UseSqlServer(ConfigurationManager.ConnectionStrings["SiteGuardEdgeDB"].ConnectionString));

        //// Register repositories and services
        //services.AddTransient<IDetectionEventRepository, DetectionEventRepository>();
        //services.AddSingleton<IConfigurationService, ConfigurationService>();

        //// Register AI detector
        //services.AddSingleton(provider =>
        //{
        //    string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "yolov8n.onnx");
        //    bool forceCpu = bool.Parse(ConfigurationManager.AppSettings["ForceCpu"] ?? "false");
        //    bool forceGpu = bool.Parse(ConfigurationManager.AppSettings["ForceGpu"] ?? "false");
        //    return new OnnxPpeDetector(modelPath, forceCpu: forceCpu, forceGpu: forceGpu);
        //});

        //// Register forms
        //services.AddTransient<Form1>();
        //services.AddTransient<LogViewerForm>();
        //services.AddTransient<SettingsForm>();
        try
        {
            ConfigureDatabase(services);
            ConfigureCoreServices(services);
            ConfigureAiDetector(services);
            ConfigureForms(services);
        }
        catch (Exception ex)
        { // Log it however your app logs (Serilog, NLog, EventLog, etc.)
            MessageBox.Show($"Service configuration failed:\n{ex.Message}", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw; // Fail fast — do NOT continue with a broken DI container 
        }
    }

    private static void ConfigureDatabase(IServiceCollection services)
    {
        var conn = ConfigurationManager.ConnectionStrings["SiteGuardEdgeDB"]?.ConnectionString;

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("Missing connection string: SiteGuardEdgeDB");

        services.AddDbContext<SiteGuardEdgeDbContext>(options => options.UseSqlServer(conn));
    }

    private static void ConfigureCoreServices(IServiceCollection services)
    {
        services.AddTransient<IDetectionEventRepository, DetectionEventRepository>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
    }

    private static void ConfigureAiDetector(IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "yolov8m.onnx");

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"ONNX model not found at: {modelPath}");

            bool forceCpu = bool.TryParse(ConfigurationManager.AppSettings["ForceCpu"], out var cpu) && cpu;
            bool forceGpu = bool.TryParse(ConfigurationManager.AppSettings["ForceGpu"], out var gpu) && gpu;

            return new OnnxPpeDetector(modelPath, forceCpu: forceCpu, forceGpu: forceGpu);
        });
    }

    private static void ConfigureForms(IServiceCollection services)
    {
        services.AddTransient<Form1>();
        services.AddTransient<LogViewerForm>();
        services.AddTransient<SettingsForm>();
    }
}
