using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace SG01G02_MVC.Infrastructure.External;

public class ReviewApiClient : IReviewApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly ILogger<ReviewApiClient> _logger;

    public ReviewApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ReviewApiClient> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["ReviewApiURL"];
        _apiKey = configuration["ReviewApiKey"];

        if (string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(_apiKey))
        {
            // Fallback to mock if main API is not configured
            _baseUrl = configuration["MockReviewApiURL"] ?? throw new InvalidOperationException("MockReviewApiURL is not configured");
            _apiKey = configuration["MockReviewApiKey"];
        }
        _logger = logger;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
        return request;
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId)
    {
        try
        {
            _logger.LogInformation("Fetching reviews for product {ProductId} from {BaseUrl}", productId, _baseUrl);
            string requestUrl = $"{_baseUrl}/api/products/{productId}/reviews?code={_apiKey}";
            
            using var request = CreateRequest(HttpMethod.Get, requestUrl);
            var httpResponse = await _httpClient.SendAsync(request);

            if (httpResponse.IsSuccessStatusCode)
            {
                // Use your DTO here:
                var reviewResponse = await httpResponse.Content.ReadFromJsonAsync<ReviewResponseDto>();
                var reviews = reviewResponse?.Reviews ?? new List<ReviewDto>();
                _logger.LogInformation("Successfully retrieved {Count} reviews for product {ProductId}", reviews?.Count ?? 0, productId);
                return reviews;
            }
            else
            {
                _logger.LogWarning("Failed to fetch reviews for product {ProductId}. Status: {StatusCode}. Reason: {ReasonPhrase}", 
                    productId, httpResponse.StatusCode, httpResponse.ReasonPhrase);
                return new List<ReviewDto>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching reviews for product {ProductId}", productId);
            return new List<ReviewDto>();
        }
        catch (Exception ex) // Catch broader exceptions for debugging
        {
            _logger.LogError(ex, "Unexpected error fetching reviews for product {ProductId}", productId);
            return new List<ReviewDto>();
        }
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        try
        {
            var baseUrl = _baseUrl;
            var jwtToken = _apiKey;
            var postReviewUrl = $"{baseUrl}/api/product/{review.ProductId}/review";
            using var request = CreateRequest(HttpMethod.Post, postReviewUrl);
            request.Content = JsonContent.Create(review);
            
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
        catch (Exception ex) // Catch broader exceptions for debugging
        {
            _logger.LogError(ex, "Unexpected error submitting review for product {ProductId}", review.ProductId);
            return false;
        }
    }
}