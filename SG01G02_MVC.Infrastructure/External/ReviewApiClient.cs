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
    private string? _jwtToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

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

    private async Task EnsureValidTokenAsync()
    {
        // If we have a valid token that hasn't expired, use it
        if (!string.IsNullOrEmpty(_jwtToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Authenticating with review API to get JWT token");
            var authUrl = $"{_baseUrl}/api/auth/login";
            
            var authRequest = new HttpRequestMessage(HttpMethod.Post, authUrl);
            authRequest.Content = JsonContent.Create(new { apiKey = _apiKey });
            
            var response = await _httpClient.SendAsync(authRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse?.Token != null)
                {
                    _jwtToken = authResponse.Token;
                    // Set token expiry to 55 minutes from now (assuming 1-hour token lifetime)
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
                    _logger.LogInformation("Successfully obtained new JWT token");
                }
                else
                {
                    throw new InvalidOperationException("Received null token from authentication endpoint");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to authenticate with review API. Status: {StatusCode}. Content: {Content}", 
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to authenticate with review API: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication with review API");
            throw;
        }
    }

    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string requestUri)
    {
        await EnsureValidTokenAsync();
        
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
        return request;
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId)
    {
        try
        {
            _logger.LogInformation("Fetching reviews for product {ProductId} from {BaseUrl}", productId, _baseUrl);
            string requestUrl = $"{_baseUrl}/api/products/{productId}/reviews";
            
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
            var httpResponse = await _httpClient.SendAsync(request);

            if (httpResponse.IsSuccessStatusCode)
            {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching reviews for product {ProductId}", productId);
            return new List<ReviewDto>();
        }
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        try
        {
            var postReviewUrl = $"{_baseUrl}/api/products/{review.ProductId}/reviews";
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, postReviewUrl);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting review for product {ProductId}", review.ProductId);
            return false;
        }
    }

    private class AuthResponse
    {
        public string? Token { get; set; }
    }
}