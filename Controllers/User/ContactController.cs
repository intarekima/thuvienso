using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuvienso.Data;
using thuvienso.Helpers;
using thuvienso.Models;

namespace thuvienso.Controllers.User
{
    [Route("contact")]
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public ContactController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/User/Contact/Index.cshtml");
        }


        [HttpPost("")]
        public async Task<IActionResult> SubmitContact(string name, string email, string phone, string subject, string message)
        {
            var contact = new Contact
            {
                Name = name,
                Email = email,
                Subject = subject,
                Phone = phone,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            var mail = new MailHelper(_config);
            
            string toEmail = contact.Email;
            string mailBody = $@"
                            <!DOCTYPE html>
                            <html>
                            <body style='font-family: Arial, sans-serif; background-color: #f8f9fa; padding: 30px;'>
                              <div style='max-width:600px;margin:0 auto;background:#fff;border-radius:6px;border:1px solid #dee2e6;overflow:hidden;'>
                                <div style='background:#0d6efd;color:white;padding:20px 30px;'>
                                  <h2 style='margin:0;font-size:20px;text-align: center'>Chúng tôi sẽ liên hệ lại sớm nhất có thể</h2>
                                </div>
                                <div style='padding:30px;'>
                                  <h3 style='color:#0d6efd;'>Liên hệ mới từ {name}</h3>
                                  <p><strong>Email:</strong> {email}</p>
                                  <p><strong>SĐT:</strong> {phone}</p>
                                  <p><strong>Tiêu đề:</strong> {subject}</p>
                                  <p><strong>Nội dung:</strong></p>
                                  <div style='background:#f1f3f5;padding:12px 16px;border-left:4px solid #0d6efd;border-radius:4px;white-space:pre-line;'>{message}</div>
                                </div>
                                <div style='background:#f1f3f5;color:#6c757d;text-align:center;padding:15px;'>
                                  <small>© 2025 <strong>Thư Viện Số</strong> – Email tự động, vui lòng không trả lời</small>
                                </div>
                              </div>
                            </body>
                            </html>";


            mail.Send(toEmail, $"Phản hồi của Thư Viện Số: {subject}", mailBody);

            TempData["Success"] = "✅ Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi sớm nhất!";
            return Redirect("/contact");
        }


    }
}
