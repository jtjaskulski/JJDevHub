using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    public sealed class StudentInvite : AggregateRoot
    {
        public StudentInvite()
        {
        }

        public string CodeHash { get; private set; } = "";

        public string Label { get; private set; } = "";

        public DateTimeOffset ExpiresOn { get; private set; }

        public int MaxUses { get; private set; }

        public int UsedCount { get; private set; }

        public DateTimeOffset? RevokedOn { get; private set; }

        public static Result<StudentInvite> Create(
            string codeHash,
            string label,
            DateTimeOffset expiresOn,
            int maxUses,
            TimeProvider time)
        {
            if (string.IsNullOrWhiteSpace(codeHash) || string.IsNullOrWhiteSpace(label) || maxUses < 1)
            {
                return Result<StudentInvite>.Fail(ResultError.Invalid);
            }

            var invite = new StudentInvite();
            invite.AddDomainEvent(new InviteCreatedDomainEvent(
                Guid.NewGuid(),
                time.GetUtcNow(),
                Guid.NewGuid(),
                codeHash.Trim(),
                label.Trim(),
                expiresOn,
                maxUses));
            return Result<StudentInvite>.Ok(invite);
        }

        public Result<StudentInvite> Redeem(Guid accountId, TimeProvider time)
        {
            if (RevokedOn is not null || time.GetUtcNow() >= ExpiresOn || UsedCount >= MaxUses)
            {
                return Result<StudentInvite>.Fail(ResultError.Invalid);
            }

            AddDomainEvent(new InviteRedeemedDomainEvent(
                Guid.NewGuid(),
                time.GetUtcNow(),
                Id,
                accountId));
            return Result<StudentInvite>.Ok(this);
        }

        public Result<StudentInvite> Revoke(TimeProvider time)
        {
            if (RevokedOn is not null)
            {
                return Result<StudentInvite>.Ok(this);
            }

            var occurred = time.GetUtcNow();
            AddDomainEvent(new InviteRevokedDomainEvent(Guid.NewGuid(), occurred, Id, occurred));
            return Result<StudentInvite>.Ok(this);
        }

        protected override void Apply(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case InviteCreatedDomainEvent created:
                    ApplyCreated(created);
                    break;
                case InviteRedeemedDomainEvent:
                    UsedCount++;
                    break;
                case InviteRevokedDomainEvent revoked:
                    RevokedOn = revoked.RevokedOn;
                    break;
                default:
                    throw new InvalidOperationException($"StudentInvite cannot apply {domainEvent.GetType().Name}.");
            }
        }

        private void ApplyCreated(InviteCreatedDomainEvent created)
        {
            Id = created.InviteId;
            CodeHash = created.CodeHash;
            Label = created.Label;
            ExpiresOn = created.ExpiresOn;
            MaxUses = created.MaxUses;
        }
    }
}
