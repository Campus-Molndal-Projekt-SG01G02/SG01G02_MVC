using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Web.Models;
using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Infrastructure.Data;
using System.Collections.Generic;

namespace SG01G02_MVC.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // TODO: We will protect this via [Authorize(Roles = "Admin")] in the future!
        [HttpGet("dbinfo")]
        public IActionResult DbInfo()
{
            _logger.LogInformation("DbInfo endpoint accessed");

            try
            {
                var provider = _context.Database.ProviderName ?? "Unknown";
                var canConnect = _context.Database.CanConnect();
                var dbName = _context.Database.GetDbConnection().Database;

                // Använd strukturerad loggning med extra information
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["DatabaseProvider"] = provider,
                    ["DatabaseName"] = dbName,
                    ["CanConnect"] = canConnect
                }))
                {
                    _logger.LogInformation("Database information retrieved successfully");
                }

                return Content($"Provider: {provider}\nDatabase: {dbName}\nCanConnect: {canConnect}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve database information");
                throw;
            }
        }

        public IActionResult Index()
        {
            try
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["PageName"] = "Home",
                    ["UserAuthenticated"] = User.Identity?.IsAuthenticated ?? false
                }))
                {
                    _logger.LogInformation("Home page accessed");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing home page");
                throw;
            }
        }

        public IActionResult Privacy()
        {
            try
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["PageName"] = "Privacy",
                    ["UserAuthenticated"] = User.Identity?.IsAuthenticated ?? false
                }))
                {
                    _logger.LogInformation("Privacy page accessed");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing privacy page");
                throw;
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["ErrorPath"] = HttpContext.Request.Path
            }))
            {
                _logger.LogError("Error page displayed");
            }

            return View(new ErrorViewModel { RequestId = requestId });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            _logger.LogInformation("Health check endpoint accessed");

            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

                // Kontrollera olika komponenters hälsa
                bool dbHealthy = _context.Database.CanConnect();

                string status = dbHealthy ? "Healthy" : "Degraded";

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["Environment"] = environment,
                    ["Status"] = status,
                    ["DatabaseConnected"] = dbHealthy
                }))
                {
                    _logger.LogInformation("Health check completed with status: {Status}", status);
                }

                var healthStatus = new
                {
                    Status = status,
                    Timestamp = DateTime.UtcNow,
                    Environment = environment,
                    Components = new
                    {
                        Database = dbHealthy ? "Healthy" : "Unhealthy"
                    }
                };

                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed with exception");
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }
    }
}