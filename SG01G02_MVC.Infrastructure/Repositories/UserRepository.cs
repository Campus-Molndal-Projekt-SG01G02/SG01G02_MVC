using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;
using SG01G02_MVC.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace SG01G02_MVC.Infrastructure.Repositories
{
    /// Retrieves users from the database and validates passwords.
    /// TODO: Replace SHA256 with a stronger algorithm post-MVP (e.g., bcrypt, PBKDF2).
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _dbContext;

        public UserRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public AppUser? GetByUsername(string username)
        {
            return _dbContext.AppUsers.FirstOrDefault(u => u.Username == username);
        }

        public bool ValidatePassword(AppUser user, string plainTextPassword)
        {
            string hash = HashPassword(plainTextPassword);
            return user.PasswordHash == hash;
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}