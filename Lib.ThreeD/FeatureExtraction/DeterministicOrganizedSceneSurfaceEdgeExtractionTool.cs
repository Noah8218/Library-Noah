using System;
using System.Collections.Generic;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    public enum ExtractedSceneSurfaceEdgeAxis
    {
        AcrossColumns,
        AcrossRows
    }

    public sealed class ExtractedSceneSurfaceEdge
    {
        public ExtractedSceneSurfaceEdge(
            int order,
            int firstPointIndex,
            int secondPointIndex,
            int anchorPointIndex,
            ThreeDPoint firstPosition,
            ThreeDPoint secondPosition,
            ThreeDPoint anchor,
            double absoluteHeightStep,
            ExtractedSceneSurfaceEdgeAxis axis)
        {
            Order = order;
            FirstPointIndex = firstPointIndex;
            SecondPointIndex = secondPointIndex;
            AnchorPointIndex = anchorPointIndex;
            FirstPosition = firstPosition;
            SecondPosition = secondPosition;
            Anchor = anchor;
            AbsoluteHeightStep = absoluteHeightStep;
            Axis = axis;
        }

        public int Order { get; }
        public int FirstPointIndex { get; }
        public int SecondPointIndex { get; }
        public int AnchorPointIndex { get; }
        public ThreeDPoint FirstPosition { get; }
        public ThreeDPoint SecondPosition { get; }
        public ThreeDPoint Anchor { get; }
        public double AbsoluteHeightStep { get; }
        public ExtractedSceneSurfaceEdgeAxis Axis { get; }
    }

    public sealed class DeterministicOrganizedSceneSurfaceEdgeExtractionOptions
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public double MinimumAbsoluteHeightStep { get; set; }
        public bool IncludeColumnNeighbors { get; set; }
        public bool IncludeRowNeighbors { get; set; }
    }

    public sealed class DeterministicOrganizedSceneSurfaceEdgeExtractionResult
    {
        private DeterministicOrganizedSceneSurfaceEdgeExtractionResult(
            bool success,
            string message,
            IReadOnlyList<ExtractedSceneSurfaceEdge> edges)
        {
            Success = success;
            Message = message ?? string.Empty;
            Edges = edges ?? Array.Empty<ExtractedSceneSurfaceEdge>();
        }

        public bool Success { get; }
        public string Message { get; }
        public IReadOnlyList<ExtractedSceneSurfaceEdge> Edges { get; }

        internal static DeterministicOrganizedSceneSurfaceEdgeExtractionResult Completed(
            IReadOnlyList<ExtractedSceneSurfaceEdge> edges)
        {
            return new DeterministicOrganizedSceneSurfaceEdgeExtractionResult(
                true,
                string.Empty,
                edges);
        }

        internal static DeterministicOrganizedSceneSurfaceEdgeExtractionResult Failed(
            string message)
        {
            return new DeterministicOrganizedSceneSurfaceEdgeExtractionResult(
                false,
                message,
                Array.Empty<ExtractedSceneSurfaceEdge>());
        }
    }

    /// <summary>
    /// Extracts inclusive height-step neighbors from one complete row-major
    /// organized XYZ grid and anchors every result at its higher endpoint.
    /// </summary>
    public sealed class DeterministicOrganizedSceneSurfaceEdgeExtractionTool
    {
        public const string Semantics =
            "organized-height-step-higher-endpoint-v1";

        public DeterministicOrganizedSceneSurfaceEdgeExtractionResult Execute(
            IReadOnlyList<ThreeDPoint> points,
            DeterministicOrganizedSceneSurfaceEdgeExtractionOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                Validate(points, options);
                List<Candidate> candidates = new List<Candidate>();
                for (int row = 0; row < options.Height; row++)
                {
                    for (int column = 0; column < options.Width; column++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        int firstIndex = checked(row * options.Width + column);
                        if (options.IncludeColumnNeighbors
                            && column + 1 < options.Width)
                        {
                            AddCandidate(
                                points,
                                firstIndex,
                                firstIndex + 1,
                                ExtractedSceneSurfaceEdgeAxis.AcrossColumns,
                                options.MinimumAbsoluteHeightStep,
                                candidates);
                        }

                        if (options.IncludeRowNeighbors
                            && row + 1 < options.Height)
                        {
                            AddCandidate(
                                points,
                                firstIndex,
                                firstIndex + options.Width,
                                ExtractedSceneSurfaceEdgeAxis.AcrossRows,
                                options.MinimumAbsoluteHeightStep,
                                candidates);
                        }
                    }
                }

                candidates.Sort(Candidate.Compare);
                ExtractedSceneSurfaceEdge[] edges =
                    new ExtractedSceneSurfaceEdge[candidates.Count];
                for (int order = 0; order < candidates.Count; order++)
                {
                    Candidate candidate = candidates[order];
                    edges[order] = new ExtractedSceneSurfaceEdge(
                        order,
                        candidate.FirstPointIndex,
                        candidate.SecondPointIndex,
                        candidate.AnchorPointIndex,
                        candidate.FirstPosition,
                        candidate.SecondPosition,
                        candidate.Anchor,
                        candidate.AbsoluteHeightStep,
                        candidate.Axis);
                }

                return DeterministicOrganizedSceneSurfaceEdgeExtractionResult.Completed(
                    edges);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicOrganizedSceneSurfaceEdgeExtractionResult.Failed(
                    "Deterministic organized scene surface-edge extraction failed: "
                    + exception.Message);
            }
        }

        private static void Validate(
            IReadOnlyList<ThreeDPoint> points,
            DeterministicOrganizedSceneSurfaceEdgeExtractionOptions options)
        {
            if (options == null
                || options.Width <= 0
                || options.Height <= 0
                || !SurfaceMatchingContractValidation.IsFinite(
                    options.MinimumAbsoluteHeightStep)
                || options.MinimumAbsoluteHeightStep <= 0.0
                || !options.IncludeColumnNeighbors
                    && !options.IncludeRowNeighbors)
            {
                throw new ArgumentException(
                    "Organized scene edge extraction options are invalid.");
            }

            long expectedPointCount = checked(
                (long)options.Width * options.Height);
            if (points == null || points.Count != expectedPointCount)
            {
                throw new ArgumentException(
                    "Organized scene edge extraction requires one row-major point per grid cell.");
            }

            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                if (points[pointIndex] == null
                    || !points[pointIndex].IsFinite)
                {
                    throw new ArgumentException(
                        "Organized scene edge extraction requires finite XYZ points.");
                }
            }
        }

        private static void AddCandidate(
            IReadOnlyList<ThreeDPoint> points,
            int firstIndex,
            int secondIndex,
            ExtractedSceneSurfaceEdgeAxis axis,
            double threshold,
            ICollection<Candidate> candidates)
        {
            ThreeDPoint first = points[firstIndex];
            ThreeDPoint second = points[secondIndex];
            double step = Math.Abs(first.Z - second.Z);
            if (step < threshold)
            {
                return;
            }

            int anchorIndex = first.Z > second.Z
                ? firstIndex
                : secondIndex;
            candidates.Add(new Candidate(
                firstIndex,
                secondIndex,
                anchorIndex,
                first,
                second,
                anchorIndex == firstIndex ? first : second,
                step,
                axis));
        }

        private sealed class Candidate
        {
            public Candidate(
                int firstPointIndex,
                int secondPointIndex,
                int anchorPointIndex,
                ThreeDPoint firstPosition,
                ThreeDPoint secondPosition,
                ThreeDPoint anchor,
                double absoluteHeightStep,
                ExtractedSceneSurfaceEdgeAxis axis)
            {
                FirstPointIndex = firstPointIndex;
                SecondPointIndex = secondPointIndex;
                AnchorPointIndex = anchorPointIndex;
                FirstPosition = firstPosition;
                SecondPosition = secondPosition;
                Anchor = anchor;
                AbsoluteHeightStep = absoluteHeightStep;
                Axis = axis;
            }

            public int FirstPointIndex { get; }
            public int SecondPointIndex { get; }
            public int AnchorPointIndex { get; }
            public ThreeDPoint FirstPosition { get; }
            public ThreeDPoint SecondPosition { get; }
            public ThreeDPoint Anchor { get; }
            public double AbsoluteHeightStep { get; }
            public ExtractedSceneSurfaceEdgeAxis Axis { get; }

            public static int Compare(Candidate first, Candidate second)
            {
                int firstComparison = first.FirstPointIndex.CompareTo(
                    second.FirstPointIndex);
                return firstComparison != 0
                    ? firstComparison
                    : first.SecondPointIndex.CompareTo(
                        second.SecondPointIndex);
            }
        }
    }
}
