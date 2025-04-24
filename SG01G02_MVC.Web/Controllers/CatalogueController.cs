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

        public IActionResult Index()
        {
            var dtos = _productService.GetAllProducts();

            var viewModels = dtos.Select(dto => new ProductViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description
            }).ToList();

            return View(viewModels);
        }

        public IActionResult Details(int id)
        {
            var product = _productService.GetProductById(id);
            if (product == null)
                return NotFound();

            var model = new Product
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl
            };

            return View("Details", model);
        }
    }
}