using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetAllProducts();
        Product? GetProductById(int id);
    }
}