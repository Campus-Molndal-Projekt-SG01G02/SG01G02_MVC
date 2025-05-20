using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IReviewApiClient _reviewApiClient;

        public ProductService(IProductRepository repository, IReviewApiClient reviewApiClient)
        {
            _repository = repository;
            _reviewApiClient = reviewApiClient;
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

        public async Task CreateProductAsync(ProductDto dto)
        {
            var product = MapToEntity(dto);
            await _repository.CreateProductAsync(product);

            var externalId = await _reviewApiClient.RegisterProductAsync(dto);
            if (externalId.HasValue)
            {
                await _repository.UpdateExternalReviewApiProductIdAsync(product.Id, externalId.Value);
            }
            else
            {
                // Set dummy value if registration fails
                await _repository.UpdateExternalReviewApiProductIdAsync(product.Id, 31337);
            }
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

        public async Task<int> PatchMissingExternalReviewApiIdsAsync(int dummyValue = 31337)
        {
            var products = await _repository.GetAllProductsAsync();
            var toUpdate = products.Where(p => p.ExternalReviewApiProductId == null).ToList();
            foreach (var product in toUpdate)
            {
                product.ExternalReviewApiProductId = dummyValue;
                await _repository.UpdateProductAsync(product);
            }
            return toUpdate.Count;
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
                ImageUrl = product.ImageUrl,
                ImageName = product.ImageName,
                ExternalReviewApiProductId = product.ExternalReviewApiProductId
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
                ImageUrl = dto.ImageUrl ?? string.Empty,
                ImageName = dto.ImageName ?? string.Empty,
                ExternalReviewApiProductId = dto.ExternalReviewApiProductId
            };
        }
    }
}