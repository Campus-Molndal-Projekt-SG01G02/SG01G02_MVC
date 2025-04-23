using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Services;
using SG01G02_MVC.Web.Models;

namespace SG01G02_MVC.Web.Controllers
{
    public class LoginController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserSessionService _session;

        public LoginController(IAuthService authService, IUserSessionService session)
        {
            _authService = authService;
            _session = session;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _authService.ValidateLogin(model.Username, model.Password);

            if (user == null)
            {
                ViewBag.LoginFailed = true;
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            // Sign in via CookieAuth
            await HttpContext.SignInAsync("CookieAuth", principal);

            // Store in session too (for navbar etc)
            _session.Username = user.Username;
            _session.Role = user.Role;

            return user.Role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Staff" => RedirectToAction("Index", "Staff"),
                "Customer" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            _session.Clear();
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }
    }
}