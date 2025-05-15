using Microsoft.Extensions.Diagnostics.HealthChecks;
using SG01G02_MVC.Infrastructure.Data;

namespace SG01G02_MVC.Infrastructure.Services;
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            Console.WriteLine($"Health check - Database connection: {(canConnect ? "Success" : "Failed")}");

            return canConnect
                ? HealthCheckResult.Healthy("Database connection is working.")
                : HealthCheckResult.Unhealthy("Cannot connect to database.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health check - Database error: {ex.Message}");
            return HealthCheckResult.Unhealthy($"Database error: {ex.Message}");
        }
    }
}
