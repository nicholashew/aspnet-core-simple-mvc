using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleMvc.Services
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string mailTo, string subject, string message);
        Task<bool> SendAsync(string mailTo, List<string> mailCc, List<string> mailBcc, string subject, string message);
    }
}
