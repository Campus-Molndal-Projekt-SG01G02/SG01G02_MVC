using Microsoft.AspNetCore.Http;

namespace SG01G02_MVC.Application.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file);
    Task<bool> DeleteImageAsync(string blobName);
    string GetBlobUrl(string blobName);
}