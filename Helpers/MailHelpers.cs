using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace thuvienso.Helpers
{
    public class MailHelper
    {
        private readonly IConfiguration _config;

        public MailHelper(IConfiguration config)
        {
            _config = config;
        }

        public void Send(string to, string subject, string body)
        {
            var smtpSection = _config.GetSection("Smtp");

            var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
            {
                Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Pass"]),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(smtpSection["User"], "Thư viện số"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            client.Send(message);
        }
    }
}
