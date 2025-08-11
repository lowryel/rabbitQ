
using MeetingReminderApp.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RabbitMQ.Client;
using rabbitQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MeetingReminderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); 
    


// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure RabbitMQ
builder.Services.Configure<RabbitMQConfig>(
    builder.Configuration.GetSection("RabbitMQ"));

// Configure Email settings
builder.Services.Configure<EmailConfig>(
    builder.Configuration.GetSection("Email"));

// Register services
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IContactService, ContactService>();

// Configure Quartz
builder.Services.AddQuartz(q =>
{

    var jobKey = new JobKey("EndOfMonthReminderJob");
    q.AddJob<EndOfMonthReminderJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("EndOfMonthReminderTrigger")
        .WithCronSchedule("0 0 9 L * ?") // 9 AM on last day of month
    );
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure HTTPS Redirection
var httpsPort = builder.Configuration["ASPNETCORE_HTTPS_PORT"] ?? "7274";
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start RabbitMQ consumer
var rabbitMQService = app.Services.GetRequiredService<IRabbitMQService>();
rabbitMQService.StartConsuming();

app.Run();