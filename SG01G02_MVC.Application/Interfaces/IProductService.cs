using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface IProductService
    {
        IEnumerable<Product> GetAllProducts();
        Product? GetProductById(int id);
    }
}