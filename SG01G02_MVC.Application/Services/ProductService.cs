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

        public IEnumerable<ProductDto> GetAllProducts()
        {
            var products = _repository.GetAllProducts();
            return products.Select(MapToDto);
        }

        public ProductDto? GetProductById(int id)
        {
            var product = _repository.GetProductById(id);
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
                Price = product.Price
            };
        }

        private static Product MapToEntity(ProductDto dto)
        {
            return new Product
            {
                Id = dto.Id,
                Name = dto.Name,
                Price = dto.Price
            };
        }
    }
}