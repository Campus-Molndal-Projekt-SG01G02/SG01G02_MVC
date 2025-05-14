using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration)
    {
        // Get connection string from configuration (Key Vault or appsettings)
        // Check both locations - directly in root and in BlobStorageSettings section
        var connectionString = configuration["BlobStorageSettings:ConnectionString"] ??
                              configuration["BlobConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException("BlobConnectionString or BlobStorageSettings:ConnectionString is missing in the configuration.");
        }

        _containerName = configuration["BlobStorageSettings:ContainerName"]
                         ?? Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_NAME")
                         ?? "product-images";

        _blobServiceClient = new BlobServiceClient(connectionString);

        // Ensure the container exists
        EnsureContainerExistsAsync().GetAwaiter().GetResult();
    }

    // Rest of your implementation remains the same
    private async Task EnsureContainerExistsAsync()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null", nameof(file));
        }

        string blobName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, new BlobHttpHeaders
            {
                ContentType = file.ContentType
            });
        }

        return blobName;
    }

    public async Task<bool> DeleteImageAsync(string blobName)
    {
        if (string.IsNullOrEmpty(blobName))
        {
            return false;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DeleteIfExistsAsync();

        return response.Value;
    }

    public string GetBlobUrl(string blobName)
    {
        if (string.IsNullOrEmpty(blobName))
        {
            return string.Empty;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        return blobClient.Uri.ToString();
    }
}