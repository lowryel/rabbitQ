using System;

namespace rabbitQ;


public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}


public interface IContactService
{
    Task<List<Contact>> GetActiveContactsAsync();
}

public class ContactService(ILogger<ContactService> logger) : IContactService
{
    private readonly ILogger<ContactService> _logger = logger;

    public async Task<List<Contact>> GetActiveContactsAsync()
    {
        // In a real application, this would fetch from a database
        // For demo purposes, returning mock data
        var contacts = new List<Contact>
            {
                new() { Id = 1, Name = "John Doe", Email = "ellowry09@gmail.com", Department = "Engineering", IsActive = true },
                // new () { Id = 2, Name = "Jane Smith", Email = "jane.smith@company.com", Department = "Marketing", IsActive = true },
                // new () { Id = 3, Name = "Mike Johnson", Email = "mike.johnson@company.com", Department = "Sales", IsActive = true },
                // new () { Id = 4, Name = "Sarah Wilson", Email = "sarah.wilson@company.com", Department = "HR", IsActive = true }
            };

        _logger.LogInformation($"Retrieved {contacts.Count} active contacts");
        return await Task.FromResult(contacts.Where(c => c.IsActive).ToList());
    }
}
