using Learning.Application.Ports;

namespace Learning.Application.Tests.Memory
{
    public sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int Saves { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Saves++;
            return Task.CompletedTask;
        }
    }
}