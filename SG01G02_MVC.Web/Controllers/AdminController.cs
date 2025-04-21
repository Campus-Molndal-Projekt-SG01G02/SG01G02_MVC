using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Web.Models;

namespace SG01G02_MVC.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IProductService _productService;

        public AdminController(IProductService productService)
        {
            _productService = productService;
        }

        public IActionResult Index()
        {
            if (!User.Identity?.IsAuthenticated ?? true || User.IsInRole("Admin") == false)
            {
                return RedirectToAction("Index", "Login");
            }

            var products = _productService.GetAllProducts(); // or async
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _productService.CreateProductAsync(MapToDto(model));
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var dto = _productService.GetProductById(id);
            if (dto == null)
                return NotFound();

            var vm = MapToViewModel(dto);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _productService.UpdateProductAsync(MapToDto(model));
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var dto = _productService.GetProductById(id);
            if (dto == null)
                return NotFound();

            var vm = MapToViewModel(dto);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productService.DeleteProductAsync(id);
            return RedirectToAction("Index");
        }

        // --- Private Mapping Helpers ---

        private ProductDto MapToDto(ProductViewModel vm)
        {
            return new ProductDto
            {
                Id = vm.Id,
                Name = vm.Name,
                Price = vm.Price
            };
        }

        private ProductViewModel MapToViewModel(ProductDto dto)
        {
            return new ProductViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Price = dto.Price
            };
        }
    }
}