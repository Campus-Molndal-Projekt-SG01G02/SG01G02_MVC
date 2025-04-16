using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SG01G02_MVC.Infrastructure.Services;

namespace SG01G02_MVC.Web.Controllers
{
    [Route("api/[controller]")]
    public class ImagesController : Controller
    {
        private readonly BlobStorageService _blobService;

        public ImagesController(BlobStorageService blobService)
        {
            _blobService = blobService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Ingen fil har skickats");
            }

            // Kontrollera filtyp (endast bilder)
            if (!file.ContentType.StartsWith("image/"))
            {
                return BadRequest("Endast bildfiler tillåts");
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
}