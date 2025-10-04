using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Interface
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName, string username, string password);
        Task<bool> SendPasswordResetEmailAsync(string toEmail);
    }
}
