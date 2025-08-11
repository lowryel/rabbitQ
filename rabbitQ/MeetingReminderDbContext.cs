using System;
using Microsoft.EntityFrameworkCore;

namespace rabbitQ;

public class MeetingReminderDbContext(DbContextOptions<MeetingReminderDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts { get; set; }
}

