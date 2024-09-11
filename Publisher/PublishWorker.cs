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
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker1);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker1);

        // publish events for Worker2
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker2);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker2);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker2);

        // publish events for Worker3
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker3);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker3);

        // publish events for Worker4
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker4);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker4);

        // publish events for Worker5
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker5);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker5);

        // publish events for Worker6
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker6);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker6);

        // publish events for Worker7
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker7);
        eventPublisher.PublishEvent(Exchange.PipelineExchange.Name, "some message", RoutingKeys.RoutingKeyWorker7);
    }, cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) =>
        new TaskFactory().StartNew(() => { logger.LogInformation("Stopping {Name}", GetType().Name); }, cancellationToken);

    #endregion

}