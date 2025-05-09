using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SG01G02_MVC.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewApiClient _apiClient;
    public ReviewService(IReviewApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsForProduct(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            throw new ArgumentException("Product ID cannot be null or empty.", nameof(productId));
        }

        return await _apiClient.GetReviewsAsync(productId);
    }
}