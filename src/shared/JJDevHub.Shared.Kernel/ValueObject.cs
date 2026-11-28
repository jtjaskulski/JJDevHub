namespace JJDevHub.Shared.Kernel
{
    public abstract class ValueObject
    {
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not ValueObject other || other.GetType() != GetType())
            {
                return false;
            }

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (object? component in GetEqualityComponents())
            {
                hash.Add(component);
            }

            return hash.ToHashCode();
        }

        protected abstract IEnumerable<object?> GetEqualityComponents();
    }
}