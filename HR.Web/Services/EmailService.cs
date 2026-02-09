using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HR.Web.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendPasswordResetEmailAsync(string to, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly bool _enableSsl;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService()
        {
            // Read from Web.config - fallback to defaults for development
            _smtpHost = ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            _smtpUser = ConfigurationManager.AppSettings["SmtpUser"] ?? "";
            _smtpPass = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
            _enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true");
            _fromEmail = ConfigurationManager.AppSettings["FromEmail"] ?? "noreply@nanosoft.com";
            _fromName = ConfigurationManager.AppSettings["FromName"] ?? "Nanosoft HR System";
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.EnableSsl = _enableSsl;
                    client.UseDefaultCredentials = false;
                    
                    if (!string.IsNullOrEmpty(_smtpUser) || !string.IsNullOrEmpty(_smtpPass))
                    {
                        client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                    }

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail, _fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(to);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - email failure shouldn't break the app
                System.Diagnostics.Debug.WriteLine("Email sending failed: " + ex.Message);
                // In production, you'd want proper logging here
            }
        }

        public async Task SendPasswordResetEmailAsync(string to, string resetLink)
        {
            var subject = "Password Reset Request - Nanosoft HR System";
            var body = string.Format(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Password Reset</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; background: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background: #3498db; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .security-note {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; margin: 20px 0; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Nanosoft HR System</h2>
            <p>Password Reset Request</p>
        </div>
        <div class='content'>
            <p>Hello,</p>
            <p>We received a request to reset your password for your Nanosoft HR System account.</p>
            
            <div class='security-note'>
                <strong>Security Notice:</strong> This password reset link will expire in 24 hours for your security.
            </div>
            
            <p style='text-align: center;'>
                <a href='{0}' class='button'>Reset Your Password</a>
            </p>
            
            <p>If you didn't request this password reset, please ignore this email. Your password will remain unchanged.</p>
            
            <p>If the button above doesn't work, you can copy and paste this link into your browser:</p>
            <p style='word-break: break-all; background: #f0f0f0; padding: 10px; border-radius: 4px;'>{0}</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 Nanosoft Technologies. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>", resetLink);

            await SendAsync(to, subject, body);
        }
    }
}



























