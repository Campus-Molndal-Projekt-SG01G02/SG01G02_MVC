using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Services
{
    /// Authenticates users using the IUserRepository.
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public AppUser? ValidateLogin(string username, string password)
        {
            var user = _userRepository.GetByUsername(username);

            if (user is null)
                return null;

            return _userRepository.ValidatePassword(user, password) ? user : null;
        }
    }
}