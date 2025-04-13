using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;
using System.Linq;

namespace SG01G02_MVC.Application.Services
{
    public class ProductService : IProductService
    {
        public IEnumerable<Product> GetAllProducts()
        {
            return new List<Product>
            {
                new Product { Id = 1, Name = "Blueberry Jam", Price = 49.99m },
                new Product { Id = 2, Name = "Frozen Blueberries", Price = 79.99m },
                new Product { Id = 3, Name = "Dried Blueberries", Price = 59.00m }
            };
        }

        public Product? GetProductById(int id)
        {
            // TODO: This is a stub implementation using in-memory data.
            // In the future, this should query the database via IProductRepository.
            return GetAllProducts().FirstOrDefault(p => p.Id == id);
        }
    }
}