using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Web.Controllers
{
    public class CatalogueController : Controller
    {
        private readonly IProductService _productService;

        public CatalogueController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var dtos = await _productService.GetAllProductsAsync();

            var viewModels = dtos.Select(dto => new ProductViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Price = dto.Price ?? 0m,
                Description = dto.Description ?? string.Empty,
                ImageUrl = dto.ImageUrl ?? string.Empty
            }).ToList();

            return View(viewModels);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            var model = new Product
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? string.Empty,
                Price = product.Price ?? 0m,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl ?? string.Empty
            };

            return View("Details", model);
        }
    }
}