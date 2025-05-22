using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Services
{
    public interface IProductReviewSyncService
    {
        Task<int?> EnsureExternalReviewProductIdAsync(Product product);
    }

    public class ProductReviewSyncService : IProductReviewSyncService
    {
        private readonly IReviewApiClient _reviewApiClient;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductReviewSyncService> _logger;

        public ProductReviewSyncService(
            IReviewApiClient reviewApiClient,
            IProductRepository productRepository,
            ILogger<ProductReviewSyncService> logger)
        {
            _reviewApiClient = reviewApiClient;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<int?> EnsureExternalReviewProductIdAsync(Product product)
        {
            if (product.ExternalReviewApiProductId.HasValue)
            {
                return product.ExternalReviewApiProductId.Value;
            }

            _logger.LogInformation("Registering product {ProductId} with external review API", product.Id);

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageName = product.ImageName,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity,
            };

            var externalId = await _reviewApiClient.RegisterProductAsync(dto);
            if (externalId.HasValue)
            {
                product.ExternalReviewApiProductId = externalId.Value;
                await _productRepository.UpdateProductAsync(product);
                _logger.LogInformation("Product {ProductId} successfully registered. External ID = {ExternalId}", product.Id, externalId.Value);
            }
            else
            {
                _logger.LogWarning("Product {ProductId} registration failed. No external ID returned.", product.Id);
            }

            return externalId;
        }
    }
}