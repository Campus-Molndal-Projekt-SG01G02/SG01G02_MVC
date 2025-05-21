using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SG01G02_MVC.Infrastructure.External
{
    /// <summary>
    /// A disabled fallback implementation of IReviewApiClient that simulates the external API being unavailable.
    /// Used to trigger mock fallback.
    /// </summary>
    public class DisabledReviewApiClient : IReviewApiClient
    {
        public Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId)
            => throw new InvalidOperationException("ExternalReviewApi is disabled");

        public Task<bool> SubmitReviewAsync(ReviewDto review)
            => throw new InvalidOperationException("ExternalReviewApi is disabled");

        public Task<int?> RegisterProductAsync(ProductDto product)
            => throw new InvalidOperationException("ExternalReviewApi is disabled");
    }
}