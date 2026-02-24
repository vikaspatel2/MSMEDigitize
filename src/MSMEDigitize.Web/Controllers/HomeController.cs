using Microsoft.AspNetCore.Mvc;

namespace MSMEDigitize.Web.Controllers;

public class HomeController : Controller
{
    // Redirect root "/" to Login page
    public IActionResult Index() => RedirectToAction("Login", "Account");

    public IActionResult Error() => View();
}
