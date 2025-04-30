using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new()
        {
            new Product { Id = 1, Name = "Blueberry Jam", Price = 49.99m, StockQuantity = 5, ImageUrl = "images/jam.jpg" },
            new Product { Id = 2, Name = "Frozen Blueberries", Price = 79.99m, StockQuantity = 0, ImageUrl = "images/frozen.jpg" },
            new Product { Id = 3, Name = "Dried Blueberries", Price = 59.00m, StockQuantity = 12, ImageUrl = "images/dried.jpg" }
        };

        public Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return Task.FromResult(_products.AsEnumerable());
        }

        public Task<Product?> GetProductByIdAsync(int id)
        {
            return Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
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
                existing.StockQuantity = product.StockQuantity;
                existing.ImageUrl = product.ImageUrl;
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
    }
}