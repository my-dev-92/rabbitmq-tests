using Common;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using ConnectionFactory = Common.ConnectionFactory;

namespace Receiver;

public class EventReceiver : IDisposable
{
    private readonly IConnection _connection;
    private readonly List<string> _consumerTags = [];
    private readonly ILogger<EventReceiver> _logger;

    private IModel? _channel;

    public EventReceiver(ILogger<EventReceiver> logger, ConnectionFactory connectionFactory)
    {
        _logger = logger;
        _connection = connectionFactory.CreateConnection($"TestApp:{nameof(EventReceiver)}");
        _connection.ConnectionShutdown += (o, e) => { _logger.LogWarning("Lost connection to broker...{Args}", e); };

        GetChannel().ExchangeDeclare(Exchange.PipelineExchange.Name, Exchange.PipelineExchange.Type);
    }

    public void Dispose()
    {
        UnsubscribeMessagesFromQueue();
        _channel?.Close();
        _channel?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void SubscribeQueue(string queueName, string routingKey, Func<BaseEvent, Task> action)
    {
        _logger.LogInformation("Bind queue {QueueName} to exchange", queueName);

        var channel = GetChannel();
        channel.QueueDeclare(queueName, true, false, false, null);

        channel.QueueBind(queueName, Exchange.PipelineExchange.Name, routingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            _logger.LogInformation("New message was received. DeliveryTag:{DeliveryTag}. Redelivered:{Redelivered}", ea.DeliveryTag, ea.Redelivered);

            if (channel.IsClosed)
            {
                _logger.LogWarning("Model is closed");
                return;
            }

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received message {Message}", message);
            await action(new BaseEvent(message, ea.DeliveryTag, DateTimeOffset.FromUnixTimeMilliseconds(ea.BasicProperties.Timestamp.UnixTime)));
        };
        consumer.Shutdown += (obj, ea) =>
        {
            _logger.LogWarning("The channel is shutdown (closed). {Object}, {Details}", obj, ea);

            return Task.CompletedTask;
        };
        consumer.Registered += (_, e) =>
        {
            _logger.LogInformation("Fires when the server confirms successful consumer cancelation. {Details}", e.ConsumerTags);
            return Task.CompletedTask;
        };
        var consumerTag = channel.BasicConsume(queueName, false, consumer);
        _logger.LogInformation("Subscribed consumer to message queue. Tag={ConsumerTag}, ChannelNumber={ChannelNumber}",
            consumerTag, channel.ChannelNumber);
        _consumerTags.Add(consumerTag);
    }

    private IModel GetChannel()
    {
        _channel ??= CreateChannel();
        return _channel;
    }

    private IModel CreateChannel()
    {
        _channel = _connection.CreateModel();
        _channel.BasicQos(0, 1, false);
        _channel.ModelShutdown += OnChannelShutdown;
        _channel.BasicAcks += (o, e) =>
        {
            _logger.LogInformation("Signalled when a Basic. Ack command arrives from the broker. Delivery tag {DeliveryTag}", e.DeliveryTag);
        };
        _channel.BasicNacks += (o, e) =>
        {
            _logger.LogInformation("Signalled when a Basic. Nack command arrives from the broker. Delivery tag {DeliveryTag}", e.DeliveryTag);
        };

        _logger.LogInformation("The channel was created");

        _channel.CallbackException += (o, e) =>
        {
            _logger.LogWarning("Signalled when an exception occurs in a callback invoked by the model. {Object}. {Details}", o, e.Exception);
        };

        return _channel;
    }

    private void OnChannelShutdown(object? sender, ShutdownEventArgs e)
    {
        var logLevel = e.ReplyCode == Constants.ReplySuccess ? LogLevel.Information : LogLevel.Warning;
        _logger.LogDebug("Connection open:{ConnectionIsOpen}. Channel open:{ChannelIsOpen}", _connection.IsOpen, _channel?.IsOpen);
        _logger.Log(logLevel, "Model shutdown event raised. ShutdownEventArgs={ShutdownEventArgs}", e);
    }

    public void UnsubscribeMessagesFromQueue()
    {
        var model = GetChannel();
        foreach (var consumerTag in _consumerTags)
        {
            _logger.LogInformation("Unsubscribe consumers from message queue. {ConsumerTag}", consumerTag);
            model.BasicCancel(consumerTag);
        }

        _consumerTags.Clear();
    }

    public void Acknowledge(ulong deliveryTag)
    {
        GetChannel().BasicAck(deliveryTag, false);
        _logger.LogInformation("Event has been acknowledged. DeliveryTag: {DeliveryTag}", deliveryTag);
    }

    public void Reject(ulong deliveryTag)
    {
        GetChannel().BasicNack(deliveryTag, false, true);
        _logger.LogInformation("Event has been rejected. DeliveryTag: {DeliveryTag}", deliveryTag);
    }
}