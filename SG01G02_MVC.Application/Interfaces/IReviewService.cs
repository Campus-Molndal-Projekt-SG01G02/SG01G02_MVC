using SG01G02_MVC.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewDto>> GetReviewsForProduct(int productId);
        Task<bool> SubmitReviewAsync(ReviewDto review);
    }
}