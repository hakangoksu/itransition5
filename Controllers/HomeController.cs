using Microsoft.AspNetCore.Mvc;

namespace UserManagement.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Admin");
        }
        return RedirectToAction("Login", "Account");
    }
}
