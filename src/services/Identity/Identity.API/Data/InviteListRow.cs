namespace JJDevHub.Api.Data
{
    public sealed class InviteListRow
    {
        public Guid InviteId { get; set; }

        public string Label { get; set; } = "";

        public DateTimeOffset ExpiresOn { get; set; }

        public int MaxUses { get; set; }

        public int UsedCount { get; set; }

        public DateTimeOffset? RevokedOn { get; set; }
    }
}
