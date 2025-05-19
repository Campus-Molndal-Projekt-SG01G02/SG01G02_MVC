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
            return Content("Catalogue works!");
        }

        public async Task<IActionResult> Details(int id)
        {
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
                ReviewCount = reviewCount
            };

            return View("Details", model);
        }
    }
}