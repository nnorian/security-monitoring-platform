using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class LogConsumerService : BackgroundService
{
    private readonly ILogger<LogConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _rabbitHost;
    private readonly string? _rabbitUser;
    private readonly string? _rabbitPass;

    public LogConsumerService(ILogger<LogConsumerService> logger, IConfiguration config, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _rabbitHost = config["RABBITMQ_HOST"] ?? "localhost";
        _rabbitUser = config["RABBITMQ_USER"];
        _rabbitPass = config["RABBITMQ_PASS"];
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = _rabbitHost, UserName = _rabbitUser, Password = _rabbitPass };
        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("security-logs", durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var log = JsonSerializer.Deserialize<SecurityLog>(body);

            _logger.LogInformation("Processing log from {Source} with severity {Severity}", log?.Source, log?.Severity);

            if (log?.Severity == "critical" || log?.Severity == "high")
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AlbertDbContext>();

                var alert = new Alert
                {
                    Title = $"High severity log from {log.Source}",
                    Description = log.RawMessage,
                    Severity = log.Severity,
                    SourceIp = log.SourceIp ?? "unknown",
                    MitreAttackTactic = DetectTactic(log)
                };

                db.Alerts.Add(alert);
                await db.SaveChangesAsync();
                _logger.LogWarning("Alert created: {Title}", alert.Title);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync("security-logs", autoAck: false, consumer: consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static string DetectTactic(SecurityLog log)
    {
        var src = log.Source.ToLower();
        if (src.Contains("auth") || src.Contains("login")) return "TA0001 - Initial Access";
        if (src.Contains("firewall")) return "TA0011 - Command and Control";
        return "Unknown";
    }
}
