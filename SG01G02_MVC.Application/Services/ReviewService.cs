using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public async Task<IEnumerable<ReviewDto>> GetReviewsForProduct(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            _logger.LogWarning("Attempted to get reviews with null or empty product ID");
            throw new ArgumentException("Product ID cannot be null or empty.", nameof(productId));
        }

        _logger.LogInformation("Getting reviews for product {ProductId}", productId);
        var reviews = await _apiClient.GetReviewsAsync(productId);
        _logger.LogInformation("Retrieved {Count} reviews for product {ProductId}", 
            reviews.Count(), productId);
        return reviews;
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        return await _apiClient.SubmitReviewAsync(review);
    }
}