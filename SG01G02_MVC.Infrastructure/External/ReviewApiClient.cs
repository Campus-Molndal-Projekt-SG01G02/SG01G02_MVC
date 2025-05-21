using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SG01G02_MVC.Infrastructure.External;

public class ReviewApiClient : IReviewApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly ILogger<ReviewApiClient> _logger;
    // Commenting out external API token handling for now
    // private string? _jwtToken;
    // private DateTime _tokenExpiry = DateTime.MinValue;

    public ReviewApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ReviewApiClient> logger)
    {
        _httpClient = httpClient;
        // Using mock API URL from configuration
        _baseUrl = configuration["MockReviewApiURL"] ?? throw new InvalidOperationException("MockReviewApiURL is not configured");
        _apiKey = configuration["MockReviewApiKey"];
        _logger = logger;

        // Log the configured base URL for debugging
        _logger.LogInformation("ReviewApiClient initialized with base URL: {BaseUrl}", _baseUrl);
    }

    // Commenting out external API authentication for now
    /*
    private async Task EnsureValidTokenAsync()
    {
        // If we have a valid token that hasn't expired, use it
        if (!string.IsNullOrEmpty(_jwtToken) && DateTime.UtcNow < _tokenExpiry)
        {
            _logger.LogInformation("Using existing valid JWT token, expires at {ExpiryTime}", _tokenExpiry);
            return;
        }

        try
        {
            _logger.LogInformation("Authenticating with review API to get JWT token. Auth URL: {AuthUrl}", $"{_baseUrl}/api/auth/login");
            var authUrl = $"{_baseUrl}/api/auth/login";
            
            var authRequest = new HttpRequestMessage(HttpMethod.Post, authUrl);
            authRequest.Content = JsonContent.Create(new { apiKey = _apiKey });
            
            _logger.LogInformation("Sending authentication request with API key length: {KeyLength}", _apiKey?.Length ?? 0);
            var response = await _httpClient.SendAsync(authRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Auth response received - Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, responseContent);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse?.Token != null)
                {
                    _jwtToken = authResponse.Token;
                    // Set token expiry to 55 minutes from now (assuming 1-hour token lifetime)
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
                    _logger.LogInformation("Successfully obtained new JWT token, expires at {ExpiryTime}", _tokenExpiry);
                }
                else
                {
                    _logger.LogError("Received null token from authentication endpoint. Response content: {Content}", responseContent);
                    throw new InvalidOperationException("Received null token from authentication endpoint");
                }
            }
            else
            {
                _logger.LogError("Failed to authenticate with review API. Status: {StatusCode}. Content: {Content}", 
                    response.StatusCode, responseContent);
                throw new InvalidOperationException($"Failed to authenticate with review API: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication with review API. Message: {Message}", ex.Message);
            throw;
        }
    }
    */

    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string requestUri)
    {
        // Temporarily simplified for mock API
        var request = new HttpRequestMessage(method, requestUri);
        if (!string.IsNullOrEmpty(_apiKey))
            request.Headers.Add("X-API-KEY", _apiKey);
        return request;
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId)
    {
        try
        {
            // Ensure the URL is properly constructed
            string requestUrl = $"{_baseUrl.TrimEnd('/')}/api/products/{productId}/reviews?code={_apiKey}";
            _logger.LogInformation("Fetching reviews for product {ProductId} from {Url}", productId, requestUrl);
            
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
            var httpResponse = await _httpClient.SendAsync(request);

            if (httpResponse.IsSuccessStatusCode)
            {
                var reviewResponse = await httpResponse.Content.ReadFromJsonAsync<ReviewResponseDto>();
                var reviews = (reviewResponse?.Reviews ?? new List<ReviewDto>()).Where(r => r != null).ToList();
                _logger.LogInformation("Successfully retrieved {Count} reviews for product {ProductId}", reviews?.Count ?? 0, productId);
                return reviews;
            }
            else
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to fetch reviews for product {ProductId}. Status: {StatusCode}. Reason: {ReasonPhrase}. Content: {Content}", 
                    productId, httpResponse.StatusCode, httpResponse.ReasonPhrase, responseContent);
                return new List<ReviewDto>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching reviews for product {ProductId}. Message: {Message}", productId, ex.Message);
            return new List<ReviewDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching reviews for product {ProductId}. Message: {Message}", productId, ex.Message);
            return new List<ReviewDto>();
        }
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        try
        {
            _logger.LogInformation("Starting review submission for product {ProductId}. Base URL: {BaseUrl}", review.ProductId, _baseUrl);
            
            if (string.IsNullOrEmpty(_baseUrl))
            {
                _logger.LogError("Review API URL is not configured");
                return false;
            }

            // Ensure the URL is properly constructed
            var postReviewUrl = $"{_baseUrl.TrimEnd('/')}/products/{review.ProductId}/review";
            _logger.LogInformation("Submitting review to URL: {Url}", postReviewUrl);
            
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, postReviewUrl);

            // Map to mock API format
            var apiReview = new
            {
                reviewerName = review.CustomerName,
                text = review.Content,
                rating = review.Rating,
                reviewDate = review.CreatedAt
            };

            request.Content = JsonContent.Create(apiReview);
            _logger.LogInformation("Review payload prepared: {@ReviewPayload}", apiReview);

            var httpResponse = await _httpClient.SendAsync(request);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            
            _logger.LogInformation("API Response - Status: {StatusCode}, Content: {Content}", 
                httpResponse.StatusCode, responseContent);

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully submitted review for product {ProductId}", review.ProductId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to submit review for product {ProductId}. Status: {StatusCode}. Reason: {ReasonPhrase}. Content: {ErrorContent}", 
                    review.ProductId, httpResponse.StatusCode, httpResponse.ReasonPhrase, responseContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error submitting review for product {ProductId}. Message: {Message}", 
                review.ProductId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting review for product {ProductId}. Message: {Message}", 
                review.ProductId, ex.Message);
            return false;
        }
    }

    public async Task<int?> RegisterProductAsync(ProductDto product)
    {
        // Adjust the endpoint and payload to match the external API spec!
        var url = $"{_baseUrl.TrimEnd('/')}/product/save";
        var payload = new
        {
            name = product.Name,
            description = product.Description,
            price = product.Price,
            // Add other fields as needed
        };

        using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, url);
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to register product. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, await response.Content.ReadAsStringAsync());
            return null;
        }

        // Adjust this to match the actual response structure!
        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("productId", out var idProp))
            return idProp.GetInt32();

        _logger.LogWarning("No productId found in register product response: {Response}", responseBody);
        return null;
    }

    // Commenting out external API response model for now
    /*
    private class AuthResponse
    {
        public string? Token { get; set; }
    }
    */
}