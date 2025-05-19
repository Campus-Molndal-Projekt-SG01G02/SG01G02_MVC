using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Net.Http;
using System.Linq;

namespace SG01G02_MVC.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewApiClient _apiClient;
    private readonly ILogger<ReviewService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ReviewService(IReviewApiClient apiClient, ILogger<ReviewService> logger, HttpClient httpClient, string baseUrl)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
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
        return (reviews ?? new List<ReviewDto>()).Where(r => r != null);
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        try
        {
            var postReviewUrl = $"{_baseUrl}/api/products/{review.ProductId}/reviews";
            using var request = CreateAuthenticatedRequestAsync(HttpMethod.Post, postReviewUrl);

            // Map to external API format
            var apiReview = new
            {
                reviewerName = review.CustomerName,
                text = review.Content,
                rating = review.Rating,
                reviewDate = review.CreatedAt
            };

            request.Content = JsonContent.Create(apiReview);

            var httpResponse = await _httpClient.SendAsync(request);

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully submitted review for product {ProductId}", review.ProductId);
                return true;
            }
            else
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to submit review for product {ProductId}. Status: {StatusCode}. Reason: {ReasonPhrase}. Content: {ErrorContent}", 
                    review.ProductId, httpResponse.StatusCode, httpResponse.ReasonPhrase, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error submitting review for product {ProductId}", review.ProductId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting review for product {ProductId}", review.ProductId);
            return false;
        }
    }

    private HttpRequestMessage CreateAuthenticatedRequestAsync(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        // Add authentication headers here
        return request;
    }
}