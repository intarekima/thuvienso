using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using thuvienso.Data;
using thuvienso.Models;

namespace thuvienso.Controllers.User;

[Route("user/auth")]
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
        return View("~/Views/User/Auth/Login.cshtml");
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginPost(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            TempData["Error"] = "Email hoặc mật khẩu không đúng.";
            return Redirect("/user/auth/login");
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Name", user.Name);
        return Redirect("/");
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("UserId");
        return Redirect("/user/auth/login");
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        return View("~/Views/User/Auth/Register.cshtml");
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterPost(string name, string email, string password, string rePassword)
    {
        if(password != rePassword)
        {
            TempData["Error"] = "Mật khẩu không trùng khớp";
            return Redirect("/user/auth/register");
        }
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            TempData["Error"] = "Email đã được sử dụng.";
            return Redirect("/user/auth/register");
        }

        var user = new Models.User
        {
            Name = name,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "user"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return Redirect("/user/auth/login");
    }

    [HttpGet("forgot")]
    public IActionResult ForgotPassword()
    {
        return View("~/Views/User/Auth/ForgotPassword.cshtml");
    }

    [HttpPost("forgot")]
    public async Task<IActionResult> ForgotPasswordPost(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            TempData["Error"] = "Email không tồn tại.";
            return RedirectToAction("ForgotPassword");
        }

        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        user.ResetCode = code;
        user.ResetCodeExpiry = DateTime.Now.AddMinutes(30);
        await _context.SaveChangesAsync();

        // Gửi mail: bạn cần thay bằng hàm gửi thực tế
        Console.WriteLine($"Gửi mã {code} đến {email}");

        TempData["Success"] = "Đã gửi mã xác nhận đến email của bạn.";
        return RedirectToAction("VerifyResetCode", new { email });
    }

    [HttpGet("verify")]
    public IActionResult VerifyResetCode(string email) => View(model: email);

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyResetCodePost(string email, string code, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || user.ResetCode != code || user.ResetCodeExpiry < DateTime.Now)
        {
            TempData["Error"] = "Mã không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction("VerifyResetCode", new { email });
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ResetCode = null;
        user.ResetCodeExpiry = null;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Mật khẩu đã được thay đổi.";
        return RedirectToAction("Login");
    }
}
