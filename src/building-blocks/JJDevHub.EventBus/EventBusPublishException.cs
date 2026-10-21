namespace JJDevHub.EventBus
{

    public sealed class EventBusPublishException(string message, Exception? innerException = null)
        : Exception(message, innerException);
}
