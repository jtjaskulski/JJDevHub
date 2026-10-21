namespace JJDevHub.Domain
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EventNameAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }
}