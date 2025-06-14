using Microsoft.AspNetCore.Mvc;

namespace thuvienso.Controllers.User
{
    [Route("user/profile")]
    public class ProfileController : Controller
    {

        public IActionResult Index()
        {
            return View("~/Views/User/Profile.cshtml");
        }
    }
}
