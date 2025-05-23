using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Web.Services;
using SG01G02_MVC.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace SG01G02_MVC.Web.Controllers;

public class AdminController : Controller
{
    private readonly IProductService _productService;
    private readonly IUserSessionService _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IReviewApiClient _reviewApiClient;
    private readonly IFeatureToggleService _featureToggleService;

    public AdminController(
        IProductService productService,
        IUserSessionService session,
        IBlobStorageService blobStorageService,
        IReviewApiClient reviewApiClient,
        IFeatureToggleService featureToggleService)
    {
        _productService = productService;
        _session = session;
        _blobStorageService = blobStorageService;
        _reviewApiClient = reviewApiClient;
        _featureToggleService = featureToggleService;
    }

    private bool IsAdmin => _session?.Role == "Admin";

    public async Task<IActionResult> Index()
    {
        try
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
                ImageUrl = p.HasImage ? _blobStorageService.GetBlobUrl(p.ImageName ?? string.Empty) : p.ImageUrl
            }).ToList();
            
            // Check if the feature toggle is enabled
            ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();
            
            return View(viewModels);
        }
        catch (Exception ex)
        {
            return Content($"Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsAdmin) return RedirectToAction("Index", "Login");
        
        // Check if the feature toggle is enabled
        ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();
        
        return View(new ProductViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (!IsAdmin) return RedirectToAction("Index", "Login");

        // Validate model and file types manually
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            // Check file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(model.ImageFile.ContentType))
            {
                ModelState.AddModelError("ImageFile", "Only image files (JPEG, PNG, GIF, WebP) are allowed.");
            }

            // Check file size (max 5MB)
            if (model.ImageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "Image size cannot exceed 5MB.");
            }
        }

        if (!ModelState.IsValid) 
        {
            // Re-add feature toggle information if validation fails
            ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();
            return View(model);
        }

        // Handle image upload if an image is provided
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            try
            {
                // Upload the image
                model.ImageName = await _blobStorageService.UploadImageAsync(model.ImageFile);
                // Set ImageUrl to null so we use blob URL instead
                model.ImageUrl = null;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();
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

        try
        {
            // 1. Register product with external API
            productDto.ExternalReviewApiProductId = await _reviewApiClient.RegisterProductAsync(productDto);

            // Show different messages based on feature toggle
            if (productDto.ExternalReviewApiProductId == null)
            {
                if (_featureToggleService.UseMockReviewApi())
                {
                    TempData["ReviewInfo"] = "Product created with Mock API (development mode).";
                }
                else
                {
                    TempData["ReviewError"] = "External product registration failed. Product saved, but reviews won't work.";
                }
            }
            else if (_featureToggleService.UseMockReviewApi())
            {
                TempData["ReviewInfo"] = $"Product registered with Mock API (ID: {productDto.ExternalReviewApiProductId}).";
            }

            // 2. Save product to internal DB
            await _productService.CreateProductAsync(productDto);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            // 3. Surface the exception to the UI for now (instead of _logger)
            ModelState.AddModelError("", $"An error occurred while creating the product: {ex.Message}");
            ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();
            return View(model);
        }
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
                : product.ImageUrl,
            ExternalReviewApiProductId = product.ExternalReviewApiProductId?.ToString()
        };

        // Add feature toggle information
        ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductViewModel model)
    {
        if (!IsAdmin) return RedirectToAction("Index", "Login");

        // Validate model and file types manually
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            // Check file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(model.ImageFile.ContentType))
            {
                ModelState.AddModelError("ImageFile", "Only image files (JPEG, PNG, GIF, WebP) are allowed.");
            }

            // Check file size (max 5MB)
            if (model.ImageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "Image size cannot exceed 5MB.");
            }
        }

        if (!ModelState.IsValid) 
        {
            ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();
            return View(model);
        }

        // Get existing product to check if we need to delete an old image
        var existingProduct = await _productService.GetProductByIdAsync(model.Id);

        // Handle image upload if a new image is provided
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            try
            {
                // If the product already has an image in Blob Storage, delete the old one
                if (existingProduct != null && !string.IsNullOrEmpty(existingProduct.ImageName))
                {
                    await _blobStorageService.DeleteImageAsync(existingProduct.ImageName);
                }

                // Upload new image
                model.ImageName = await _blobStorageService.UploadImageAsync(model.ImageFile);
                // Set ImageUrl to null so we use blob URL instead
                model.ImageUrl = null;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();
                return View(model);
            }
        }
        else if (existingProduct != null)
        {
            // Keep existing image if no new one is uploaded
            model.ImageName = existingProduct.ImageName;

            // If there's no ImageName (Blob Storage) but there's an ImageUrl
            // (from previous implementation), keep ImageUrl
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
            ImageName = model.ImageName,
            ExternalReviewApiProductId = int.TryParse(model.ExternalReviewApiProductId, out int id) ? id : null
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
                : product.ImageUrl,
            ExternalReviewApiProductId = product.ExternalReviewApiProductId?.ToString()
        };

        // Add feature toggle information
        ViewBag.UseMockApi = _featureToggleService.UseMockReviewApi();

        return View(model);
    }
    
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!IsAdmin) return RedirectToAction("Index", "Login");

        // Get the product before it's deleted so we can delete the image
        var product = await _productService.GetProductByIdAsync(id);
        if (product != null && !string.IsNullOrEmpty(product.ImageName))
        {
            try
            {
                await _blobStorageService.DeleteImageAsync(product.ImageName);
            }
            catch (Exception ex)
            {
                // Log the error but continue deleting the product
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

    [HttpGet]
    public async Task<IActionResult> FixMissingExternalReviewApiIds()
    {
        if (!IsAdmin) return RedirectToAction("Index", "Login");
        int updated = await _productService.PatchMissingExternalReviewApiIdsAsync();
        return Content($"Updated {updated} products with dummy ExternalReviewApiProductId.");
    }
    
    // Feature toggle action
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleFeatureFlag(string featureName, string newValue)
    {
        if (!IsAdmin) return RedirectToAction("Index", "Login");

        try
        {
            if (featureName == "UseMockApi")
            {
                // Update configuration in memory
                var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        
                // Note: This is a runtime change that affects the current session
                // For persistent changes, you'd need to update appsettings.json or use a database
                if (configuration is IConfigurationRoot configRoot)
                {
                    configRoot["FeatureToggles:UseMockApi"] = newValue;
                }

                bool isEnabled = bool.Parse(newValue);
                string message = isEnabled 
                    ? "Switched to Mock API mode (Development)" 
                    : "Switched to Production API mode";
            
                TempData["FeatureToggleMessage"] = message;
            }
            else
            {
                TempData["FeatureToggleError"] = "Unknown feature toggle.";
            }
        }
        catch (Exception ex)
        {
            TempData["FeatureToggleError"] = $"Error updating feature toggle: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
