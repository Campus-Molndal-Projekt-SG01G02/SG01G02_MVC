using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace SG01G02_MVC.Infrastructure.External;

public class MockReviewApiClient : IReviewApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockReviewApiClient> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public MockReviewApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<MockReviewApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["MockReviewApiURL"] ?? throw new ArgumentNullException("MockReviewApiURL");
        _apiKey = configuration["MockReviewApiKey"] ?? throw new ArgumentNullException("MockReviewApiKey"); // ✅ NEW LINE
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId)
    {
        var url = $"{_baseUrl}/api/products/{productId}/reviews?code={_apiKey}";
        Console.WriteLine($"[MOCK] GET {url}");
        _logger.LogInformation("Fetching mock reviews from: {Url}", url);

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mock API returned error status: {StatusCode}", response.StatusCode);
            return Enumerable.Empty<ReviewDto>();
        }

        var wrapper = await response.Content.ReadFromJsonAsync<ReviewResponseDto>();
        return wrapper?.Reviews ?? Enumerable.Empty<ReviewDto>();
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        var url = $"{_baseUrl}/api/reviews";
        _logger.LogInformation("Submitting mock review to: {Url}", url);

        var response = await _httpClient.PostAsJsonAsync(url, review);
        return response.IsSuccessStatusCode;
    }

    public async Task<int?> RegisterProductAsync(ProductDto product)
    {
        // Assume the mock API just returns 200 OK and echoes back a fake ID
        var url = $"{_baseUrl}/api/products";
        _logger.LogInformation("Registering product with mock API: {Url}", url);

        var response = await _httpClient.PostAsJsonAsync(url, product);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mock API registration failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }

        // Let's assume mock returns { id: 123 } or similar
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        return content != null && content.TryGetValue("id", out var id) ? id : null;
    }
}
