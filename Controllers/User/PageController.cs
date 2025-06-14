using Microsoft.AspNetCore.Mvc;

namespace thuvienso.Controllers.User
{
    [Route("page")]
    public class PageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("about")]
        public IActionResult About()
        {
            return View("~/Views/User/Page/About.cshtml");
        }
    }
}
