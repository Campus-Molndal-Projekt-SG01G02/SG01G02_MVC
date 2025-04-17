using Xunit;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly IAuthService _authService;

        public AuthServiceTests()
        {
            _authService = new AuthService(new FakeUserRepository());
        }

        [Theory]
        [InlineData("admin", "password123", true)]
        [InlineData("admin", "wrongpassword", false)]
        [InlineData("unknown", "password123", false)]
        public void ValidateLogin_ReturnsExpectedResult(string username, string password, bool expected)
        {
            // Act
            var result = _authService.ValidateLogin(username, password);

            // Assert
            Assert.Equal(expected, result != null);
        }
    }

    /// Fake implementation of IUserRepository for testing purposes
    internal class FakeUserRepository : IUserRepository
    {
        private readonly AppUser _adminUser = new()
        {
            Id = 1,
            Username = "admin",
            PasswordHash = HashPassword("password123"),
            Role = "Admin"
        };

        public AppUser? GetByUsername(string username)
        {
            return username == _adminUser.Username ? _adminUser : null;
        }

        public bool ValidatePassword(AppUser user, string plainTextPassword)
        {
            return user.PasswordHash == HashPassword(plainTextPassword);
        }

        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return System.Convert.ToBase64String(hash);
        }
    }
}