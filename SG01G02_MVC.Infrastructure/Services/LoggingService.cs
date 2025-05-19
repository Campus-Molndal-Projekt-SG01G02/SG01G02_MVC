using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.Services;

public class LoggingService : ILoggingService
{
    private readonly IKeyVaultService? _keyVaultService;

    public LoggingService(IKeyVaultService? keyVaultService = null)
    {
        _keyVaultService = keyVaultService;
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Hämta connection string från konfiguration eller miljövariabel
        string? appInsightsConnectionString =
            configuration["ApplicationInsights:ConnectionString"] ??
            Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

        // Försök hämta från Key Vault om tillgängligt
        if (string.IsNullOrEmpty(appInsightsConnectionString) && _keyVaultService?.IsAvailable == true)
        {
            try
            {
                appInsightsConnectionString = _keyVaultService.GetSecret("ApplicationInsightsConnectionString");
                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                {
                    Console.WriteLine("Retrieved Application Insights connection string from Key Vault");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Application Insights connection string from Key Vault: {ex.Message}");
            }
        }

        // Konfigurera Application Insights och OpenTelemetry
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            Console.WriteLine("Configuring Application Insights with connection string");

            // Lägg till Application Insights med connection string
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });

            // Lägg till OpenTelemetry med Azure Monitor
            services.AddOpenTelemetry().UseAzureMonitor();

            // Konfigurera Application Insights Logger
            services.AddLogging(builder =>
            {
                builder.AddApplicationInsights(
                    configureTelemetryConfiguration: (config) =>
                        config.ConnectionString = appInsightsConnectionString,
                    configureApplicationInsightsLoggerOptions: (options) => { }
                );
            });

            Console.WriteLine("Application Insights configured successfully");
        }
        else
        {
            Console.WriteLine("WARNING: No Application Insights connection string found. Using default configuration.");
            services.AddApplicationInsightsTelemetry();

            // Om ingen connection string finns, lägg till standardloggning
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }
    }
}