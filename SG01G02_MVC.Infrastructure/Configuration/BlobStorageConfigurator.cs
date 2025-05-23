using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Infrastructure.Services;

namespace SG01G02_MVC.Infrastructure.Configuration;

public class BlobStorageConfigurator
{
    public void Configure(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var connectionString = config["BlobStorageSettings:ConnectionString"] ?? config["BlobConnectionString"];
        var isEmulated = connectionString == "InMemoryEmulation=true";

        if (isEmulated || string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Using InMemoryBlobStorageService");
            builder.Services.AddSingleton<IBlobStorageService, InMemoryBlobStorageService>();
        }
        else
        {
            Console.WriteLine("Using real BlobStorageService");
            builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
        }

        if (string.IsNullOrEmpty(config["BlobStorageSettings:ContainerName"]))
        {
            builder.Configuration["BlobStorageSettings:ContainerName"] = "product-images";
        }
    }
}