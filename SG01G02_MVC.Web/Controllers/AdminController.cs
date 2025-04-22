using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Web.Services;

namespace SG01G02_MVC.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IProductService _productService;
        private readonly AppDbContext _context;
        private readonly IUserSessionService _session;

        public AdminController(IProductService productService, AppDbContext context, IUserSessionService session)
        {
            _productService = productService;
            _context = context;
            _session = session;
        }

        // Admin Landing Page (Dashboard)
        public IActionResult Index()
        {
            if (!_context.Database.CanConnect())
                return View("DatabaseUnavailable");

            if (_session?.Role != "Admin")
                return RedirectToAction("Index", "Login");

            var products = _productService.GetAllProducts();
            return View(products);
        }

        // CREATE Product
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View("Create"); // Reuse Create.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            await _productService.CreateProductAsync(MapToDto(model));
            return RedirectToAction("Index");
        }

        // EDIT Product
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var dto = _productService.GetProductById(id);
            if (dto == null)
                return NotFound();

            var vm = MapToViewModel(dto);
            return View("Edit", vm);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(int id, ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Edit", model);

            await _productService.UpdateProductAsync(MapToDto(model));
            return RedirectToAction("Index");
        }

        // DELETE Product
        [HttpGet]
        public IActionResult DeleteProduct(int id)
        {
            var dto = _productService.GetProductById(id);
            if (dto == null)
                return NotFound();

            var vm = MapToViewModel(dto);
            return View("Delete", vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productService.DeleteProductAsync(id);
            return RedirectToAction("Index");
        }

        // Mapping Helpers (DTO <-> ViewModel)
        private ProductDto MapToDto(ProductViewModel vm)
        {
            return new ProductDto
            {
                Id = vm.Id,
                Name = vm.Name,
                Description = vm.Description,
                Price = vm.Price,
                StockQuantity = vm.StockQuantity,
                ImageUrl = vm.ImageUrl
            };
        }

        private ProductViewModel MapToViewModel(ProductDto dto)
        {
            return new ProductViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                ImageUrl = dto.ImageUrl
            };
        }
    }
}