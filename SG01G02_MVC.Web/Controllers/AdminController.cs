using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Web.Services;
using SG01G02_MVC.Application.DTOs;

namespace SG01G02_MVC.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IProductService _productService;
        private readonly IUserSessionService _session;

        public AdminController(IProductService productService, IUserSessionService session)
        {
            _productService = productService;
            _session = session;
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
                ImageUrl = p.ImageUrl
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
            if (!ModelState.IsValid) return View(model);
            var productDto = new ProductDto
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                ImageUrl = model.ImageUrl
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
                ImageUrl = product.ImageUrl
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");
            if (!ModelState.IsValid) return View(model);
            var productDto = new ProductDto
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                ImageUrl = model.ImageUrl
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
                ImageUrl = product.ImageUrl
            };
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin) return RedirectToAction("Index", "Login");
            await _productService.DeleteProductAsync(id);
            return RedirectToAction("Index");
        }
    }
}