using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace thuvienso.Controllers.Admin;

[Authorize(Roles = "admin")]
[Route("admin")]
public class DashboardController : Controller
{
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        return View("Views/Admin/Dashboard.cshtml"); // Views/Admin/Dashboard.cshtml
    }
}
