using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using thuvienso.Data;
using thuvienso.Models;
using static QRCoder.PayloadGenerator;


namespace thuvienso.Controllers.Admin;

[Route("admin/auth")]
public class AuthController : Controller
{
    private readonly AppDbContext _context;
    public AuthController(AppDbContext context)
    {
        _context = context;
    }


    [HttpGet("login")]
    public IActionResult Login()
    {
        return View("~/Views/Admin/Auth/Login.cshtml");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
            return View("~/Views/Admin/Auth/Login.cshtml");
        }

        // So sánh trực tiếp, không dùng hash
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu.";
            return View("~/Views/Admin/Auth/Login.cshtml");
        }

        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, (user.Role ?? "user").Trim())
            };
            var identity = new ClaimsIdentity(claims, "AdminAuth");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("AdminAuth", principal);
            return Redirect("/admin/dashboard");
        }

        ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu.";
        return View("~/Views/Admin/Auth/Login.cshtml");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminAuth");
        return Redirect("/admin/auth/login");
    }

    [HttpGet("denied")]
    public IActionResult Denied() => Content("Bạn không có quyền.");
}

