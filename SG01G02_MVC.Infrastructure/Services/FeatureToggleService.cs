using Microsoft.Extensions.Configuration;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.Services;
public class FeatureToggleService : IFeatureToggleService
{
    private readonly IConfiguration _config;

    public FeatureToggleService(IConfiguration config)
    {
        _config = config;
    }

    public bool UseMockReviewApi()
    {
        // Try the structured configuration first
        var structuredValue = _config["FeatureToggles:UseMockApi"];
        if (!string.IsNullOrEmpty(structuredValue))
        {
            return bool.TryParse(structuredValue, out var result) && result;
        }

        // Fall back to the flat configuration
        var flatValue = _config["UseMockApi"];
        return bool.TryParse(flatValue, out var flatResult) && flatResult;
    }
}
