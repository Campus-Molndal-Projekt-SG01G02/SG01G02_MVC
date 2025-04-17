using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System;
using System.Linq;

namespace SG01G02_MVC.Web.Services
{
    public static class SeederHelper
    {
        public static void SeedAdminUser(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!db.AppUsers.Any(u => u.Role == "Admin"))
            {
                var admin = new AppUser
                {
                    Username = "admin",
                    PasswordHash = HashPassword("password123"), // Change in prod
                    Role = "Admin"
                };

                db.AppUsers.Add(admin);
                db.SaveChanges();
                Console.WriteLine("✅ Seeded default admin user.");
            }
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}