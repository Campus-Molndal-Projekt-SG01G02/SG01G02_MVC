using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index(LoginViewModel model)
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
        public IActionResult Logout()
        {
            _session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}