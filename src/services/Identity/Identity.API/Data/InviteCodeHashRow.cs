namespace JJDevHub.Api.Data
{
    public sealed class InviteCodeHashRow
    {
        public string CodeHash { get; set; } = "";

        public Guid InviteId { get; set; }
    }
}
