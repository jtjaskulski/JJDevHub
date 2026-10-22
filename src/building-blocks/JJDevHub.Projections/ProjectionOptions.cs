namespace JJDevHub.Projections
{
    public sealed class ProjectionOptions
    {
        public int BatchSize { get; set; } = 100;

        public TimeSpan IdleDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        public bool Enabled { get; set; } = true;
    }
}