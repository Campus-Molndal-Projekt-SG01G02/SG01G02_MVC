using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Web.Controllers;

[Route("api/[controller]")]
public class ImagesController : Controller
{
    private readonly IBlobStorageService _blobService;

    public ImagesController(IBlobStorageService blobService)
    {
        _blobService = blobService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was uploaded");
        }

        // Check file type (only images)
        if (!file.ContentType.StartsWith("image/"))
        {
            return BadRequest("Only image files are allowed");
        }

        var imageUrl = await _blobService.UploadImageAsync(file);
        return Ok(new { url = imageUrl });
    }

    [HttpDelete("{blobName}")]
    public async Task<IActionResult> DeleteImage(string blobName)
    {
        var result = await _blobService.DeleteImageAsync(blobName);
        return result ? Ok() : NotFound();
    }
}