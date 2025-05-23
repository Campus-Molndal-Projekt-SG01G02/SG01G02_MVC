using Microsoft.Extensions.Configuration;
using SG01G02_MVC.Application.Interfaces;

public class FeatureToggleService : IFeatureToggleService
{
    private readonly IConfiguration _config;

    public FeatureToggleService(IConfiguration config)
    {
        _config = config;
    }

    public bool UseMockReviewApi()
    {
        return bool.TryParse(_config["UseMockApi"], out var result) && result;
    }
}