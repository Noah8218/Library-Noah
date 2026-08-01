using System;
using System.Collections.Generic;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    public enum ExtractedModelSurfaceEdgeKind
    {
        Boundary,
        Crease
    }

    public sealed class ExtractedModelSurfaceEdge
    {
        public ExtractedModelSurfaceEdge(
            int order,
            int firstPointIndex,
            int secondPointIndex,
            ThreeDPoint firstPosition,
            ThreeDPoint secondPosition,
            ThreeDPoint anchor,
            double length,
            double strengthDegrees,
            ExtractedModelSurfaceEdgeKind kind)
        {
            Order = order;
            FirstPointIndex = firstPointIndex;
            SecondPointIndex = secondPointIndex;
            FirstPosition = firstPosition;
            SecondPosition = secondPosition;
            Anchor = anchor;
            Length = length;
            StrengthDegrees = strengthDegrees;
            Kind = kind;
        }

        public int Order { get; }
        public int FirstPointIndex { get; }
        public int SecondPointIndex { get; }
        public ThreeDPoint FirstPosition { get; }
        public ThreeDPoint SecondPosition { get; }
        public ThreeDPoint Anchor { get; }
        public double Length { get; }
        public double StrengthDegrees { get; }
        public ExtractedModelSurfaceEdgeKind Kind { get; }
    }

    public sealed class DeterministicModelSurfaceEdgeExtractionOptions
    {
        public double MinimumEdgeLength { get; set; }
        public double MinimumCreaseAngleDegrees { get; set; }
        public bool IncludeBoundaryEdges { get; set; }
    }

    public sealed class DeterministicModelSurfaceEdgeExtractionResult
    {
        private DeterministicModelSurfaceEdgeExtractionResult(
            bool success,
            string message,
            IReadOnlyList<ExtractedModelSurfaceEdge> edges)
        {
            Success = success;
            Message = message ?? string.Empty;
            Edges = edges ?? Array.Empty<ExtractedModelSurfaceEdge>();
        }

        public bool Success { get; }
        public string Message { get; }
        public IReadOnlyList<ExtractedModelSurfaceEdge> Edges { get; }

        internal static DeterministicModelSurfaceEdgeExtractionResult Completed(
            IReadOnlyList<ExtractedModelSurfaceEdge> edges)
        {
            return new DeterministicModelSurfaceEdgeExtractionResult(
                true,
                string.Empty,
                edges);
        }

        internal static DeterministicModelSurfaceEdgeExtractionResult Failed(
            string message)
        {
            return new DeterministicModelSurfaceEdgeExtractionResult(
                false,
                message,
                Array.Empty<ExtractedModelSurfaceEdge>());
        }
    }

    /// <summary>
    /// Extracts deterministic undirected mesh boundary and dihedral-crease
    /// edges. Non-manifold topology is rejected rather than repaired.
    /// </summary>
    public sealed class DeterministicModelSurfaceEdgeExtractionTool
    {
        public const string Semantics =
            "mesh-topology-boundary-and-dihedral-v1";

        public DeterministicModelSurfaceEdgeExtractionResult Execute(
            IReadOnlyList<ThreeDPoint> points,
            IReadOnlyList<SurfaceModelTriangleInput> triangles,
            DeterministicModelSurfaceEdgeExtractionOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                Validate(points, triangles, options);
                Dictionary<EdgeKey, List<int>> owners =
                    new Dictionary<EdgeKey, List<int>>();
                for (int triangleIndex = 0;
                     triangleIndex < triangles.Count;
                     triangleIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SurfaceModelTriangleInput triangle = triangles[triangleIndex];
                    AddOwner(owners, triangle.FirstPointIndex,
                        triangle.SecondPointIndex, triangleIndex);
                    AddOwner(owners, triangle.SecondPointIndex,
                        triangle.ThirdPointIndex, triangleIndex);
                    AddOwner(owners, triangle.ThirdPointIndex,
                        triangle.FirstPointIndex, triangleIndex);
                }

                List<EdgeKey> keys = new List<EdgeKey>(owners.Keys);
                keys.Sort(EdgeKey.Compare);
                List<ExtractedModelSurfaceEdge> edges =
                    new List<ExtractedModelSurfaceEdge>();
                for (int keyIndex = 0; keyIndex < keys.Count; keyIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    EdgeKey key = keys[keyIndex];
                    List<int> triangleOwners = owners[key];
                    if (triangleOwners.Count > 2)
                    {
                        throw new ArgumentException(
                            "Model edge extraction rejects non-manifold edges owned by more than two triangles.");
                    }

                    ThreeDPoint first = points[key.First];
                    ThreeDPoint second = points[key.Second];
                    double length = Distance(first, second);
                    if (length < options.MinimumEdgeLength)
                    {
                        continue;
                    }

                    ExtractedModelSurfaceEdgeKind kind;
                    double strengthDegrees;
                    if (triangleOwners.Count == 1)
                    {
                        if (!options.IncludeBoundaryEdges)
                        {
                            continue;
                        }

                        kind = ExtractedModelSurfaceEdgeKind.Boundary;
                        strengthDegrees = 180.0;
                    }
                    else
                    {
                        ThreeDPoint firstNormal = TriangleNormal(
                            points,
                            triangles[triangleOwners[0]]);
                        ThreeDPoint secondNormal = TriangleNormal(
                            points,
                            triangles[triangleOwners[1]]);
                        strengthDegrees = Math.Acos(Clamp(
                            Dot(firstNormal, secondNormal),
                            -1.0,
                            1.0)) * 180.0 / Math.PI;
                        if (strengthDegrees
                            < options.MinimumCreaseAngleDegrees)
                        {
                            continue;
                        }

                        kind = ExtractedModelSurfaceEdgeKind.Crease;
                    }

                    edges.Add(new ExtractedModelSurfaceEdge(
                        edges.Count,
                        key.First,
                        key.Second,
                        first,
                        second,
                        new ThreeDPoint(
                            (first.X + second.X) * 0.5,
                            (first.Y + second.Y) * 0.5,
                            (first.Z + second.Z) * 0.5),
                        length,
                        strengthDegrees,
                        kind));
                }

                return DeterministicModelSurfaceEdgeExtractionResult.Completed(
                    edges);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicModelSurfaceEdgeExtractionResult.Failed(
                    "Deterministic model surface-edge extraction failed: "
                    + exception.Message);
            }
        }

        private static void Validate(
            IReadOnlyList<ThreeDPoint> points,
            IReadOnlyList<SurfaceModelTriangleInput> triangles,
            DeterministicModelSurfaceEdgeExtractionOptions options)
        {
            if (points == null || points.Count == 0
                || triangles == null || triangles.Count == 0)
            {
                throw new ArgumentException(
                    "Model edge extraction requires points and triangles.");
            }

            if (options == null
                || !SurfaceMatchingContractValidation.IsFinite(
                    options.MinimumEdgeLength)
                || options.MinimumEdgeLength <= 0.0
                || !SurfaceMatchingContractValidation.IsFinite(
                    options.MinimumCreaseAngleDegrees)
                || options.MinimumCreaseAngleDegrees < 0.0
                || options.MinimumCreaseAngleDegrees > 180.0)
            {
                throw new ArgumentException(
                    "Model edge extraction options are invalid.");
            }

            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                if (points[pointIndex] == null
                    || !points[pointIndex].IsFinite)
                {
                    throw new ArgumentException(
                        "Model edge extraction requires finite XYZ points.");
                }
            }

            for (int triangleIndex = 0;
                 triangleIndex < triangles.Count;
                 triangleIndex++)
            {
                SurfaceModelTriangleInput triangle = triangles[triangleIndex];
                if (triangle == null
                    || triangle.FirstPointIndex < 0
                    || triangle.SecondPointIndex < 0
                    || triangle.ThirdPointIndex < 0
                    || triangle.FirstPointIndex >= points.Count
                    || triangle.SecondPointIndex >= points.Count
                    || triangle.ThirdPointIndex >= points.Count)
                {
                    throw new ArgumentException(
                        "Model edge triangles must reference existing zero-based points.");
                }
            }
        }

        private static void AddOwner(
            IDictionary<EdgeKey, List<int>> owners,
            int first,
            int second,
            int triangleIndex)
        {
            EdgeKey key = first < second
                ? new EdgeKey(first, second)
                : new EdgeKey(second, first);
            List<int> triangleOwners;
            if (!owners.TryGetValue(key, out triangleOwners))
            {
                triangleOwners = new List<int>();
                owners.Add(key, triangleOwners);
            }

            triangleOwners.Add(triangleIndex);
        }

        private static ThreeDPoint TriangleNormal(
            IReadOnlyList<ThreeDPoint> points,
            SurfaceModelTriangleInput triangle)
        {
            ThreeDPoint first = points[triangle.FirstPointIndex];
            ThreeDPoint second = points[triangle.SecondPointIndex];
            ThreeDPoint third = points[triangle.ThirdPointIndex];
            double abX = second.X - first.X;
            double abY = second.Y - first.Y;
            double abZ = second.Z - first.Z;
            double acX = third.X - first.X;
            double acY = third.Y - first.Y;
            double acZ = third.Z - first.Z;
            ThreeDPoint cross = new ThreeDPoint(
                abY * acZ - abZ * acY,
                abZ * acX - abX * acZ,
                abX * acY - abY * acX);
            double length = Math.Sqrt(Dot(cross, cross));
            if (!SurfaceMatchingContractValidation.IsFinite(length)
                || length <= 0.0)
            {
                throw new ArgumentException(
                    "Model edge extraction encountered a degenerate triangle.");
            }

            return new ThreeDPoint(
                cross.X / length,
                cross.Y / length,
                cross.Z / length);
        }

        private static double Distance(ThreeDPoint first, ThreeDPoint second)
        {
            double x = first.X - second.X;
            double y = first.Y - second.Y;
            double z = first.Z - second.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        private static double Dot(ThreeDPoint first, ThreeDPoint second)
        {
            return first.X * second.X
                + first.Y * second.Y
                + first.Z * second.Z;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return value < minimum
                ? minimum
                : value > maximum ? maximum : value;
        }

        private struct EdgeKey : IEquatable<EdgeKey>
        {
            public EdgeKey(int first, int second)
            {
                First = first;
                Second = second;
            }

            public int First { get; }
            public int Second { get; }

            public bool Equals(EdgeKey other)
            {
                return First == other.First && Second == other.Second;
            }

            public override bool Equals(object obj)
            {
                return obj is EdgeKey && Equals((EdgeKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return First * 397 ^ Second;
                }
            }

            public static int Compare(EdgeKey first, EdgeKey second)
            {
                int firstComparison = first.First.CompareTo(second.First);
                return firstComparison != 0
                    ? firstComparison
                    : first.Second.CompareTo(second.Second);
            }
        }
    }
}
