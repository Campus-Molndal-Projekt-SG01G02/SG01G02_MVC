using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SG01G02_MVC.Infrastructure.External;

public class ReviewApiClient : IReviewApiClient
{
    // TODO: Implement Review API client
    
    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId)
    {
        // Implementation
        // return new List<ReviewDto>(); // Placeholder return value
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ReviewDto>> GetReviewsForProductAsync(string productId)
    {
        throw new NotImplementedException();
    }
}