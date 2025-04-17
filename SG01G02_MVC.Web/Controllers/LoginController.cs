using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Services;

namespace SG01G02_MVC.Web.Controllers
{
    public class LoginController : Controller
    {
        private readonly IAuthService _authService;
        private readonly UserSessionService _session;

        public LoginController(IAuthService authService, UserSessionService session)
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
        public IActionResult Index(string username, string password)
        {
            var user = _authService.ValidateLogin(username, password);

            if (user == null)
            {
                ViewBag.LoginFailed = true;
                return View();
            }

            _session.Username = user.Username;
            _session.Role = user.Role;

            // Redirect based on role
            return user.Role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Staff" => RedirectToAction("Index", "Home"), // TODO: Implement Staff page and adjust to "Staff"
                "Customer" => RedirectToAction("Index", "Home"), // or a future Account page
                _ => RedirectToAction("Index", "Home")
            };
        }

        [HttpPost]
        public IActionResult Logout()
        {
            _session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}