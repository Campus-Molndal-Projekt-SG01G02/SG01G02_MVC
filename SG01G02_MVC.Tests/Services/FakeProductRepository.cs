using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Tests.Services
{
    public class FakeProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new()
        {
            new Product { Id = 1, Name = "Test Product 1", Price = 10.0m },
            new Product { Id = 2, Name = "Test Product 2", Price = 20.0m }
        };

        public Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return Task.FromResult<IEnumerable<Product>>(_products);
        }

        public Task<Product?> GetProductByIdAsync(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(product);
        }

        public Task CreateProductAsync(Product product)
        {
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateProductAsync(Product product)
        {
            var existing = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existing != null)
            {
                existing.Name = product.Name;
                existing.Price = product.Price;
            }
            return Task.CompletedTask;
        }

        public Task DeleteProductAsync(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _products.Remove(product);
            }
            return Task.CompletedTask;
        }

        public Task UpdateExternalReviewApiProductIdAsync(int productId, int externalId)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
            {
                product.ExternalReviewApiProductId = externalId.ToString();
            }
            return Task.CompletedTask;
        }
    }
}