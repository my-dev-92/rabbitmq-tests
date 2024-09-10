using RabbitMQ.Client;

namespace Common;

public static class Exchange
{
    public static readonly (string Name, string Type) PipelineExchange = (Name: "PipelineExchange", Type: ExchangeType.Direct);
}