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
        // Retrieve connection string from configuration or environment variable
        string? appInsightsConnectionString =
            configuration["ApplicationInsights:ConnectionString"] ??
            Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

        // Try to retrieve from Key Vault if available
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

        // Configure Application Insights and OpenTelemetry
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            Console.WriteLine("Configuring Application Insights with connection string");

            // Configure OpenTelemetry with Azure Monitor Exporter
            services.AddOpenTelemetry()
                .UseAzureMonitor(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                });
        }
        else
        {
            Console.WriteLine("Application Insights connection string not found - using standard logging");

            // Only basic logging without Azure Monitor
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }
    }
}