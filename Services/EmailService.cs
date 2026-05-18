// ============================================================
// Services/EmailService.cs
// Envoi d'emails via SMTP Gmail — simple et gratuit
// ============================================================

using System.Net;
using System.Net.Mail;

namespace MoneyTransferApp.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = _config["Email:From"] ?? "moneymoney.noreply@gmail.com";
                var password = _config["Email:Password"] ?? "";

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(fromEmail, password),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Money Money"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(toEmail);

                await smtp.SendMailAsync(mail);
            }
            catch
            {
                // On ne bloque JAMAIS l'app si l'email échoue
                // (adresse fictive, pas de config SMTP, etc.)
            }
        }
    }
}