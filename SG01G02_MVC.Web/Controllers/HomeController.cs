using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Web.Models;

namespace SG01G02_MVC.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        
        // TEMPORARILY COMMENTED OUT FOR CI/CD BUILD FIX
        // private readonly AppDbContext _context;

        // public HomeController(ILogger<HomeController> logger, AppDbContext context)
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            // _context = context;
        }

        // TEMPORARY PATCH: DISABLED DB TEST ENDPOINT
        // [HttpGet("dbinfo")]
        // public IActionResult DbInfo()
        // {
        //     var provider = _context.Database.ProviderName ?? "Unknown";
        //     var canConnect = _context.Database.CanConnect();
        //     var dbName = _context.Database.GetDbConnection().Database;
        //
        //     return Content($"Provider: {provider}\nDatabase: {dbName}\nCanConnect: {canConnect}");
        // }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            try
            {
                var healthStatus = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }
    }
}