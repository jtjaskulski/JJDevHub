namespace JJDevHub.EventStore.Postgres
{

    public sealed class EventStream
    {
        public string StreamId { get; set; } = string.Empty;

        public int Version { get; set; }

        public string? IdentityGuid { get; set; }
    }
}
