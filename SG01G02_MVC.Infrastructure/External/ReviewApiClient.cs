using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SG01G02_MVC.Infrastructure.External;

public class ReviewApiClient : IReviewApiClient
{
    // TODO: Implement Review API client

    public virtual Task<IEnumerable<ReviewDto>> GetReviewsForProductAsync(string productId)
    {
        // Implementation will be added later
        throw new NotImplementedException();
    }
}