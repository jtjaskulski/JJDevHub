using System.Text.Json.Nodes;

namespace JJDevHub.EventStore
{

    public interface IEventUpcaster
    {
        string FromName { get; }

        string ToName { get; }

        JsonObject Upcast(JsonObject payload);
    }
}
