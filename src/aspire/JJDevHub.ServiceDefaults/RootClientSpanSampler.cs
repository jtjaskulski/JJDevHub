using System.Diagnostics;

using OpenTelemetry.Trace;

namespace JJDevHub.ServiceDefaults
{

    public sealed class RootClientSpanSampler : Sampler
    {
        public RootClientSpanSampler()
        {
            Description = "Drops root client spans.";
        }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            if (IsRootClient(samplingParameters))
            {
                return new SamplingResult(SamplingDecision.Drop);
            }

            return new SamplingResult(SamplingDecision.RecordAndSample);
        }

        private static bool IsRootClient(SamplingParameters samplingParameters)
        {
            var hasParent = samplingParameters.ParentContext.TraceId != default;
            return !hasParent && samplingParameters.Kind == ActivityKind.Client;
        }
    }
}
