using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Application.DTOs;

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
                Price = dto.Price
            }).ToList();

            return View(viewModels);
        }
    }
}