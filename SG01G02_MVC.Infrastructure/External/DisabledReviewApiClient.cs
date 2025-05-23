using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.External;

// A fallback implementation of IReviewApiClient that simulates the external API being unavailable.
// Used to trigger mock fallback.
    public class DisabledReviewApiClient : IReviewApiClient
    {
    public Task<IEnumerable<ReviewDto>> GetReviewsAsync(int productId)
        => throw new InvalidOperationException("ExternalReviewApi is disabled");

    public Task<bool> SubmitReviewAsync(ReviewDto review)
        => throw new InvalidOperationException("ExternalReviewApi is disabled");

    public Task<int?> RegisterProductAsync(ProductDto product)
        => throw new InvalidOperationException("ExternalReviewApi is disabled");
}