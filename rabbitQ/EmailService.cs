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
        using var smtp = new SmtpClient();
        try
        {
            // Set shorter timeout (30 seconds)
            smtp.Timeout = 30000;

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_config.FromName, _config.FromEmail));
            email.To.Add(new MailboxAddress("", message.To));
            email.Subject = message.Subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Body };

            // Add cancellation token to prevent hanging
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var secureSocketOptions = _config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await smtp.ConnectAsync(_config.SmtpServer, _config.SmtpPort, secureSocketOptions, cts.Token);

            if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                await smtp.AuthenticateAsync(_config.Username, _config.Password, cts.Token);
            }

            await smtp.SendAsync(email, cts.Token);
            _logger.LogInformation("Email sent successfully to {EmailAddress}", message.To);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("Email sending timed out for {EmailAddress}: {Message}", message.To, ex.Message);
            // Don't rethrow
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning("Email sending timed out for {EmailAddress}: {Message}", message.To, ex.Message);
            // Don't rethrow
        }
        catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
        {
            _logger.LogWarning("Email address does not exist: {EmailAddress}", message.To);
            // Don't rethrow
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {EmailAddress}", message.To);
            // Don't rethrow to keep the queue processing
        }
        finally
        {
            try
            {
                if (smtp.IsConnected)
                {
                    await smtp.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error disconnecting SMTP client: {Message}", ex.Message);
            }
        }
    }
}

