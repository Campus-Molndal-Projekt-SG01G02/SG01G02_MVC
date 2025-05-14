using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Domain.Entities;
using System.Linq;
using System.Threading.Tasks;
using SG01G02_MVC.Infrastructure.Services;

namespace SG01G02_MVC.Web.Controllers
{
    public class CatalogueController : Controller
    {
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;
        private readonly IBlobStorageService _blobStorageService;

        public CatalogueController(IProductService productService, IReviewService reviewService)
        {
            _productService = productService;
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index()
        {
            var dtos = await _productService.GetAllProductsAsync();
            var viewModels = new List<ProductViewModel>();
            foreach (var dto in dtos)
            {
                var reviews = (await _reviewService.GetReviewsForProduct(dto.Id.ToString())).ToList();
                double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                int reviewCount = reviews.Count;
                viewModels.Add(new ProductViewModel
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Price = dto.Price ?? 0m,
                    Description = dto.Description ?? string.Empty,
                    ImageName = dto.ImageName,
                    ImageUrl = dto.HasImage ? _blobStorageService.GetBlobUrl(dto.ImageName) : dto.ImageUrl,
                    StockQuantity = dto.StockQuantity,
                    Reviews = reviews,
                    AverageRating = avgRating,
                    ReviewCount = reviewCount
                });
            }
            return View(viewModels);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            var reviews = (await _reviewService.GetReviewsForProduct(product.Id.ToString())).ToList();
            double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            int reviewCount = reviews.Count;

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? string.Empty,
                Price = product.Price ?? 0m,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl ?? string.Empty,
                Reviews = reviews,
                AverageRating = avgRating,
                ReviewCount = reviewCount
            };

            return View("Details", model);
        }
    }
}