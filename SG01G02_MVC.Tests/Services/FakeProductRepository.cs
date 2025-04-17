using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SG01G02_MVC.Tests.Services
{
    public class FakeProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new()
        {
            new Product { Id = 1, Name = "Test Product 1", Price = 100 },
            new Product { Id = 2, Name = "Test Product 2", Price = 200 }
        };

        public IEnumerable<Product> GetAllProducts()
        {
            return _products;
        }

        public Product? GetProductById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }
    }
}
