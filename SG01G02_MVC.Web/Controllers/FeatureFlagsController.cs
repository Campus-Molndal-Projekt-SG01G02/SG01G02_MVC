using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureToggleService _featureToggleService;
    
    public FeatureFlagsController(IFeatureToggleService featureToggleService)
    {
        _featureToggleService = featureToggleService;
    }
    
    [HttpGet]
    public IActionResult GetFeatureFlags()
    {
        return Ok(new
        {
            UseMockReviewApi = _featureToggleService.UseMockReviewApi()
        });
    }
}