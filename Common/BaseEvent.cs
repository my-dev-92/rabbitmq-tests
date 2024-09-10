namespace Common;

public class BaseEvent
{
    #region Properties

    public ulong DeliveryTag { get; }
    public string Message { get; }
    public DateTimeOffset Created { get; }

    #endregion

    public BaseEvent(string message, ulong deliveryTag, DateTimeOffset created)
    {
        Message = message;
        DeliveryTag = deliveryTag;
        Created = created;
    }

    public BaseEvent(string message)
    {
        Message = message;
        DeliveryTag = 0;
        Created = default;
    }
}