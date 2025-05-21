using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SG01G02_MVC.Infrastructure.External;

public class ReviewApiClient : IReviewApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly ILogger<ReviewApiClient> _logger;

    public ReviewApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ReviewApiClient> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["ReviewApiURL"]
                    ?? configuration["MockReviewApiURL"]
                    ?? throw new InvalidOperationException("Neither ReviewApiURL nor MockReviewApiURL is configured.");

        _apiKey = configuration["ReviewApiKey"]
                    ?? configuration["MockReviewApiKey"]
                    ?? throw new InvalidOperationException("Neither ReviewApiKey nor MockReviewApiKey is configured.");

        _logger = logger;
        _logger.LogInformation("ReviewApiClient initialized with base URL: {BaseUrl}", _baseUrl);
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(int productId)
    {
        var requestUrl = $"{_baseUrl.TrimEnd('/')}/api/products/{productId}/reviews?code={_apiKey}";
        _logger.LogInformation("Fetching reviews for product {ProductId} from {Url}", productId, requestUrl);

        try
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, requestUrl);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to fetch reviews for product {ProductId}. Status: {StatusCode}. Content: {Content}", productId, response.StatusCode, content);
                return new List<ReviewDto>();
            }

            var dto = await response.Content.ReadFromJsonAsync<ReviewResponseDto>();
            var reviews = dto?.Reviews?.Where(r => r != null).ToList() ?? new List<ReviewDto>();
            _logger.LogInformation("Retrieved {Count} reviews for product {ProductId}", reviews.Count, productId);
            return reviews;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching reviews for product {ProductId}", productId);
            return new List<ReviewDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching reviews for product {ProductId}", productId);
            return new List<ReviewDto>();
        }
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        var postUrl = $"{_baseUrl.TrimEnd('/')}/products/{review.ProductId}/review";
        _logger.LogInformation("Submitting review to: {Url}", postUrl);

        try
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, postUrl);
            request.Content = JsonContent.Create(new
            {
                reviewerName = review.CustomerName,
                text = review.Content,
                rating = review.Rating,
                reviewDate = review.CreatedAt
            });

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully submitted review for product {ProductId}", review.ProductId);
                return true;
            }

            _logger.LogWarning("Failed to submit review for product {ProductId}. Status: {StatusCode}. Content: {Content}", review.ProductId, response.StatusCode, content);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error submitting review for product {ProductId}", review.ProductId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting review for product {ProductId}", review.ProductId);
            return false;
        }
    }

    public async Task<int?> RegisterProductAsync(ProductDto product)
    {
        var registerUrl = $"{_baseUrl.TrimEnd('/')}/product/save";
        _logger.LogInformation("Registering product via: {Url}", registerUrl);

        try
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, registerUrl);
            request.Content = JsonContent.Create(new
            {
                name = product.Name,
                description = product.Description,
                price = product.Price
            });

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to register product. Status: {StatusCode}. Content: {Content}", response.StatusCode, content);
                return null;
            }

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("productId", out var idProp))
                return idProp.GetInt32();

            _logger.LogWarning("No productId returned in response: {Content}", content);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error registering product");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error registering product");
            return null;
        }
    }

    private Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(_apiKey))
            request.Headers.Add("X-API-KEY", _apiKey);
        return Task.FromResult(request);
    }
}