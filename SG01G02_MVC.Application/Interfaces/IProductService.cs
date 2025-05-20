using SG01G02_MVC.Application.DTOs;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task CreateProductAsync(ProductDto dto);
        Task UpdateProductAsync(ProductDto dto);
        Task DeleteProductAsync(int id);
        Task<int> PatchMissingExternalReviewApiIdsAsync(int dummyValue = 31337);
    }
}