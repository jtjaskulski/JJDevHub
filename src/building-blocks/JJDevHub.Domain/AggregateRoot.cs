namespace JJDevHub.Domain
{
    public abstract class AggregateRoot : Entity, IAggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = [];

        public int Version { get; private set; }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void Replay(IDomainEvent domainEvent, int version)
        {
            Apply(domainEvent);
            Version = version;
        }

        internal void MarkPersisted(int version)
        {
            _domainEvents.Clear();
            Version = version;
        }

        protected static ResultError Check(params IBusinessRule[] rules)
        {
            foreach (var rule in rules)
            {
                if (rule.IsBroken())
                {
                    return rule.Error;
                }
            }

            return ResultError.None;
        }

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            Apply(domainEvent);
            _domainEvents.Add(domainEvent);
        }

        protected abstract void Apply(IDomainEvent domainEvent);
    }
}