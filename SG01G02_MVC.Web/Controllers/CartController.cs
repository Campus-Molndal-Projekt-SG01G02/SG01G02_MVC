using Microsoft.AspNetCore.Mvc;

namespace SG01G02_MVC.Web.Controllers;

public class CartController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}