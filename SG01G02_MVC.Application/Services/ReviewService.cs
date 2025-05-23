using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace SG01G02_MVC.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewApiClient _apiClient;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IReviewApiClient apiClient, ILogger<ReviewService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsForProduct(int productId)
    {
        // TODO: TEMP DEBUG LINE
        Console.WriteLine("ReviewService.GetReviewsForProduct() called");
        Console.WriteLine("📦 ReviewService.GetReviewsForProduct called with productId = " + productId);

        if (productId <= 0)
        {
            _logger.LogWarning("Attempted to get reviews with invalid product ID: {ProductId}", productId);
            throw new ArgumentException("Product ID must be greater than 0.", nameof(productId));
        }

        _logger.LogInformation("Getting reviews for product {ProductId}", productId);
        var reviews = await _apiClient.GetReviewsAsync(productId);
        _logger.LogInformation("Retrieved {Count} reviews for product {ProductId}", 
            reviews.Count(), productId);
        return (reviews ?? new List<ReviewDto>()).Where(r => r != null);
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        return await _apiClient.SubmitReviewAsync(review);
    }
}