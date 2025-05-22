using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using SG01G02_MVC.Infrastructure.Configuration;
using SG01G02_MVC.Web.Middleware;

namespace SG01G02_MVC.Web.Configuration;

public class AppConfigurator
{
    public void Configure(WebApplication app, DatabaseConfigurator databaseConfig)
    {
        ConfigureExceptionHandling(app);
        ConfigureMiddleware(app);
        ConfigureRouting(app);
        ConfigureHealthChecks(app);
        ConfigureFileUploadMiddleware(app);

        // Initialize database
        databaseConfig.Initialize(app);

        ConfigureSwagger(app);
    }

    private void ConfigureExceptionHandling(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var error = context.Features.Get<IExceptionHandlerFeature>();
                    var ex = error?.Error;

                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;
                    var sessionId = context.Session.GetString("SessionId") ?? "unknown";

                    using (logger.BeginScope(new Dictionary<string, object>
                           {
                               ["SessionId"] = sessionId,
                               ["RequestPath"] = context.Request.Path.Value ?? "",
                               ["StatusCode"] = context.Response.StatusCode
                           }))
                    {
                        logger.LogError(exception, "Ohanterat undantag: {Message}", exception?.Message);
                    }

                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("Ett fel inträffade. Vänligen försök igen senare.");
                });
            });
        }
    }

    private void ConfigureMiddleware(WebApplication app)
    {
        app.UseRouting();
        app.UseSession();
        app.UseSessionTracking();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStaticFiles();
    }

    private void ConfigureRouting(WebApplication app)
    {
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
    }

    private void ConfigureHealthChecks(WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    Status = report.Status.ToString(),
                    Checks = report.Entries.Select(e => new
                    {
                        Name = e.Key,
                        Status = e.Value.Status.ToString(),
                    })
                }));
            }
        });
    }

    private void ConfigureFileUploadMiddleware(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var controllerAction = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                if (controllerAction != null)
                {
                    var actionName = controllerAction.ActionName.ToLower();
                    var controllerName = controllerAction.ControllerName.ToLower();

                    bool isUploadRelated =
                        actionName.Contains("upload") ||
                        actionName.Contains("add") ||
                        actionName.Contains("edit") ||
                        actionName.Contains("create") ||
                        controllerName.Contains("admin");

                    if (isUploadRelated)
                    {
                        var bodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                        if (bodySizeFeature != null && bodySizeFeature.IsReadOnly == false)
                        {
                            bodySizeFeature.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB
                            Console.WriteLine($"Increased request size limit for path: {context.Request.Path}");
                        }
                    }
                }
            }

            await next();
        });
    }

    private void ConfigureSwagger(WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}