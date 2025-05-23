using Microsoft.AspNetCore.Mvc;

namespace SG01G02_MVC.Web.Controllers;

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

        // Extend to list all orders (read-only) once OrderService is ready.
        // Role: Staff (only)
    }
}