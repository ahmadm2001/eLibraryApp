using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace eLibraryApp.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Sends an email asynchronously.
        /// </summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="subject">Subject of the email.</param>
        /// <param name="message">HTML message body.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SendEmailAsync(string toEmail, string subject, string message, List<string> attachments = null)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("eBook Library Service", _configuration["EmailSettings:SenderEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message
            };

            // Attach files
            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    bodyBuilder.Attachments.Add(attachment);
                }
            }

            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:Port"]), MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_configuration["EmailSettings:SenderEmail"], _configuration["EmailSettings:SenderPassword"]);
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }


    }
}
