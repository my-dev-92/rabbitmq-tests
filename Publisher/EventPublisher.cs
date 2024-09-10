using Common;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using ConnectionFactory = Common.ConnectionFactory;

namespace Publisher;

public class EventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<EventPublisher> _logger;
    private IModel? _model;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastNotification = new();

    public EventPublisher(ConnectionFactory connectionFactory, ILogger<EventPublisher> logger)
    {
        _logger = logger;
        _connection = connectionFactory.CreateConnection($"TestApp:{nameof(EventPublisher)}");

        GetModel().ExchangeDeclare(Exchange.PipelineExchange.Name, Exchange.PipelineExchange.Type);
    }

    public void PublishEvent(string exchangeName, string message, string routingKey, bool mandatory = true)
    {
        if (routingKey == string.Empty) _logger.LogWarning("The routing key is empty. Maybe an error in the configuration of the component.");

        var properties = GetModel().CreateBasicProperties();

        properties.Persistent = true;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        GetModel().BasicPublish(exchangeName, routingKey, mandatory, properties, Encoding.UTF8.GetBytes(message));
        _logger.LogInformation("Message is sent. {Exchange}. {Message}", exchangeName, message);
    }

    private IModel GetModel()
    {
        switch (_model)
        {
            case { IsOpen: true }: return _model;
            case { IsClosed: true }:
            {
                _logger.LogWarning("RabbitMQ channel and model found in a closed state and will be re-opened");
                _model.Dispose();
                break;
            }
        }

        _model = CreateModel();
        return _model;
    }

    private IModel CreateModel()
    {
        _model = _connection.CreateModel();
        _model.BasicReturn += OnBasicReturn;
        _model.ModelShutdown += OnModelShutdown;
        return _model;
    }

    private void OnModelShutdown(object? sender, ShutdownEventArgs e)
    {
        var logLevel = e.ReplyCode == Constants.ReplySuccess ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(logLevel, "Model shutdown event raised. ShutdownEventArgs={ShutdownEventArgs}", e);
    }

    /// <summary>
    /// When a published message cannot be routed to any queue, and the publisher set the mandatory message property to true,
    /// the message will be returned to it
    /// </summary>
    private void OnBasicReturn(object? sender, BasicReturnEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var loggedLastTime = _lastNotification.GetOrAdd(routingKey, _ => DateTimeOffset.Now);

        // we do not want to pollute logs with the same error, so normally log as Trace, but log as Error every hour
        var itIsTimeToIncreaseSeverity = DateTimeOffset.Now - loggedLastTime >= TimeSpan.FromHours(1);
        var logLevel = itIsTimeToIncreaseSeverity ? LogLevel.Error : LogLevel.Trace;
        _logger.Log(logLevel, "Published message cannot be routed to any queue. Check that next service in the pipeline is listening. " +
                              "Message={Message}, Exchange={Exchange}, RoutingKey={RoutingKey}",
            Encoding.UTF8.GetString(ea.Body.Span), ea.Exchange, routingKey);

        if (itIsTimeToIncreaseSeverity)
            _lastNotification[routingKey] = DateTimeOffset.Now;
    }
}