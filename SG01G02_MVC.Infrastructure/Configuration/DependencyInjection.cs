using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Infrastructure.Services;

namespace SG01G02_MVC.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register the FeatureToggleService
        services.AddSingleton<IFeatureToggleService, FeatureToggleService>();
        
        // Register the LoggingServiceFactory
        services.AddSingleton<ILoggingServiceFactory, LoggingServiceFactory>();
            
        // Register the LoggingService
        services.AddSingleton<ILoggingService>(sp =>
        {
            var factory = sp.GetRequiredService<ILoggingServiceFactory>();
            var keyVaultService = sp.GetService<IKeyVaultService>();
            var config = sp.GetRequiredService<IConfiguration>();
                
            var loggingService = factory.Create(keyVaultService);
            loggingService.ConfigureServices(services, config);
                
            return loggingService;
        });
        
        return services;
    }
}