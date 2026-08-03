using System;
using System.Collections.Generic;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    public sealed class ModelKeyPointInput
    {
        public ModelKeyPointInput(
            int sourceSampleOrder,
            ThreeDPoint position,
            ThreeDPoint normal)
        {
            SourceSampleOrder = sourceSampleOrder;
            Position = position;
            Normal = normal;
        }

        public int SourceSampleOrder { get; }
        public ThreeDPoint Position { get; }
        public ThreeDPoint Normal { get; }
    }

    public sealed class ExtractedModelKeyPoint
    {
        public ExtractedModelKeyPoint(
            int order,
            int sourceSampleOrder,
            ThreeDPoint position,
            ThreeDPoint normal,
            double nearestSelectedDistance)
        {
            Order = order;
            SourceSampleOrder = sourceSampleOrder;
            Position = position;
            Normal = normal;
            NearestSelectedDistance = nearestSelectedDistance;
        }

        public int Order { get; }
        public int SourceSampleOrder { get; }
        public ThreeDPoint Position { get; }
        public ThreeDPoint Normal { get; }
        public double NearestSelectedDistance { get; }
    }

    public sealed class DeterministicModelKeyPointExtractionOptions
    {
        public int MaximumKeyPointCount { get; set; }
        public double MinimumSeparation { get; set; }
    }

    public sealed class DeterministicModelKeyPointExtractionResult
    {
        private DeterministicModelKeyPointExtractionResult(
            bool success,
            string message,
            IReadOnlyList<ExtractedModelKeyPoint> keyPoints)
        {
            Success = success;
            Message = message ?? string.Empty;
            KeyPoints = keyPoints ?? Array.Empty<ExtractedModelKeyPoint>();
        }

        public bool Success { get; }
        public string Message { get; }
        public IReadOnlyList<ExtractedModelKeyPoint> KeyPoints { get; }

        internal static DeterministicModelKeyPointExtractionResult Completed(
            IReadOnlyList<ExtractedModelKeyPoint> keyPoints)
        {
            return new DeterministicModelKeyPointExtractionResult(
                true,
                string.Empty,
                keyPoints);
        }

        internal static DeterministicModelKeyPointExtractionResult Failed(
            string message)
        {
            return new DeterministicModelKeyPointExtractionResult(
                false,
                message,
                Array.Empty<ExtractedModelKeyPoint>());
        }
    }

    /// <summary>
    /// Selects a deterministic spatially distributed subset from prepared
    /// model samples. The lowest source order is the seed; each later point
    /// maximizes distance to its nearest selected point, with source order as
    /// the exact-tie breaker. The Tool does not execute or alter matching.
    /// </summary>
    public sealed class DeterministicModelKeyPointExtractionTool
    {
        public const string Semantics =
            "deterministic-farthest-model-sample-v1";

        public DeterministicModelKeyPointExtractionResult Execute(
            IReadOnlyList<ModelKeyPointInput> samples,
            DeterministicModelKeyPointExtractionOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                Validate(samples, options);
                List<ModelKeyPointInput> candidates =
                    new List<ModelKeyPointInput>(samples);
                candidates.Sort((first, second) =>
                    first.SourceSampleOrder.CompareTo(
                        second.SourceSampleOrder));

                int targetCount = Math.Min(
                    options.MaximumKeyPointCount,
                    candidates.Count);
                bool[] selected = new bool[candidates.Count];
                double[] nearestDistances = new double[candidates.Count];
                for (int index = 0; index < nearestDistances.Length; index++)
                {
                    nearestDistances[index] = double.PositiveInfinity;
                }

                List<ExtractedModelKeyPoint> keyPoints =
                    new List<ExtractedModelKeyPoint>(targetCount);
                Select(
                    candidates,
                    selected,
                    nearestDistances,
                    0,
                    0.0,
                    keyPoints);

                while (keyPoints.Count < targetCount)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int bestIndex = -1;
                    double bestDistance = -1.0;
                    for (int index = 0;
                         index < candidates.Count;
                         index++)
                    {
                        if (selected[index])
                        {
                            continue;
                        }

                        double distance = nearestDistances[index];
                        if (distance > bestDistance)
                        {
                            bestIndex = index;
                            bestDistance = distance;
                        }
                    }

                    if (bestIndex < 0
                        || bestDistance <= options.MinimumSeparation)
                    {
                        break;
                    }

                    Select(
                        candidates,
                        selected,
                        nearestDistances,
                        bestIndex,
                        bestDistance,
                        keyPoints);
                }

                return DeterministicModelKeyPointExtractionResult.Completed(
                    keyPoints);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicModelKeyPointExtractionResult.Failed(
                    "Deterministic model key-point extraction failed: "
                    + exception.Message);
            }
        }

        private static void Select(
            IReadOnlyList<ModelKeyPointInput> candidates,
            bool[] selected,
            double[] nearestDistances,
            int selectedIndex,
            double nearestSelectedDistance,
            ICollection<ExtractedModelKeyPoint> keyPoints)
        {
            ModelKeyPointInput selectedCandidate =
                candidates[selectedIndex];
            selected[selectedIndex] = true;
            keyPoints.Add(new ExtractedModelKeyPoint(
                keyPoints.Count,
                selectedCandidate.SourceSampleOrder,
                selectedCandidate.Position,
                selectedCandidate.Normal,
                nearestSelectedDistance));

            for (int index = 0; index < candidates.Count; index++)
            {
                if (selected[index])
                {
                    continue;
                }

                double distance = Distance(
                    selectedCandidate.Position,
                    candidates[index].Position);
                if (!SurfaceMatchingContractValidation.IsFinite(distance))
                {
                    throw new ArgumentException(
                        "Model key-point distance must be finite.");
                }

                if (distance < nearestDistances[index])
                {
                    nearestDistances[index] = distance;
                }
            }
        }

        private static void Validate(
            IReadOnlyList<ModelKeyPointInput> samples,
            DeterministicModelKeyPointExtractionOptions options)
        {
            if (samples == null || samples.Count == 0)
            {
                throw new ArgumentException(
                    "Model key-point extraction requires prepared model samples.");
            }

            if (options == null
                || options.MaximumKeyPointCount <= 0
                || !SurfaceMatchingContractValidation.IsFinite(
                    options.MinimumSeparation)
                || options.MinimumSeparation < 0.0)
            {
                throw new ArgumentException(
                    "Model key-point extraction options are invalid.");
            }

            HashSet<int> sourceOrders = new HashSet<int>();
            for (int index = 0; index < samples.Count; index++)
            {
                ModelKeyPointInput sample = samples[index];
                if (sample == null
                    || sample.SourceSampleOrder < 0
                    || !sourceOrders.Add(sample.SourceSampleOrder)
                    || sample.Position == null
                    || !sample.Position.IsFinite
                    || sample.Normal == null
                    || !sample.Normal.IsFinite
                    || !IsUnit(sample.Normal))
                {
                    throw new ArgumentException(
                        "Model key-point samples require unique non-negative orders, finite positions, and unit normals.");
                }
            }
        }

        private static bool IsUnit(ThreeDPoint value)
        {
            double length = Math.Sqrt(
                value.X * value.X
                + value.Y * value.Y
                + value.Z * value.Z);
            return SurfaceMatchingContractValidation.IsFinite(length)
                && Math.Abs(length - 1.0) <= 1e-6;
        }

        private static double Distance(ThreeDPoint first, ThreeDPoint second)
        {
            double x = Math.Abs(first.X - second.X);
            double y = Math.Abs(first.Y - second.Y);
            double z = Math.Abs(first.Z - second.Z);
            double scale = Math.Max(x, Math.Max(y, z));
            if (scale == 0.0)
            {
                return 0.0;
            }

            x /= scale;
            y /= scale;
            z /= scale;
            return scale * Math.Sqrt(x * x + y * y + z * z);
        }
    }
}
