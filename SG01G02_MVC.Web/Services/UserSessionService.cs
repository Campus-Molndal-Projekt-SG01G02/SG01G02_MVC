using Microsoft.AspNetCore.Http;

namespace SG01G02_MVC.Web.Services
{
    public class UserSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? Username
        {
            get => _httpContextAccessor.HttpContext?.Session.GetString("Username");
            set => _httpContextAccessor.HttpContext?.Session.SetString("Username", value ?? "");
        }

        public string? Role
        {
            get => _httpContextAccessor.HttpContext?.Session.GetString("UserRole");
            set => _httpContextAccessor.HttpContext?.Session.SetString("UserRole", value ?? "");
        }

        public void Clear()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }
    }
}