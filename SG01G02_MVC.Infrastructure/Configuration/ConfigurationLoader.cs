using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace SG01G02_MVC.Infrastructure.Configuration;

public static class ConfigurationLoader
{
    public static void LoadAppConfiguration(WebApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    }
}