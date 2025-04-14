using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetAll();
        Product? GetById(int id);
    }
}