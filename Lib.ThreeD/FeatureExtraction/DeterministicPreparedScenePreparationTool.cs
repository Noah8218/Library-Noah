using System;
using System.Collections.Generic;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    public sealed class PreparedScenePointSample
    {
        public PreparedScenePointSample(
            int order,
            int sourcePointIndex,
            ThreeDPoint position)
        {
            Order = order;
            SourcePointIndex = sourcePointIndex;
            Position = position;
        }

        public int Order { get; }
        public int SourcePointIndex { get; }
        public ThreeDPoint Position { get; }
    }

    public sealed class DeterministicPreparedScenePreparationOptions
    {
        public int MaximumSampleCount { get; set; }
    }

    public sealed class DeterministicPreparedScenePreparationResult
    {
        private DeterministicPreparedScenePreparationResult(
            bool success,
            string message,
            IReadOnlyList<PreparedScenePointSample> samples)
        {
            Success = success;
            Message = message ?? string.Empty;
            Samples = samples ?? Array.Empty<PreparedScenePointSample>();
        }

        public bool Success { get; }
        public string Message { get; }
        public IReadOnlyList<PreparedScenePointSample> Samples { get; }

        internal static DeterministicPreparedScenePreparationResult Completed(
            IReadOnlyList<PreparedScenePointSample> samples)
        {
            return new DeterministicPreparedScenePreparationResult(
                true,
                string.Empty,
                samples);
        }

        internal static DeterministicPreparedScenePreparationResult Failed(
            string message)
        {
            return new DeterministicPreparedScenePreparationResult(
                false,
                message,
                Array.Empty<PreparedScenePointSample>());
        }
    }

    /// <summary>
    /// Selects a deterministic even-index subset from an already-admitted
    /// finite scene. Source quality, identity, unit, frame, and persistence
    /// remain with the caller.
    /// </summary>
    public sealed class DeterministicPreparedScenePreparationTool
    {
        public const string Semantics =
            "deterministic-even-point-index-v1";

        public DeterministicPreparedScenePreparationResult Execute(
            IReadOnlyList<ThreeDPoint> finitePoints,
            DeterministicPreparedScenePreparationOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (finitePoints == null || finitePoints.Count == 0)
                {
                    throw new ArgumentException(
                        "Prepared-scene preparation requires finite source points.");
                }

                if (options == null || options.MaximumSampleCount <= 0)
                {
                    throw new ArgumentException(
                        "Prepared-scene preparation requires a positive maximum sample count.");
                }

                for (int pointIndex = 0;
                     pointIndex < finitePoints.Count;
                     pointIndex++)
                {
                    if (finitePoints[pointIndex] == null
                        || !finitePoints[pointIndex].IsFinite)
                    {
                        throw new ArgumentException(
                            "Prepared-scene preparation requires finite XYZ points.");
                    }
                }

                int sampleCount = Math.Min(
                    options.MaximumSampleCount,
                    finitePoints.Count);
                PreparedScenePointSample[] samples =
                    new PreparedScenePointSample[sampleCount];
                for (int sampleOrder = 0;
                     sampleOrder < sampleCount;
                     sampleOrder++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int sourcePointIndex = checked((int)(
                        ((long)sampleOrder * 2L + 1L)
                        * finitePoints.Count
                        / (sampleCount * 2L)));
                    samples[sampleOrder] = new PreparedScenePointSample(
                        sampleOrder,
                        sourcePointIndex,
                        finitePoints[sourcePointIndex]);
                }

                return DeterministicPreparedScenePreparationResult.Completed(
                    samples);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicPreparedScenePreparationResult.Failed(
                    "Deterministic prepared-scene preparation failed: "
                    + exception.Message);
            }
        }
    }
}
