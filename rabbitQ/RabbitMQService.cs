// Services/RabbitMQService.cs
using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace rabbitQ;

public interface IRabbitMQService
{
    Task PublishEmailAsync(EmailMessage message);
    void StartConsuming();
    void StopConsuming();
}

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly RabbitMQConfig _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQService(IOptions<RabbitMQConfig> config, IServiceProvider serviceProvider, ILogger<RabbitMQService> logger)
    {
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: _config.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("RabbitMQ connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
        }
    }

    public async Task PublishEmailAsync(EmailMessage message)
    {
        try
        {
            if (_channel == null)
            {
                _logger.LogError("RabbitMQ channel is not initialized");
                return;
            }

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: _config.QueueName,
                basicProperties: properties,
                body: body);

            _logger.LogInformation($"Email message published for {message.To}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish email message for {message.To}");
        }
    }

    public void StartConsuming()
    {
        try
        {
            if (_channel == null)
            {
                _logger.LogError("Cannot start consuming - RabbitMQ channel is not initialized");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var emailMessage = JsonSerializer.Deserialize<EmailMessage>(message);
                    if (emailMessage != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        await emailService.SendEmailAsync(emailMessage);

                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        _logger.LogInformation($"Email sent successfully to {emailMessage.To}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process email message: {message}");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _config.QueueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Started consuming messages from RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming messages");
        }
    }

    public void StopConsuming()
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("Stopped consuming messages from RabbitMQ");
    }

    public void Dispose()
    {
        StopConsuming();
    }
}

