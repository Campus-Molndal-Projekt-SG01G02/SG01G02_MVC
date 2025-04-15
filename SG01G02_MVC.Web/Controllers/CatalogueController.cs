using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Web.Controllers
{
    public class CatalogueController : Controller
    {
        private readonly IProductRepository _productRepository;

        public CatalogueController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public IActionResult Index()
        {
            var products = _productRepository.GetAllProducts();
            return View(products);
        }
    }
}