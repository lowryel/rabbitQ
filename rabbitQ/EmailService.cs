using System;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace rabbitQ;


public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
}

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message);
}


public class EmailService(IOptions<EmailConfig> config, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailConfig _config = config.Value;
    private readonly ILogger<EmailService> _logger = logger;

    public async Task SendEmailAsync(EmailMessage message)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_config.FromName, _config.FromEmail));
            email.To.Add(new MailboxAddress("", message.To));
            email.Subject = message.Subject;

            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message.Body
            };

            using var smtp = new SmtpClient();

            // Choose connection type based on EnableSsl setting
            var secureSocketOptions = _config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await smtp.ConnectAsync(_config.SmtpServer, _config.SmtpPort, secureSocketOptions);

            // Only authenticate if username/password are provided
            if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                await smtp.AuthenticateAsync(_config.Username, _config.Password);
            }

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation($"Email sent successfully to {message.To}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {message.To}");
            throw;
        }
    }
}

