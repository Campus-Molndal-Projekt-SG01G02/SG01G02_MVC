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
    
    // TODO: Remove the code below if it is not needed

    // private void UpdateBlobConnectionStringConfiguration(WebApplicationBuilder builder, string blobConnectionString)
    // {
    //     builder.Configuration["BlobStorageSettings:ConnectionString"] = blobConnectionString;
    //     builder.Configuration["BlobConnectionString"] = blobConnectionString;
    // }

    // private void LogCurrentConfiguration(WebApplicationBuilder builder)
    // {
    //     Console.WriteLine("=== Blob Storage Configuration ===");
    //     Console.WriteLine($"BlobStorageSettings:ConnectionString: {
    //         (string.IsNullOrEmpty(builder.Configuration["BlobStorageSettings:ConnectionString"]) ? "missing" : "exists")
    //     }");
    //     Console.WriteLine($"BlobConnectionString: {
    //         (string.IsNullOrEmpty(builder.Configuration["BlobConnectionString"]) ? "missing" : "exists")}");
    // }

    // private string? GetBlobConnectionString(WebApplicationBuilder builder)
    // {
    //     return builder.Configuration["BlobStorageSettings:ConnectionString"] ??
    //            builder.Configuration["BlobConnectionString"];
    // }

    // private string ProcessConnectionString(string? blobConnectionString, WebApplicationBuilder builder)
    // {
    //     if (blobConnectionString == "UseDevelopmentStorage=true")
    //     {
    //         Console.WriteLine(
    //             "WARNING: Connection string is set to UseDevelopmentStorage=true but Azure Storage Emulator is not available");
    //         Console.WriteLine("Using in-memory fallback for blob storage instead");

    //         blobConnectionString = "InMemoryEmulation=true";
    //         UpdateBlobConnectionStringConfiguration(builder, blobConnectionString);
    //     }
    //     else if (string.IsNullOrEmpty(blobConnectionString))
    //     {
    //         Console.WriteLine("WARNING: No Blob connection string found in configuration from Key Vault");
    //         Console.WriteLine(
    //             "Using fallback test mode for blob storage (no actual blob operations will be performed)");

    //         blobConnectionString = "InMemoryEmulation=true";
    //         UpdateBlobConnectionStringConfiguration(builder, blobConnectionString);
    //     }
    //     else
    //     {
    //         Console.WriteLine("Found Blob connection string in configuration from Key Vault");
    //     }

    //     return blobConnectionString;
    // }

    // private void SetDefaultContainerName(WebApplicationBuilder builder)
    // {
    //     if (string.IsNullOrEmpty(builder.Configuration["BlobStorageSettings:ContainerName"]))
    //     {
    //         builder.Configuration["BlobStorageSettings:ContainerName"] = "product-images";
    //     }
    // }

    // private void LogFinalConfiguration(WebApplicationBuilder builder, string blobConnectionString)
    // {
    //     Console.WriteLine($"Using Blob connection string: {
    //         (blobConnectionString.Contains("AccountKey") ? "Real Azure Blob" : blobConnectionString)}");
    //     Console.WriteLine($"Container name: {builder.Configuration["BlobStorageSettings:ContainerName"]}");
    // }
}