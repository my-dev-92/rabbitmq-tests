using Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Publisher;

public class PublishWorker(EventPublisher eventPublisher, ILogger<PublishWorker> logger) : IHostedService
{
    #region Implementation of IHostedService

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => new TaskFactory().StartNew(() =>
    {
        // publish events for Worker1
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "Worker1", RoutingKeys.RoutingKeyWorker1);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "Worker1", RoutingKeys.RoutingKeyWorker1);

        // publish events for Worker2
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "Worker2", RoutingKeys.RoutingKeyWorker2);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "Worker2", RoutingKeys.RoutingKeyWorker2);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "Worker2", RoutingKeys.RoutingKeyWorker2);
    }, cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) =>
        new TaskFactory().StartNew(() => { logger.LogInformation("Stopping {Name}", GetType().Name); }, cancellationToken);

    #endregion

}