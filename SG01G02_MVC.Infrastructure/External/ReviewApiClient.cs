using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SG01G02_MVC.Infrastructure.External;

public class ReviewApiClient : IReviewApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<ReviewApiClient> _logger;

    public ReviewApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ReviewApiClient> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["ReviewApi:BaseUrl"] ?? throw new InvalidOperationException("ReviewApi:BaseUrl is not configured");
        _logger = logger;
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId)
    {
        try
        {
            _logger.LogInformation("Fetching reviews for product {ProductId}", productId);
            var response = await _httpClient.GetFromJsonAsync<List<ReviewDto>>($"{_baseUrl}/product/{productId}");
            _logger.LogInformation("Successfully retrieved {Count} reviews for product {ProductId}", 
                response?.Count ?? 0, productId);
            return response ?? new List<ReviewDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching reviews for product {ProductId}", productId);
            return new List<ReviewDto>();
        }
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/product/{review.ProductId}/reviews", review);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully submitted review for product {ProductId}", review.ProductId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to submit review for product {ProductId}. Status: {Status}", review.ProductId, response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error submitting review for product {ProductId}", review.ProductId);
            return false;
        }
    }
}