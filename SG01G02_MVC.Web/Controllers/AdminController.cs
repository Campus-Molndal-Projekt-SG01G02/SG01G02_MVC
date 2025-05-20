using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Web.Services;
using SG01G02_MVC.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace SG01G02_MVC.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IProductService _productService;
        private readonly IUserSessionService _session;
        private readonly IBlobStorageService _blobStorageService;

        public AdminController(
            IProductService productService,
            IUserSessionService session,
            IBlobStorageService blobStorageService)
        {
            _productService = productService;
            _session = session;
            _blobStorageService = blobStorageService;
        }

        private bool IsAdmin => _session?.Role == "Admin";

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");
            var products = await _productService.GetAllProductsAsync();
            var viewModels = products.Select(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageName = p.ImageName,
                ImageUrl = p.HasImage ? _blobStorageService.GetBlobUrl(p.ImageName) : p.ImageUrl

            }).ToList();
            return View(viewModels);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");
            return View(new ProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");

            // Validera modellen och filtyperna manuellt
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // Kontrollera filtyp
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(model.ImageFile.ContentType))
                {
                    ModelState.AddModelError("ImageFile", "Only image files (JPEG, PNG, GIF, WebP) are allowed.");
                }

                // Kontrollera filstorlek (max 5MB)
                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Image size cannot exceed 5MB.");
                }
            }

            if (!ModelState.IsValid) return View(model);

            // Hantera bilduppladdning om en bild tillhandahålls
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    // Ladda upp bilden
                    model.ImageName = await _blobStorageService.UploadImageAsync(model.ImageFile);
                    // Sätt ImageUrl till null så att vi använder blob URL istället
                    model.ImageUrl = null;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                    return View(model);
                }
            }

            var productDto = new ProductDto
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price ?? 0,
                StockQuantity = model.StockQuantity,
                ImageUrl = model.ImageUrl,
                ImageName = model.ImageName
            };

            await _productService.CreateProductAsync(productDto);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageName = product.ImageName,
                ImageUrl = !string.IsNullOrEmpty(product.ImageName)
                    ? _blobStorageService.GetBlobUrl(product.ImageName)
                    : product.ImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");

            // Validera modellen och filtyperna manuellt
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // Kontrollera filtyp
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(model.ImageFile.ContentType))
                {
                    ModelState.AddModelError("ImageFile", "Only image files (JPEG, PNG, GIF, WebP) are allowed.");
                }

                // Kontrollera filstorlek (max 5MB)
                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Image size cannot exceed 5MB.");
                }
            }

            if (!ModelState.IsValid) return View(model);

            // Hämta befintlig produkt för att kontrollera om vi behöver ta bort en gammal bild
            var existingProduct = await _productService.GetProductByIdAsync(model.Id);

            // Hantera bilduppladdning om en ny bild tillhandahålls
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    // Om produkten redan har en bild i Blob Storage, ta bort den gamla
                    if (existingProduct != null && !string.IsNullOrEmpty(existingProduct.ImageName))
                    {
                        await _blobStorageService.DeleteImageAsync(existingProduct.ImageName);
                    }

                    // Ladda upp ny bild
                    model.ImageName = await _blobStorageService.UploadImageAsync(model.ImageFile);
                    // Sätt ImageUrl till null så att vi använder blob URL istället
                    model.ImageUrl = null;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                    return View(model);
                }
            }
            else if (existingProduct != null)
            {
                // Behåll befintlig bild om ingen ny laddas upp
                model.ImageName = existingProduct.ImageName;

                // Om det inte finns en ImageName (Blob Storage) men det finns en ImageUrl
                // (från tidigare implementation), behåll ImageUrl
                if (string.IsNullOrEmpty(model.ImageName) && !string.IsNullOrEmpty(existingProduct.ImageUrl))
                {
                    model.ImageUrl = existingProduct.ImageUrl;
                }
            }

            var productDto = new ProductDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price ?? 0,
                StockQuantity = model.StockQuantity,
                ImageUrl = model.ImageUrl,
                ImageName = model.ImageName
            };

            await _productService.UpdateProductAsync(productDto);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageName = product.ImageName,
                ImageUrl = !string.IsNullOrEmpty(product.ImageName)
                    ? _blobStorageService.GetBlobUrl(product.ImageName)
                    : product.ImageUrl
            };

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");

            // Hämta produkten innan den tas bort så vi kan ta bort bilden
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null && !string.IsNullOrEmpty(product.ImageName))
            {
                try
                {
                    await _blobStorageService.DeleteImageAsync(product.ImageName);
                }
                catch (Exception ex)
                {
                    // Logga felet men fortsätt radera produkten
                    Console.WriteLine($"Error deleting blob: {ex.Message}");
                }
            }

            await _productService.DeleteProductAsync(id);
            return RedirectToAction("Index");
        }

        // Delete image action
        [HttpGet]
        public async Task<IActionResult> DeleteImage(int id)
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");

            // Get the product
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            // Delete the image from blob storage if it exists
            if (!string.IsNullOrEmpty(product.ImageName))
            {
                try
                {
                    await _blobStorageService.DeleteImageAsync(product.ImageName);
                }
                catch (Exception ex)
                {
                    // Log the error but continue updating the product
                    Console.WriteLine($"Error deleting blob: {ex.Message}");
                }
            }

            // Update the product to remove image reference
            var updatedProduct = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageName = null,
                ImageUrl = null
            };

            await _productService.UpdateProductAsync(updatedProduct);

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> FixMissingExternalReviewApiIds()
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");
            int updated = await _productService.PatchMissingExternalReviewApiIdsAsync();
            return Content($"Updated {updated} products with dummy ExternalReviewApiProductId.");
        }
    }
}