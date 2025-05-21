using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Application.DTOs;
using System.Linq;

namespace SG01G02_MVC.Web.Controllers
{
    public class CatalogueController : Controller
    {
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;
        private readonly IBlobStorageService _blobStorageService;

        public CatalogueController(
            IProductService productService,
            IReviewService reviewService,
            IBlobStorageService blobStorageService)
        {
            _productService = productService;
            _reviewService = reviewService;
            _blobStorageService = blobStorageService;

        }

        public async Task<IActionResult> Index()
        {
            // TODO: TEMP DEBUG LINE
            Console.WriteLine("📋 CatalogueController: Fetching reviews...");

            try
            {
                var dtos = await _productService.GetAllProductsAsync();
                var viewModels = new List<ProductViewModel>();
                foreach (var dto in dtos)
                {
                    var reviewsEnumerable = await _reviewService.GetReviewsForProduct(dto.Id.ToString());
                    var reviews = (reviewsEnumerable ?? Enumerable.Empty<ReviewDto>()).Where(r => r != null).ToList();

                    double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                    int reviewCount = reviews.Count();
                    viewModels.Add(new ProductViewModel
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        Price = dto.Price ?? 0m,
                        Description = dto.Description ?? string.Empty,
                        ImageName = dto.ImageName,
                        ImageUrl = dto.HasImage ? _blobStorageService.GetBlobUrl(dto.ImageName ?? string.Empty) : dto.ImageUrl,
                        StockQuantity = dto.StockQuantity,
                        Reviews = reviews,
                        AverageRating = avgRating,
                        ReviewCount = reviewCount,
                        ExternalReviewApiProductId = dto.ExternalReviewApiProductId
                    });
                }
                return View(viewModels);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            // TODO: TEMP DEBUG LINE
            Console.WriteLine("📋 CatalogueController: Fetching reviews...");

            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            var reviewsEnumerable = await _reviewService.GetReviewsForProduct(product.Id.ToString());
            var reviews = (reviewsEnumerable ?? Enumerable.Empty<ReviewDto>()).Where(r => r != null).ToList();

            double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            int reviewCount = reviews.Count();

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? string.Empty,
                Price = product.Price ?? 0m,
                StockQuantity = product.StockQuantity,
                ImageName = product.ImageName,
                ImageUrl = product.HasImage ? _blobStorageService.GetBlobUrl(product.ImageName) : product.ImageUrl,
                Reviews = reviews,
                AverageRating = avgRating,
                ReviewCount = reviewCount,
                ExternalReviewApiProductId = product.ExternalReviewApiProductId
            };

            return View("Details", model);
        }
    }
}