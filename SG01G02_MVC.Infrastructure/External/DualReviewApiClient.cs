using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace SG01G02_MVC.Infrastructure.External;

public class DualReviewApiClient : IReviewApiClient
{
    private readonly IReviewApiClient _primary;   // External
    private readonly IReviewApiClient _fallback;  // Mock
    private readonly ILogger<DualReviewApiClient> _logger;

    public DualReviewApiClient(IReviewApiClient primary, IReviewApiClient fallback, ILogger<DualReviewApiClient> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(int productId)
    {
        Console.WriteLine("→ Trying external API...");

        try
        {
            _logger.LogInformation("Trying to fetch reviews from external API...");
            return await _primary.GetReviewsAsync(productId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External API failed, falling back to mock.");
            return await _fallback.GetReviewsAsync(productId);
        }
    }

    public async Task<bool> SubmitReviewAsync(ReviewDto review)
    {
        try
        {
            _logger.LogInformation("Trying to submit review to external API...");
            return await _primary.SubmitReviewAsync(review);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Submit to external failed, falling back.");
            return await _fallback.SubmitReviewAsync(review);
        }
    }

    public async Task<int?> RegisterProductAsync(ProductDto product)
    {
        try
        {
            _logger.LogInformation("Trying to register product with external API...");
            return await _primary.RegisterProductAsync(product);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External register failed, falling back.");
            return await _fallback.RegisterProductAsync(product);
        }
    }
}