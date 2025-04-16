using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Infrastructure.Repositories
{
    // TODO: Stub implementation for MVP/testing only.
    // Replace with EF Core-backed repository in future ticket.

    public class ProductRepository : IProductRepository
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
            return GetAllProducts().FirstOrDefault(p => p.Id == id);
        }
    }
}