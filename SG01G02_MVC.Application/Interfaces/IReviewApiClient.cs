using SG01G02_MVC.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface IReviewApiClient
    {
        Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId);
        Task<bool> SubmitReviewAsync(ReviewDto review);
        Task<int?> RegisterProductAsync(ProductDto product);
    }
}