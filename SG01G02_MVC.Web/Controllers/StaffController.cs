using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace SG01G02_MVC.Web.Controllers
{
    // Controller uses no async dependencies (yet)

    public class StaffController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Staff")
            {
                return RedirectToAction("Index", "Home");
            }

            // For now, show a placeholder message
            ViewBag.Message = "Welcome, staff member! Order list will appear here.";
            return View();

            /// TODO: Extend to list all orders (read-only) once OrderService is ready.
            /// Role: Staff (only)
        }
    }
}