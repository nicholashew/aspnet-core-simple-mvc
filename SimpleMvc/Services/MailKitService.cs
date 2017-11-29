using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SimpleMvc.Config;
using SimpleMvc.Extensions.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.Services
{
    public class MailKitService : IEmailService
    {
        private readonly EmailSettings _settings;
        private ILogger<MailKitService> _logger;

        public MailKitService(IOptions<EmailSettings> emailSettings, ILogger<MailKitService> logger)
        {
            _settings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string mailTo, string subject, string message)
        {
            return await SendAsync(mailTo, null, null, subject, message);
        }

        public async Task<bool> SendAsync(string mailTo, List<string> mailCc, List<string> mailBcc, string subject, string message)
        {
            if (_settings.UseDebugEmail)
            {
                _logger.LogInformation($"MailKitService Sending to {mailTo}, cc to {mailCc.ToJson()}, bcc to {mailBcc.ToJson()} for {subject}: {Environment.NewLine}{message}");
                return true;
            }

            try
            {
                var mimeMessage = new MimeMessage
                {
                    Subject = subject,
                    Body = new TextPart("Html") { Text = message }
                };

                mimeMessage.From.Add(new MailboxAddress(_settings.SenderEmail, _settings.SenderEmail));
                mimeMessage.To.Add(new MailboxAddress(mailTo, mailTo));

                if (mailCc != null && mailCc.Any())
                {
                    foreach (string cc in mailCc)
                    {
                        if (!string.IsNullOrWhiteSpace(cc))
                            mimeMessage.Cc.Add(new MailboxAddress(cc, cc));
                    }
                }

                if (mailBcc != null && mailBcc.Any())
                {
                    foreach (string bcc in mailBcc)
                    {
                        if (!string.IsNullOrWhiteSpace(bcc))
                            mimeMessage.Bcc.Add(new MailboxAddress(bcc, bcc));
                    }
                }

                using (var client = new SmtpClient())
                {
                    client.Connect(_settings.Server, _settings.Port, SecureSocketOptions.None);
                    if (!string.IsNullOrWhiteSpace(_settings.Account) && !string.IsNullOrWhiteSpace(_settings.Password))
                    {
                        client.Authenticate(_settings.Account, _settings.Password);
                    }
                    await client.SendAsync(mimeMessage);
                    client.Disconnect(true);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Exception Thrown sending message via MailKitService. mailTo: {mailTo}, cc: {mailCc.ToJson()}, bcc: {mailBcc.ToJson()} subject: {subject}, message: {message}");
                return false;
            }
        }
    }
}
