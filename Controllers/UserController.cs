using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace thuvienso.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/users")]
public class UserController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(); // List users

    [HttpGet("create")]
    public IActionResult Create() => View();

    [HttpPost("create")]
    public IActionResult Store() => RedirectToAction("Index");

    [HttpGet("edit/{id}")]
    public IActionResult Edit(int id) => View();

    [HttpPost("edit/{id}")]
    public IActionResult Update(int id) => RedirectToAction("Index");

    [HttpPost("delete/{id}")]
    public IActionResult Delete(int id) => RedirectToAction("Index");
}
