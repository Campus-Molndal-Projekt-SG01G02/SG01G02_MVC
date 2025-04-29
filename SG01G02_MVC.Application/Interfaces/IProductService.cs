using SG01G02_MVC.Application.DTOs;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProducts();
        Task<ProductDto?> GetProductById(int id);
        Task CreateProductAsync(ProductDto dto);
        Task UpdateProductAsync(ProductDto dto);
        Task DeleteProductAsync(int id);
    }
}