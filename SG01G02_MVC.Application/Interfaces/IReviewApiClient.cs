using SG01G02_MVC.Application.DTOs;

namespace SG01G02_MVC.Application.Interfaces;

public interface IReviewApiClient
{
    Task<IEnumerable<ReviewDto>> GetReviewsAsync(int productId);
    Task<bool> SubmitReviewAsync(ReviewDto review);
    Task<int?> RegisterProductAsync(ProductDto product);
}