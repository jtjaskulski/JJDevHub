using JJDevHub.EventStore;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JJDevHub.Projections
{

    public sealed class ProjectionRunner<TContext>(
        IEventStore eventStore,
        TContext context,
        IEnumerable<IProjection> projections,
        IOptions<ProjectionOptions> options)
        where TContext : ReadContext
    {
        public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
        {
            var projectionList = projections.ToList();
            if (projectionList.Count == 0)
            {
                return 0;
            }

            var checkpoints = await context.Checkpoints.ToListAsync(cancellationToken);
            var positions = Positions(projectionList, checkpoints);
            var batch = await eventStore.ReadAllAsync(
                positions.Values.Min(), options.Value.BatchSize, cancellationToken);
            var head = await eventStore.ReadHeadPositionAsync(cancellationToken);
            if (batch.Count == 0)
            {
                ProjectionTelemetry.RememberLags(projectionList, positions, head, advancedTo: null);
                return 0;
            }

            using (ProjectionTelemetry.Begin(projectionList, positions, batch[^1].Position))
            {
                await ProjectAsync(projectionList, positions, batch, cancellationToken);
                Advance(projectionList, checkpoints, positions, batch[^1].Position);
                await context.SaveChangesAsync(cancellationToken);
                ProjectionTelemetry.RememberLags(projectionList, positions, head, batch[^1].Position);
                return batch.Count;
            }
        }

        private static Dictionary<string, long> Positions(
            IReadOnlyList<IProjection> projectionList, IReadOnlyList<Checkpoint> checkpoints)
        {
            var positions = new Dictionary<string, long>();
            foreach (var projection in projectionList)
            {
                var checkpoint = checkpoints.FirstOrDefault(item => item.ProjectionName == projection.Name);
                positions[projection.Name] = checkpoint?.Position ?? 0;
            }

            return positions;
        }

        private static async Task ProjectAsync(
            IReadOnlyList<IProjection> projectionList,
            Dictionary<string, long> positions,
            IReadOnlyList<StoredEvent> batch,
            CancellationToken cancellationToken)
        {
            foreach (var storedEvent in batch)
            {
                await ProjectEventAsync(projectionList, positions, storedEvent, cancellationToken);
            }
        }

        private static async Task ProjectEventAsync(
            IReadOnlyList<IProjection> projectionList,
            Dictionary<string, long> positions,
            StoredEvent storedEvent,
            CancellationToken cancellationToken)
        {
            foreach (var projection in projectionList)
            {
                if (positions[projection.Name] >= storedEvent.Position)
                {
                    continue;
                }

                if (!projection.CanHandle(storedEvent.DomainEvent))
                {
                    continue;
                }

                await projection.ProjectAsync(storedEvent, cancellationToken);
            }
        }

        private void Advance(
            IReadOnlyList<IProjection> projectionList,
            IReadOnlyList<Checkpoint> checkpoints,
            Dictionary<string, long> positions,
            long lastPosition)
        {
            foreach (var projection in projectionList)
            {
                if (positions[projection.Name] >= lastPosition)
                {
                    continue;
                }

                var checkpoint = checkpoints.FirstOrDefault(item => item.ProjectionName == projection.Name);
                if (checkpoint is null)
                {
                    context.Checkpoints.Add(new Checkpoint
                    {
                        ProjectionName = projection.Name,
                        Position = lastPosition,
                    });
                    continue;
                }

                checkpoint.Position = lastPosition;
            }
        }
    }
}
