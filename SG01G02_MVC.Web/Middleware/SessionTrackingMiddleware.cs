namespace SG01G02_MVC.Web.Middleware;

public class SessionTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionTrackingMiddleware> _logger;

    public SessionTrackingMiddleware(RequestDelegate next, ILogger<SessionTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Define sessionId once and reuse it
        string sessionId;

        var existingSessionId = context.Session.GetString("SessionId");
        if (string.IsNullOrEmpty(existingSessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            context.Session.SetString("SessionId", sessionId);
        }
        else
        {
            sessionId = existingSessionId;
        }

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["SessionId"] = sessionId,
            ["RequestPath"] = context.Request.Path.Value ?? "",
            ["RequestMethod"] = context.Request.Method,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["Referrer"] = context.Request.Headers.Referer.ToString(),
            ["ClientIP"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        }))
        {
            _logger.LogInformation("Request started");

            try
            {
                await _next(context);
                _logger.LogInformation("Request completed with status code {StatusCode}", context.Response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed");
                throw;
            }
        }
    }
}

// Extension method to make it easier to use the middleware
public static class SessionTrackingMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionTrackingMiddleware>();
    }
}