// Controllers/ReminderController.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using rabbitQ;

namespace MeetingReminderApp.Controllers;

[ApiController]
public class ReminderController(IRabbitMQService rabbitMQService, IContactService contactService, ILogger<ReminderController> logger) : ControllerBase
{
    private readonly IRabbitMQService _rabbitMQService = rabbitMQService;
    private readonly IContactService _contactService = contactService;
    private readonly ILogger<ReminderController> _logger = logger;

    [HttpPost("send-test-reminder")]
    public async Task<IActionResult> SendTestReminder([FromBody] SendReminderRequest request)
    {
        _logger.LogInformation("Received request to send test reminder to {Email}", request.Email);
        try
        {
            var message = new EmailMessage
            {
                To = request.Email,
                Subject = "Test Meeting Reminder",
                Body = "<p>This is a test meeting reminder email.</p>",
                FromName = "Meeting Reminder System",
                ScheduledFor = DateTime.Now
            };

            await _rabbitMQService.PublishEmailAsync(message);
            return Ok(new { Message = "Test reminder queued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test reminder");
            return StatusCode(500, new { Error = "Failed to queue test reminder" });
        }
    }

    [HttpPost("trigger-monthly-reminders")]
    public async Task<IActionResult> TriggerMonthlyReminders()
    {
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
                    Body = $"<p>Dear {contact.Name},</p><p>This is your monthly meeting reminder.</p>",
                    FromName = "Meeting Reminder System",
                    ScheduledFor = DateTime.Now
                };

                await _rabbitMQService.PublishEmailAsync(emailMessage);
            }

            return Ok(new { Message = $"{contacts.Count} reminders queued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger monthly reminders");
            return StatusCode(500, new { Error = "Failed to queue monthly reminders" });
        }
    }

    [HttpGet("contacts")]
    public async Task<IActionResult> GetContacts()
    {
        try
        {
            var contacts = await _contactService.GetActiveContactsAsync();
            return Ok(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve contacts");
            return StatusCode(500, new { Error = "Failed to retrieve contacts" });
        }
    }
}

public class SendReminderRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
