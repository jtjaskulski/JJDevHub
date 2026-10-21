namespace JJDevHub.Projections
{
    public sealed class Checkpoint
    {
        public string ProjectionName { get; set; } = string.Empty;

        public long Position { get; set; }
    }
}