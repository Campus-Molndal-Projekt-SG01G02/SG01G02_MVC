using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerName = configuration["BlobStorageSettings:ContainerName"] ?? "product-images";

            // Get connection string from configuration
            var connectionString = configuration["BlobStorageSettings:ConnectionString"] ??
                                 configuration["BlobConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                var errorMsg = "BlobConnectionString or BlobStorageSettings:ConnectionString is missing in the configuration.";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            try
            {
                // Log connection attempt (masking sensitive info)
                var connStrParts = connectionString.Split(';');
                var safeConnStr = string.Join(";",
                    Array.FindAll(connStrParts, p => !p.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase)));
                _logger.LogInformation("Connecting to Blob Storage with: {SafeConnectionString}", safeConnStr);

                // Initialize the blob service client
                _blobServiceClient = new BlobServiceClient(connectionString);

                // Ensure the container exists
                EnsureContainerExistsAsync().GetAwaiter().GetResult();
                _logger.LogInformation("Successfully connected to Blob Storage and verified container: {ContainerName}", _containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Blob Storage");
                throw;
            }
        }

        private async Task EnsureContainerExistsAsync()
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                _logger.LogInformation("Checking if container '{ContainerName}' exists...", _containerName);

                // Check if exists first to avoid unnecessary create attempts
                var exists = await containerClient.ExistsAsync();
                if (exists)
                {
                    _logger.LogInformation("Container '{ContainerName}' already exists", _containerName);
                    return;
                }

                // Create the container with public access
                _logger.LogInformation("Creating container '{ContainerName}'...", _containerName);
                var response = await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                if (response != null && response.Value != null)
                {
                    _logger.LogInformation("Container '{ContainerName}' created successfully", _containerName);
                }
                else
                {
                    _logger.LogInformation("Container '{ContainerName}' already existed or creation status unknown", _containerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring container '{ContainerName}' exists", _containerName);
                throw;
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                var errorMsg = "File is empty or null";
                _logger.LogError(errorMsg);
                throw new ArgumentException(errorMsg, nameof(file));
            }

            try
            {
                string blobName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                _logger.LogInformation("Uploading file {FileName} as blob {BlobName}", file.FileName, blobName);

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Set content type and upload
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                };

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, blobHttpHeaders);
                }

                var blobUrl = blobClient.Uri.ToString();
                _logger.LogInformation("Successfully uploaded blob {BlobName} to {BlobUrl}", blobName, blobUrl);

                return blobName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                _logger.LogWarning("Delete request with empty blob name");
                return false;
            }

            try
            {
                _logger.LogInformation("Deleting blob {BlobName}", blobName);

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DeleteIfExistsAsync();

                if (response.Value)
                {
                    _logger.LogInformation("Successfully deleted blob {BlobName}", blobName);
                }
                else
                {
                    _logger.LogWarning("Blob {BlobName} did not exist or could not be deleted", blobName);
                }

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob {BlobName}", blobName);
                // Return false rather than throw to avoid disrupting the application flow
                return false;
            }
        }

        public string GetBlobUrl(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                _logger.LogWarning("GetBlobUrl called with empty blob name");
                return string.Empty;
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                var url = blobClient.Uri.ToString();

                _logger.LogInformation("GetBlobUrl for {BlobName}: {Url}", blobName, url);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting URL for blob {BlobName}", blobName);
                // Return empty string rather than throw to avoid disrupting the application flow
                return string.Empty;
            }
        }
    }
}