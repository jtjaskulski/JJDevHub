using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace JJDevHub.Projections
{

    public static class ProjectionTelemetry
    {
        public const string Name = "JJDevHub.Projections";

        public static readonly ActivitySource ActivitySource = new(Name);

        public static readonly Meter Meter = new(Name);

        private static readonly ConcurrentDictionary<string, long> LagByProjection = new();

        static ProjectionTelemetry()
        {
            Meter.CreateObservableGauge("jjdevhub.projection.lag", ObserveLag);
        }

        public static void RememberLag(string projection, long lag)
        {
            LagByProjection[projection] = lag;
        }

        public static IDisposable Begin(
            IReadOnlyList<IProjection> projectionList,
            IReadOnlyDictionary<string, long> positions,
            long lastPosition)
        {
            var activities = new List<Activity>();
            foreach (var projection in projectionList)
            {
                if (positions[projection.Name] >= lastPosition)
                {
                    continue;
                }

                var activity = ActivitySource.StartActivity($"project {projection.Name}");
                if (activity is not null)
                {
                    activities.Add(activity);
                }
            }

            return new Pass(activities);
        }

        public static void RememberLags(
            IReadOnlyList<IProjection> projectionList,
            IReadOnlyDictionary<string, long> positions,
            long head,
            long? advancedTo)
        {
            foreach (var projection in projectionList)
            {
                var checkpoint = positions[projection.Name];
                if (advancedTo is long end && checkpoint < end)
                {
                    checkpoint = end;
                }

                var lag = head - checkpoint;
                if (lag < 0)
                {
                    lag = 0;
                }

                RememberLag(projection.Name, lag);
            }
        }

        private static IEnumerable<Measurement<long>> ObserveLag()
        {
            foreach (var entry in LagByProjection)
            {
                yield return new Measurement<long>(
                    entry.Value,
                    new KeyValuePair<string, object?>("projection", entry.Key));
            }
        }

        private sealed class Pass(List<Activity> activities) : IDisposable
        {
            public void Dispose()
            {
                for (var index = activities.Count - 1; index >= 0; index--)
                {
                    activities[index].Dispose();
                }
            }
        }
    }
}