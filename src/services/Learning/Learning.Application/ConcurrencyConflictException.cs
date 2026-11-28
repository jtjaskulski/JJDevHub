namespace Learning.Application
{
    public sealed class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException()
            : base("learning.version.conflict")
        {
        }
    }
}