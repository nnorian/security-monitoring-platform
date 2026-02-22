using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class RabbitMqPublisher : IDisposable{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string QueueName = "security-logs";

    public RabbitMqPublisher(string host){
        var factory = new  ConnectionFactory { HostName = host };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResults();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(QueueName, durable: true, excluse: false, autoDelete: false);

    }

    public asynk Task PublishAsynk<T>(T message){
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        await _channel.BasicPublishAsynk(exchange: "", routingKey: QueueName, body: body);
    }

    public void Dispose() {_channel&.dispose(); _connection?.Dispose(); }
}

