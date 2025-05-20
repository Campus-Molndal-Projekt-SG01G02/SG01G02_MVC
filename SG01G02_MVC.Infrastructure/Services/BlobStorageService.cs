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
        private readonly bool _isTestMode;
        private readonly ILogger<BlobStorageService>? _logger;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService>? logger = null)
        {
            _logger = logger;

            // Check if we're in test mode using environment variable
            _isTestMode = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Testing",
                StringComparison.OrdinalIgnoreCase);

            _containerName = configuration["BlobStorageSettings:ContainerName"] ?? "product-images";

            // Log test mode status
            LogInfo($"BlobStorageService initializing, test mode: {_isTestMode}");

            // Get connection string from configuration
            var connectionString = configuration["BlobStorageSettings:ConnectionString"] ??
                                configuration["BlobConnectionString"];

            // Handle special case for in-memory emulation
            if (connectionString == "InMemoryEmulation=true")
            {
                LogInfo("Using in-memory blob storage emulation (no actual storage)");
                _isTestMode = true;
                return;
            }

            // In test mode, don't initialize the real blob client
            if (_isTestMode)
            {
                LogInfo("BlobStorageService running in test mode - no actual blob operations will be performed");
                return;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                var errorMsg = "BlobConnectionString or BlobStorageSettings:ConnectionString is missing in the configuration.";
                LogError(errorMsg);

                // Fall back to test mode instead of crashing
                LogWarning("Falling back to test mode due to missing connection string");
                _isTestMode = true;
                return;
            }

            try
            {
                // Log connection attempt (masking sensitive info)
                var connStrParts = connectionString.Split(';');
                var safeConnStr = string.Join(";",
                    Array.FindAll(connStrParts, p => !p.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase)));
                LogInfo($"Connecting to Blob Storage with: {safeConnStr}");

                // Initialize the blob service client
                _blobServiceClient = new BlobServiceClient(connectionString);

                // Ensure the container exists
                EnsureContainerExistsAsync().GetAwaiter().GetResult();
                LogInfo($"Successfully connected to Blob Storage and verified container: {_containerName}");
            }
            catch (Exception ex)
            {
                // In test mode, swallow exceptions
                if (_isTestMode)
                {
                    LogWarning($"Ignoring blob storage error in test mode: {ex.Message}");
                    return;
                }

                // In other modes, log and rethrow
                LogError($"Failed to initialize Blob Storage: {ex.Message}");
                throw;
            }
        }

        private async Task EnsureContainerExistsAsync()
        {
            if (_isTestMode) return;

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                LogInfo($"Checking if container '{_containerName}' exists...");

                // Check if exists first to avoid unnecessary create attempts
                var exists = await containerClient.ExistsAsync();
                if (exists)
                {
                    LogInfo($"Container '{_containerName}' already exists");
                    return;
                }

                // Create the container with public access
                LogInfo($"Creating container '{_containerName}'...");
                var response = await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                if (response != null && response.Value != null)
                {
                    LogInfo($"Container '{_containerName}' created successfully");
                }
                else
                {
                    LogInfo($"Container '{_containerName}' already existed or creation status unknown");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error ensuring container '{_containerName}' exists: {ex.Message}");
                throw;
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                var errorMsg = "File is empty or null";
                LogError(errorMsg);
                throw new ArgumentException(errorMsg, nameof(file));
            }

            // In test mode, just return a dummy blob name
            if (_isTestMode)
            {
                var testBlobName = $"test-{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                LogInfo($"Test mode: Simulating upload of {file.FileName}, returning {testBlobName}");
                return testBlobName;
            }

            try
            {
                string blobName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                LogInfo($"Uploading file {file.FileName} as blob {blobName}");

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
                LogInfo($"Successfully uploaded blob {blobName} to {blobUrl}");

                return blobName;
            }
            catch (Exception ex)
            {
                LogError($"Error uploading file {file.FileName}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                LogWarning("Delete request with empty blob name");
                return false;
            }

            // In test mode, always return success
            if (_isTestMode)
            {
                LogInfo($"Test mode: Simulating deletion of blob {blobName}");
                return true;
            }

            try
            {
                LogInfo($"Deleting blob {blobName}");

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DeleteIfExistsAsync();

                if (response.Value)
                {
                    LogInfo($"Successfully deleted blob {blobName}");
                }
                else
                {
                    LogWarning($"Blob {blobName} did not exist or could not be deleted");
                }

                return response.Value;
            }
            catch (Exception ex)
            {
                LogError($"Error deleting blob {blobName}: {ex.Message}");
                // Return false rather than throw to avoid disrupting the application flow
                return false;
            }
        }

        public string GetBlobUrl(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                LogWarning("GetBlobUrl called with empty blob name");
                return string.Empty;
            }

            // In test mode, return a dummy URL
            if (_isTestMode)
            {
                var testUrl = $"/images/{blobName}";
                LogInfo($"Test mode: GetBlobUrl returning {testUrl}");
                return testUrl;
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                var url = blobClient.Uri.ToString();

                LogInfo($"GetBlobUrl for {blobName}: {url}");
                return url;
            }
            catch (Exception ex)
            {
                LogError($"Error getting URL for blob {blobName}: {ex.Message}");
                // Return empty string rather than throw to avoid disrupting the application flow
                return string.Empty;
            }
        }

        #region Logging Helpers

        private void LogInfo(string message)
        {
            _logger?.LogInformation(message);
            Console.WriteLine($"INFO: {message}");
        }

        private void LogWarning(string message)
        {
            _logger?.LogWarning(message);
            Console.WriteLine($"WARNING: {message}");
        }

        private void LogError(string message)
        {
            _logger?.LogError(message);
            Console.WriteLine($"ERROR: {message}");
        }

        #endregion
    }
}