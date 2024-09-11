using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Common;

public class ConnectionFactory(ILogger<ConnectionFactory> logger)
{
    private readonly RabbitMQ.Client.ConnectionFactory _factory = new()
    {
        Uri = new Uri("amqp://guest:guest@127.0.0.1/"),

        // true is default value, keep it just in case
        AutomaticRecoveryEnabled = true,

        // this is required to be enable in order to consume messages asynchronously via AsyncEventingBasicConsumer
        DispatchConsumersAsync = true,

        ConsumerDispatchConcurrency = 2,

        // make the client check every 30 seconds if the connection is still alive
        RequestedHeartbeat = TimeSpan.FromSeconds(5)
    };

    public IConnection CreateConnection(string creatorName)
    {
        logger.LogDebug("Creating a connection of {AppName} to RabbitMQ server {Url}", creatorName, _factory.Uri);
        return _factory.CreateConnection(creatorName);
    }
}