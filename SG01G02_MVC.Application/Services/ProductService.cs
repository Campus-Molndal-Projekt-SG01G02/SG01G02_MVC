using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _repository.GetAllProductsAsync();
            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _repository.GetProductByIdAsync(id);
            return product == null ? null : MapToDto(product);
        }

        public Task CreateProductAsync(ProductDto dto)
        {
            var product = MapToEntity(dto);
            return _repository.CreateProductAsync(product);
        }

        public Task UpdateProductAsync(ProductDto dto)
        {
            var product = MapToEntity(dto);
            return _repository.UpdateProductAsync(product);
        }

        public Task DeleteProductAsync(int id)
        {
            return _repository.DeleteProductAsync(id);
        }

        // --- Mapping ---

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl
            };
        }

        private static Product MapToEntity(ProductDto dto)
        {
            return new Product
            {
                Id = dto.Id,
                Name = dto.Name,
                Price = dto.Price ?? 0m,
                Description = dto.Description ?? string.Empty,
                StockQuantity = dto.StockQuantity,
                ImageUrl = dto.ImageUrl ?? string.Empty
            };
        }
    }
}