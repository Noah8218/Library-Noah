using System;
using System.Collections.Generic;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    public sealed class SurfaceEdgeAnchorSample
    {
        public SurfaceEdgeAnchorSample(int order, ThreeDPoint anchor)
        {
            Order = order;
            Anchor = anchor;
        }

        public int Order { get; }
        public ThreeDPoint Anchor { get; }
    }

    public sealed class SurfaceEdgeCoverageMatch
    {
        public SurfaceEdgeCoverageMatch(
            int modelEdgeOrder,
            int sceneEdgeOrder,
            double distance)
        {
            ModelEdgeOrder = modelEdgeOrder;
            SceneEdgeOrder = sceneEdgeOrder;
            Distance = distance;
        }

        public int ModelEdgeOrder { get; }
        public int SceneEdgeOrder { get; }
        public double Distance { get; }
    }

    public sealed class DeterministicSurfaceEdgeCoverageResult
    {
        public const string Semantics =
            "one-way-model-edge-greedy-unique-nearest-anchor-v1";

        private DeterministicSurfaceEdgeCoverageResult(
            bool success,
            string message,
            int modelEdgeCount,
            int sceneEdgeCount,
            int matchedModelEdgeCount,
            double coverageRatio,
            bool hasInlierRmse,
            double inlierRmse,
            double maximumCorrespondenceDistance,
            IReadOnlyList<SurfaceEdgeCoverageMatch> matches)
        {
            Success = success;
            Message = message ?? string.Empty;
            ModelEdgeCount = modelEdgeCount;
            SceneEdgeCount = sceneEdgeCount;
            MatchedModelEdgeCount = matchedModelEdgeCount;
            CoverageRatio = coverageRatio;
            HasInlierRmse = hasInlierRmse;
            InlierRmse = inlierRmse;
            MaximumCorrespondenceDistance = maximumCorrespondenceDistance;
            Matches = matches ?? Array.Empty<SurfaceEdgeCoverageMatch>();
        }

        public bool Success { get; }
        public string Message { get; }
        public int ModelEdgeCount { get; }
        public int SceneEdgeCount { get; }
        public int MatchedModelEdgeCount { get; }
        public int UnmatchedModelEdgeCount =>
            ModelEdgeCount - MatchedModelEdgeCount;
        public double CoverageRatio { get; }
        public bool HasInlierRmse { get; }
        public double InlierRmse { get; }
        public double MaximumCorrespondenceDistance { get; }
        public IReadOnlyList<SurfaceEdgeCoverageMatch> Matches { get; }

        internal static DeterministicSurfaceEdgeCoverageResult Completed(
            DeterministicSurfaceCoverageResult coverage,
            IReadOnlyList<SurfaceEdgeCoverageMatch> matches)
        {
            return new DeterministicSurfaceEdgeCoverageResult(
                true,
                string.Empty,
                coverage.ModelSampleCount,
                coverage.SceneSampleCount,
                coverage.MatchedModelSampleCount,
                coverage.CoverageRatio,
                coverage.HasInlierRmse,
                coverage.InlierRmse,
                coverage.MaximumCorrespondenceDistance,
                matches);
        }

        internal static DeterministicSurfaceEdgeCoverageResult CompletedEmptyScene(
            int modelEdgeCount,
            double maximumCorrespondenceDistance)
        {
            return new DeterministicSurfaceEdgeCoverageResult(
                true,
                string.Empty,
                modelEdgeCount,
                0,
                0,
                0.0,
                false,
                double.NaN,
                maximumCorrespondenceDistance,
                Array.Empty<SurfaceEdgeCoverageMatch>());
        }

        internal static DeterministicSurfaceEdgeCoverageResult Failed(
            string message)
        {
            return new DeterministicSurfaceEdgeCoverageResult(
                false,
                message,
                0,
                0,
                0,
                0.0,
                false,
                double.NaN,
                double.NaN,
                Array.Empty<SurfaceEdgeCoverageMatch>());
        }
    }

    /// <summary>
    /// Edge-domain adapter over the shared deterministic one-way unique
    /// nearest coverage kernel. It preserves edge orders and exposes no
    /// acceptance policy.
    /// </summary>
    public sealed class DeterministicSurfaceEdgeCoverageTool
    {
        private readonly DeterministicSurfaceCoverageTool coverageTool =
            new DeterministicSurfaceCoverageTool();

        public DeterministicSurfaceEdgeCoverageResult Execute(
            IReadOnlyList<SurfaceEdgeAnchorSample> modelEdges,
            IReadOnlyList<SurfaceEdgeAnchorSample> sceneEdges,
            RigidSurfacePose pose,
            double maximumCorrespondenceDistance,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                SurfaceMatchSample[] modelSamples = ToSamples(
                    modelEdges,
                    "Surface edge coverage model input",
                    false);
                SurfaceMatchSample[] sceneSamples = ToSamples(
                    sceneEdges,
                    "Surface edge coverage scene input",
                    true);
                if (sceneSamples.Length == 0)
                {
                    if (pose == null || !pose.IsRigid(1e-9))
                    {
                        throw new ArgumentException(
                            "Surface edge coverage requires a finite rigid pose.");
                    }

                    if (!SurfaceMatchingContractValidation.IsFinite(
                            maximumCorrespondenceDistance)
                        || maximumCorrespondenceDistance <= 0.0)
                    {
                        throw new ArgumentException(
                            "Surface edge coverage distance must be finite and positive.");
                    }

                    return DeterministicSurfaceEdgeCoverageResult
                        .CompletedEmptyScene(
                            modelSamples.Length,
                            maximumCorrespondenceDistance);
                }

                DeterministicSurfaceCoverageResult coverage =
                    coverageTool.Execute(
                        modelSamples,
                        sceneSamples,
                        pose,
                        maximumCorrespondenceDistance,
                        cancellationToken);
                if (!coverage.Success)
                {
                    throw new ArgumentException(coverage.Message);
                }

                SurfaceEdgeCoverageMatch[] matches =
                    new SurfaceEdgeCoverageMatch[coverage.Matches.Count];
                for (int index = 0; index < matches.Length; index++)
                {
                    SurfaceCoverageMatch match = coverage.Matches[index];
                    matches[index] = new SurfaceEdgeCoverageMatch(
                        match.ModelSampleOrder,
                        match.SceneSampleOrder,
                        match.Distance);
                }

                return DeterministicSurfaceEdgeCoverageResult.Completed(
                    coverage,
                    matches);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicSurfaceEdgeCoverageResult.Failed(
                    "Deterministic surface-edge coverage failed: "
                    + exception.Message);
            }
        }

        private static SurfaceMatchSample[] ToSamples(
            IReadOnlyList<SurfaceEdgeAnchorSample> edges,
            string label,
            bool allowEmpty)
        {
            if (edges == null || !allowEmpty && edges.Count == 0)
            {
                throw new ArgumentException(
                    label + " requires at least one ordered edge anchor.");
            }

            SurfaceMatchSample[] samples =
                new SurfaceMatchSample[edges.Count];
            for (int index = 0; index < edges.Count; index++)
            {
                SurfaceEdgeAnchorSample edge = edges[index];
                if (edge == null
                    || edge.Order != index
                    || edge.Anchor == null
                    || !edge.Anchor.IsFinite)
                {
                    throw new ArgumentException(
                        label + " requires contiguous zero-based orders and finite XYZ anchors.");
                }

                samples[index] = new SurfaceMatchSample(
                    edge.Order,
                    edge.Anchor);
            }

            return samples;
        }
    }
}
