using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Systen.Text.Json;

public class LogConsumerService : BackgroundService{
    private readonly ILogger<LogConsumerService> _logger;
    private readonly string _rabbitHost;

    public LogConsumerService(ILogger<LogConsumerService> logger, IConfiguration config){

        _logger = logger;
        _rabbitHost = config["RABBITMQ_HOST"] ?? "localhost";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingTocken){
        var factory = new ConnectionFactory {HostName = _rabbitHost};
        using var connection = await factory.CreateConnectionAsync(stoppingTocken);
        using var channel = await connection.CreateChannelAsync(CancellationToken: stoppingToken);

        await channel.QueueDeclareAsync("security-logs", durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsynkEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var log = JsonSerializer.Deserializer.Seserialize<SecurityLog>(body);

            _logger.LogInformation("processing log from {Source} with severity {Severity} ",
                log?.Source, log?.Severity);

            await channel .BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync("security-logs", autoAck: false, consumer: consumer);
        await Task.Delay(Timeout.Infinite, stoppingTocken)

    }