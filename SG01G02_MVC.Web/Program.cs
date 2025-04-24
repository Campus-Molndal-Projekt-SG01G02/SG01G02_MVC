using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Infrastructure.Repositories;
using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Configure Entity Framework Core with SQLite or PostgreSQL
// Use SQLite by default, but allow for PostgreSQL connection string from environment variables
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var postgresConnString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

    if (!string.IsNullOrEmpty(postgresConnString))
    {
        Console.WriteLine("Using PostgreSQL-connection from environment variable");
        options.UseNpgsql(postgresConnString);
    }
    else
    {
        Console.WriteLine("Using SQLite-connection from appsettings");
        options.UseSqlite(connectionString);
    }
});

builder.Services.AddHttpContextAccessor(); // Needed to access HttpContext in services
builder.Services.AddScoped<IUserSessionService, UserSessionService>();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
});

// Add authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Login/Index"; // fallback if unauthenticated
    });

var app = builder.Build();

// Try to connect to the SQLite database, and seed admin user if available.
// If the DB is missing (e.g. during CI/CD), log a warning and render fallback view if needed.

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        if (db.Database.CanConnect())
        {
            Console.WriteLine($"Connected to database with provider: {db.Database.ProviderName}");

            if (db.Database.ProviderName?.Contains("Npgsql") == true)
            {
                Console.WriteLine("Applying PostgreSQL-database migrations...");
                db.Database.Migrate();
            }

            // Seed default admin - NH design, keeping Program.cs clean as in Clean Architecture
            SeederHelper.SeedAdminUser(app);
        }
        else
        {
            Console.WriteLine("WARNING: Could not connect to database. No seeding will occur.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database check failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Use routing, authorization, and static assets
app.UseRouting();
app.UseSession(); // Enables session before authorization - very important!
app.UseAuthentication(); // Handles ClaimsPrincipal + CookieAuth
app.UseAuthorization(); // Enables [Authorize] attribute
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Run the application
app.Run();