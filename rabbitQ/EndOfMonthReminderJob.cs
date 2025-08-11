// Jobs/EndOfMonthReminderJob.cs
using Quartz;
using rabbitQ;
namespace MeetingReminderApp.Jobs;

public class EndOfMonthReminderJob(
    IRabbitMQService rabbitMQService,
    IContactService contactService,
    ILogger<EndOfMonthReminderJob> logger) : IJob
{
    private readonly IRabbitMQService _rabbitMQService = rabbitMQService;
    private readonly IContactService _contactService = contactService;
    private readonly ILogger<EndOfMonthReminderJob> _logger = logger;

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting end-of-month reminder job");

        try
        {
            var contacts = await _contactService.GetActiveContactsAsync();
            var currentMonth = DateTime.Now.ToString("MMMM yyyy");

            foreach (var contact in contacts)
            {
                var emailMessage = new EmailMessage
                {
                    To = contact.Email,
                    Subject = $"Monthly Team Meeting Reminder - {currentMonth}",
                    Body = GenerateEmailBody(contact.Name, currentMonth),
                    FromName = "Meeting Reminder System",
                    ScheduledFor = DateTime.Now
                };

                await _rabbitMQService.PublishEmailAsync(emailMessage);
            }

            _logger.LogInformation($"End-of-month reminder job completed. {contacts.Count} emails queued.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing end-of-month reminder job");
            throw;
        }
    }

    private static string GenerateEmailBody(string recipientName, string monthYear)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #2c5aa0; border-bottom: 2px solid #2c5aa0; padding-bottom: 10px;'>
                    Monthly Team Meeting Reminder
                </h2>
                
                <p>Dear {recipientName},</p>
                
                <p>This is a friendly reminder about our upcoming monthly team meeting for <strong>{monthYear}</strong>.</p>
                
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Meeting Details:</h3>
                    <ul style='margin-bottom: 0;'>
                        <li><strong>Date:</strong> Last Friday of the month</li>
                        <li><strong>Time:</strong> 2:00 PM - 3:00 PM</li>
                        <li><strong>Location:</strong> Conference Room A / Teams Meeting</li>
                        <li><strong>Agenda:</strong> Monthly review, updates, and planning</li>
                    </ul>
                </div>
                
                <p>Please confirm your attendance and let us know if you have any agenda items to discuss.</p>
                
                <p>Thank you!</p>
                
                <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666;'>
                    <p>This is an automated reminder. Please do not reply to this email.</p>
                </div>
            </div>
        </body>
        </html>";
    }
}

