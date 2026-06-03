using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;

namespace HamaStudio.Services
{
    public class EmailService
    {
        // Cấu hình SMTP Gmail
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SenderEmail = "qpham159753@gmail.com"; // Thay bằng email của bạn
        private const string AppPassword = "klas akno mkto bbob";    // Thay bằng mật khẩu ứng dụng Gmail

        public static async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(SmtpServer)
                {
                    Port = SmtpPort,
                    Credentials = new NetworkCredential(SenderEmail, AppPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(SenderEmail, "Hama Studio"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
            }
        }
    }
}
