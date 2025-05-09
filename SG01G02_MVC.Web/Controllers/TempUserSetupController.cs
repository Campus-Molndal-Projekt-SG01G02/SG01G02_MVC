using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace SG01G02_MVC.Web.Controllers
{
    public class TempUserSetupController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly string _secretKey;

        public TempUserSetupController(AppDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _secretKey = configuration["TempSetup:SecretKey"] ?? throw new InvalidOperationException("TempSetup:SecretKey is not configured");
        }

        [HttpGet]
        public IActionResult Setup(string key)
        {
            if (key != _secretKey)
            {
                return NotFound();
            }
            return View();
        }

        [HttpPost]
        public IActionResult Setup(string key, string username, string password, string role)
        {
            if (key != _secretKey)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                return BadRequest("All fields are required");
            }

            // Validate role
            if (role != "Admin" && role != "Staff" && role != "Customer")
            {
                return BadRequest("Invalid role. Must be Admin, Staff, or Customer");
            }

            // Check if user already exists
            if (_dbContext.AppUsers.Any(u => u.Username == username))
            {
                return BadRequest("Username already exists");
            }

            // Create new user
            var user = new AppUser
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Role = role
            };

            _dbContext.AppUsers.Add(user);
            _dbContext.SaveChanges();

            return Content($"User {username} created successfully with role {role}");
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