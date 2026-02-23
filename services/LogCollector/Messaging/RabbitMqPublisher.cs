using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string QueueName = "security-logs";

    public RabbitMqPublisher(string host, string user, string pass)
    {
        var factory = new ConnectionFactory { HostName = host, UserName = user, Password = pass };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
    }

    public async Task PublishAsync<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        await _channel.BasicPublishAsync(exchange: "", routingKey: QueueName, body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
