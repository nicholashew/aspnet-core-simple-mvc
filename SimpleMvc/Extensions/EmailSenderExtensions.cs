using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using SimpleMvc.Services;
using Microsoft.Extensions.Logging;

namespace SimpleMvc.Services
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailService emailService, string email, string link)
        {
            return emailService.SendAsync(email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }

        public static Task SendSystemEmailAsync(this IEmailService emailService, ILogger logger, string subject, string message, bool includeCc)
        {
            if (logger != null)
            {
                logger.LogInformation(message);
            }
            
            return emailService.SendSystemEmailAsync(subject, message, includeCc);
        }
    }
}
