using System.Diagnostics;

namespace JJDevHub.Cqrs
{

    public static class CqrsTelemetry
    {
        public const string Name = "JJDevHub.Cqrs";

        public static readonly ActivitySource ActivitySource = new(Name);
    }
}
