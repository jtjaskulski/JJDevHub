namespace JJDevHub.Api.Data
{
    public sealed class AccountListRow
    {
        public Guid AccountId { get; set; }

        public string Email { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public string[] Roles { get; set; } = [];

        public DateTimeOffset CreatedOn { get; set; }
    }
}
