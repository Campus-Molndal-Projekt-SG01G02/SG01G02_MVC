using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.Services;

public class InMemoryBlobStorageService : IBlobStorageService
{
    private readonly ILogger<InMemoryBlobStorageService> _logger;
    private readonly Dictionary<string, byte[]> _storage = new();

    public InMemoryBlobStorageService(ILogger<InMemoryBlobStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadImageAsync(IFormFile file)
    {
        var id = $"{Guid.NewGuid()}_{file.FileName}";

        using var ms = new MemoryStream();
        file.CopyTo(ms);
        _storage[id] = ms.ToArray();

        _logger.LogInformation("InMemory: uploaded {BlobName} ({Size} bytes)", id, _storage[id].Length);
        return Task.FromResult(id);
    }

    public Task<bool> DeleteImageAsync(string blobName)
    {
        var removed = _storage.Remove(blobName);
        _logger.LogInformation("InMemory: deleted {BlobName} = {Result}", blobName, removed);
        return Task.FromResult(removed);
    }

    public string GetBlobUrl(string blobName)
    {
        if (_storage.ContainsKey(blobName))
        {
            var fakeUrl = $"https://localhost/inmemory/{blobName}";
            _logger.LogInformation("InMemory: returning URL {Url}", fakeUrl);
            return fakeUrl;
        }

        _logger.LogWarning("InMemory: blob not found: {BlobName}", blobName);
        return string.Empty;
    }
}