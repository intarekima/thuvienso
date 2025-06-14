using Microsoft.EntityFrameworkCore;
using thuvienso.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== Đăng ký Razor Pages, Cookie Auth, EF Core, Session =====
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication("AdminAuth")
    .AddCookie("AdminAuth", options =>
    {
        options.LoginPath = "/admin/auth/login";
        options.LogoutPath = "/admin/auth/logout";
        options.AccessDeniedPath = "/admin/auth/denied";
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// ✅ Session cho người dùng cuối
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddAuthorization();

var app = builder.Build();

// ===== Middleware xử lý static, routing, session, auth =====
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // ✅ Bắt buộc cho User session
app.UseAuthentication();
app.UseAuthorization();

// ===== Middleware kiểm tra truy cập admin =====
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isAdminArea = path.StartsWithSegments("/admin");

    if (isAdminArea && path == "/admin")
    {
        context.Response.Redirect("/admin/dashboard");
        return;
    }

    var isLoggedIn = context.User?.Identity?.IsAuthenticated ?? false;
    if (isAdminArea && !isLoggedIn &&
        !path.StartsWithSegments("/admin/auth") &&
        context.Request.Method == "GET")
    {
        context.Response.Redirect("/admin/auth/login");
        return;
    }

    await next();
});

// ===== Middleware kiểm tra truy cập người dùng cuối (session-based) =====
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isUserProtected = path.StartsWithSegments("/user/profile")
                        || path.StartsWithSegments("/user/payment")
                        || path.StartsWithSegments("/document");

    var userId = context.Session.GetInt32("UserId");
    if (isUserProtected && userId == null && context.Request.Method == "GET")
    {
        context.Response.Redirect("/user/auth/login");
        return;
    }

    await next();
});

// ===== Route mặc định người dùng cuối =====
app.MapControllerRoute(
    name: "user",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
