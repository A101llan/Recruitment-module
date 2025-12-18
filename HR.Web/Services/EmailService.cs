using System.Threading.Tasks;

namespace HR.Web.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        public Task SendAsync(string to, string subject, string body)
        {
            // Stub: integrate with SMTP or another email provider in production
            return Task.CompletedTask;
        }
    }
}



















