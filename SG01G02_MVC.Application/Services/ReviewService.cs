using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SG01G02_MVC.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewApiClient _apiClient;
        public ReviewService(IReviewApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<IEnumerable<ReviewDto>> GetReviewsForProduct(string productId)
        {
            throw new NotImplementedException();
        }
    }
}

