using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace SG01G02_MVC.Infrastructure.External;

public class ReviewApiClient : IReviewApiClient
{
    private readonly HttpClient _httpClient;

    public ReviewApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(string productId)
    {
        // TODO Fix this..
        await Task.Delay(1); 
        return new List<ReviewDto>(); 

    }
}