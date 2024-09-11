using Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;

namespace Receiver.Workers;

public class Worker4 : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _cancellationSource;
    private readonly EventReceiver _eventReceiver;
    private readonly ILogger<Worker4> _logger;
    private readonly AsyncPolicyWrap _timeoutAndReTryPolicy;

    public Worker4(EventReceiver eventReceiver, ILogger<Worker4> logger)
    {
        _eventReceiver = eventReceiver;
        _logger = logger;

        _cancellationSource = new CancellationTokenSource();
        _cancellationSource.Token.Register(() => { _logger.LogInformation("Cancellation token is cancelled"); });

        const int retryLimit = 10;
        var waitAndRetryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(retryLimit,
                reTryCount => TimeSpan.FromSeconds(5 * reTryCount),
                (ex, span, retryCount, context) =>
                {
                    var logLevel = retryCount == retryLimit ? LogLevel.Warning : LogLevel.Information;
                    if (ex is OperationCanceledException)
                        _logger.Log(logLevel, "Service execution has interrupted");
                    else
                        _logger.Log(logLevel, ex, "Failed to process event. Event: {@Event}, Re-try in {Span}", context["event"], span);
                });
        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(45), (context, span, _) =>
        {
            _logger.LogWarning("{Operation} was interrupted due to long execution time {Timeout:g}. A timeout has occurred.",
                context.OperationKey, span);
            return Task.CompletedTask;
        });
        // Timeout for all retries as a whole
        _timeoutAndReTryPolicy = Policy.WrapAsync(timeoutPolicy, waitAndRetryPolicy);
    }

    #region Implementation of IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        _cancellationSource.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    private async Task MessageReceivedAsync(BaseEvent @event)
    {
        _logger.LogInformation("New event has been received. {@Event}", @event);

        var capture = await _timeoutAndReTryPolicy
            .ExecuteAndCaptureAsync(async (_, cancellationToken) =>
                {
                    _logger.LogInformation("Start processing the message");
                    await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
                    _eventReceiver.Acknowledge(@event.DeliveryTag);

                    return Task.CompletedTask;
                },
                new Context("Processing of the message") { { "event", @event } }, _cancellationSource.Token);

        if (capture.Outcome == OutcomeType.Failure)
        {
            if (capture.FinalException is not OperationCanceledException)
                _logger.LogError(capture.FinalException, "Event processing error. {@Event}", @event);
            _eventReceiver.Reject(@event.DeliveryTag);
        }

        _logger.LogInformation("Finished processing the message");
    }

    #region Implementation of IHostedService

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) =>
        new TaskFactory().StartNew(() =>
        {
            const string queueName = $"Pipeline.{RoutingKeys.RoutingKeyWorker4}";
            _eventReceiver.SubscribeQueue(queueName, RoutingKeys.RoutingKeyWorker4, MessageReceivedAsync);
        }, cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => new TaskFactory().StartNew(() =>
    {
        _logger.LogInformation("Stopping {Name}", GetType().Name);
        _eventReceiver.UnsubscribeMessagesFromQueue();
        _cancellationSource.Cancel();
    }, cancellationToken);

    #endregion
}