using System;
using System.Collections.Generic;

namespace OpenVisionLab.Vision3D.FeatureExtraction
{
    /// <summary>
    /// Source-neutral triangle with a stable caller-owned source index.
    /// </summary>
    public sealed class MeshTriangle
    {
        public MeshTriangle(
            long sourceTriangleIndex,
            ThreeDPoint a,
            ThreeDPoint b,
            ThreeDPoint c)
        {
            SourceTriangleIndex = sourceTriangleIndex;
            A = a;
            B = b;
            C = c;
        }

        public long SourceTriangleIndex { get; }

        public ThreeDPoint A { get; }

        public ThreeDPoint B { get; }

        public ThreeDPoint C { get; }
    }

    public enum MeshClosestFeature
    {
        FaceInterior,
        Edge,
        Vertex
    }

    /// <summary>
    /// Deterministic closest-point evidence. Boundary signs remain unresolved
    /// until the caller explicitly requests robust sign recovery.
    /// </summary>
    public sealed class PointMeshDistance
    {
        public PointMeshDistance(
            long sourceTriangleIndex,
            ThreeDPoint closestPoint,
            ThreeDPoint triangleNormal,
            MeshClosestFeature closestFeature,
            double unsignedDistance,
            double? signedDistance,
            bool signResolved)
        {
            SourceTriangleIndex = sourceTriangleIndex;
            ClosestPoint = closestPoint;
            TriangleNormal = triangleNormal;
            ClosestFeature = closestFeature;
            UnsignedDistance = unsignedDistance;
            SignedDistance = signedDistance;
            SignResolved = signResolved;
        }

        public long SourceTriangleIndex { get; }

        public ThreeDPoint ClosestPoint { get; }

        public ThreeDPoint TriangleNormal { get; }

        public MeshClosestFeature ClosestFeature { get; }

        public double UnsignedDistance { get; }

        public double? SignedDistance { get; }

        public bool SignResolved { get; }
    }

    /// <summary>
    /// Builds a deterministic BVH once, then executes closest-point and robust
    /// sign queries without owning source identity, units, frames, or product
    /// acceptance policy.
    /// </summary>
    public sealed class TriangleMeshDistanceTool
    {
        private const int LeafTriangleCount = 8;

        public const double RobustSignDistanceEpsilon =
            1.1920928955078125e-7;

        private readonly TriangleEntry[] triangles;
        private readonly Node root;

        public TriangleMeshDistanceTool(IReadOnlyList<MeshTriangle> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source.Count == 0)
            {
                throw new ArgumentException(
                    "A distance index requires at least one triangle.",
                    nameof(source));
            }

            triangles = new TriangleEntry[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                triangles[index] = CreateEntry(source[index]);
            }

            root = BuildNode(0, triangles.Length);
        }

        public int TriangleCount => triangles.Length;

        public PointMeshDistance Execute(ThreeDPoint point)
        {
            Vector3 query = ToVector(point, nameof(point));
            SearchResult best = new SearchResult(
                double.PositiveInfinity,
                long.MaxValue,
                null,
                default(ClosestPointResult));
            Search(root, query, ref best);

            double unsignedDistance = Math.Sqrt(
                Math.Max(0.0, best.DistanceSquared));
            double? signedDistance = null;
            bool signResolved =
                best.Closest.Feature == MeshClosestFeature.FaceInterior;
            if (signResolved)
            {
                double side = Dot(
                    query - best.Closest.Point,
                    best.Triangle.Normal);
                if (unsignedDistance == 0.0)
                {
                    signedDistance = 0.0;
                }
                else if (side == 0.0)
                {
                    signResolved = false;
                }
                else
                {
                    signedDistance = side < 0.0
                        ? -unsignedDistance
                        : unsignedDistance;
                }
            }

            return CreateDistance(
                best.Triangle,
                best.Closest,
                unsignedDistance,
                signedDistance,
                signResolved);
        }

        public PointMeshDistance ExecuteRobustSign(
            ThreeDPoint point,
            double nearestUnsignedDistance)
        {
            Vector3 query = ToVector(point, nameof(point));
            if (!IsFinite(nearestUnsignedDistance)
                || nearestUnsignedDistance < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nearestUnsignedDistance),
                    "The nearest unsigned distance must be finite and non-negative.");
            }

            double maximumCandidateDistance =
                nearestUnsignedDistance + RobustSignDistanceEpsilon;
            RobustSearchState state =
                new RobustSearchState(maximumCandidateDistance);
            SearchRobustCandidates(root, query, state);
            RobustCandidate selected = state.BestInterior
                ?? state.BestBoundary;
            if (selected == null)
            {
                throw new InvalidOperationException(
                    "No robust sign candidate was found within the nearest-distance tolerance.");
            }

            double side = Dot(
                query - selected.Closest.Point,
                selected.Triangle.Normal);
            double signedDistance = selected.Distance == 0.0
                ? 0.0
                : side < 0.0
                    ? -selected.Distance
                    : selected.Distance;
            return CreateDistance(
                selected.Triangle,
                selected.Closest,
                selected.Distance,
                signedDistance,
                true);
        }

        private Node BuildNode(int start, int count)
        {
            Bounds bounds = CalculateBounds(start, count);
            if (count <= LeafTriangleCount)
            {
                return new Node(
                    bounds.Minimum,
                    bounds.Maximum,
                    start,
                    count,
                    null,
                    null);
            }

            Bounds centroidBounds = CalculateCentroidBounds(start, count);
            Vector3 span = centroidBounds.Maximum - centroidBounds.Minimum;
            int axis = span.X >= span.Y && span.X >= span.Z
                ? 0
                : span.Y >= span.Z
                    ? 1
                    : 2;
            Array.Sort(
                triangles,
                start,
                count,
                CentroidComparer.ForAxis(axis));

            int leftCount = count / 2;
            Node left = BuildNode(start, leftCount);
            Node right = BuildNode(
                start + leftCount,
                count - leftCount);
            return new Node(
                bounds.Minimum,
                bounds.Maximum,
                start,
                count,
                left,
                right);
        }

        private void Search(
            Node node,
            Vector3 point,
            ref SearchResult best)
        {
            if (DistanceSquaredToBounds(
                    point,
                    node.Minimum,
                    node.Maximum) > best.DistanceSquared)
            {
                return;
            }

            if (node.Left == null || node.Right == null)
            {
                int end = node.Start + node.Count;
                for (int index = node.Start; index < end; index++)
                {
                    TriangleEntry triangle = triangles[index];
                    ClosestPointResult closest = FindClosestPoint(
                        point,
                        triangle);
                    double distanceSquared = DistanceSquared(
                        point,
                        closest.Point);
                    if (distanceSquared < best.DistanceSquared
                        || distanceSquared == best.DistanceSquared
                        && triangle.Source.SourceTriangleIndex
                            < best.SourceTriangleIndex)
                    {
                        best = new SearchResult(
                            distanceSquared,
                            triangle.Source.SourceTriangleIndex,
                            triangle,
                            closest);
                    }
                }

                return;
            }

            double leftDistance = DistanceSquaredToBounds(
                point,
                node.Left.Minimum,
                node.Left.Maximum);
            double rightDistance = DistanceSquaredToBounds(
                point,
                node.Right.Minimum,
                node.Right.Maximum);
            if (leftDistance <= rightDistance)
            {
                Search(node.Left, point, ref best);
                Search(node.Right, point, ref best);
            }
            else
            {
                Search(node.Right, point, ref best);
                Search(node.Left, point, ref best);
            }
        }

        private void SearchRobustCandidates(
            Node node,
            Vector3 point,
            RobustSearchState state)
        {
            if (DistanceSquaredToBounds(
                    point,
                    node.Minimum,
                    node.Maximum) > state.MaximumDistanceSquared)
            {
                return;
            }

            if (node.Left == null || node.Right == null)
            {
                int end = node.Start + node.Count;
                for (int index = node.Start; index < end; index++)
                {
                    TriangleEntry triangle = triangles[index];
                    ClosestPointResult closest = FindClosestPoint(
                        point,
                        triangle);
                    double distance = Math.Sqrt(
                        Math.Max(
                            0.0,
                            DistanceSquared(point, closest.Point)));
                    if (distance > state.MaximumDistance)
                    {
                        continue;
                    }

                    double orthogonality = distance == 0.0
                        ? 1.0
                        : Math.Min(
                            1.0,
                            Math.Abs(
                                Dot(
                                    point - closest.Point,
                                    triangle.Normal)) / distance);
                    state.Consider(
                        new RobustCandidate(
                            triangle,
                            closest,
                            distance,
                            orthogonality));
                }

                return;
            }

            SearchRobustCandidates(node.Left, point, state);
            SearchRobustCandidates(node.Right, point, state);
        }

        private Bounds CalculateBounds(int start, int count)
        {
            Vector3 minimum = new Vector3(float.PositiveInfinity);
            Vector3 maximum = new Vector3(float.NegativeInfinity);
            int end = start + count;
            for (int index = start; index < end; index++)
            {
                minimum = Vector3.Min(minimum, triangles[index].Minimum);
                maximum = Vector3.Max(maximum, triangles[index].Maximum);
            }

            return new Bounds(minimum, maximum);
        }

        private Bounds CalculateCentroidBounds(int start, int count)
        {
            Vector3 minimum = new Vector3(float.PositiveInfinity);
            Vector3 maximum = new Vector3(float.NegativeInfinity);
            int end = start + count;
            for (int index = start; index < end; index++)
            {
                minimum = Vector3.Min(minimum, triangles[index].Centroid);
                maximum = Vector3.Max(maximum, triangles[index].Centroid);
            }

            return new Bounds(minimum, maximum);
        }

        private static TriangleEntry CreateEntry(MeshTriangle triangle)
        {
            if (triangle == null)
            {
                throw new ArgumentNullException(nameof(triangle));
            }

            if (!IsFinite(triangle.A)
                || !IsFinite(triangle.B)
                || !IsFinite(triangle.C))
            {
                throw new ArgumentException(
                    "Triangle "
                    + triangle.SourceTriangleIndex
                    + " contains a non-finite coordinate.",
                    nameof(triangle));
            }

            Vector3 a = ToVector(triangle.A, nameof(triangle));
            Vector3 b = ToVector(triangle.B, nameof(triangle));
            Vector3 c = ToVector(triangle.C, nameof(triangle));
            Vector3 cross = Vector3.Cross(b - a, c - a);
            double crossLengthSquared = Dot(cross, cross);
            if (!IsFinite(crossLengthSquared)
                || crossLengthSquared <= 0.0)
            {
                throw new ArgumentException(
                    "Triangle "
                    + triangle.SourceTriangleIndex
                    + " is degenerate.",
                    nameof(triangle));
            }

            Vector3 normal = cross
                / (float)Math.Sqrt(crossLengthSquared);
            Vector3 minimum = Vector3.Min(a, Vector3.Min(b, c));
            Vector3 maximum = Vector3.Max(a, Vector3.Max(b, c));
            Vector3 centroid = new Vector3(
                (float)(((double)a.X + b.X + c.X) / 3.0),
                (float)(((double)a.Y + b.Y + c.Y) / 3.0),
                (float)(((double)a.Z + b.Z + c.Z) / 3.0));
            return new TriangleEntry(
                triangle,
                a,
                b,
                c,
                minimum,
                maximum,
                centroid,
                normal);
        }

        private static ClosestPointResult FindClosestPoint(
            Vector3 point,
            TriangleEntry triangle)
        {
            Vector3 ab = triangle.B - triangle.A;
            Vector3 ac = triangle.C - triangle.A;
            Vector3 ap = point - triangle.A;
            double d1 = Dot(ab, ap);
            double d2 = Dot(ac, ap);
            if (d1 <= 0.0 && d2 <= 0.0)
            {
                return new ClosestPointResult(
                    triangle.A,
                    MeshClosestFeature.Vertex);
            }

            Vector3 bp = point - triangle.B;
            double d3 = Dot(ab, bp);
            double d4 = Dot(ac, bp);
            if (d3 >= 0.0 && d4 <= d3)
            {
                return new ClosestPointResult(
                    triangle.B,
                    MeshClosestFeature.Vertex);
            }

            double vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
            {
                double scale = d1 / (d1 - d3);
                return new ClosestPointResult(
                    triangle.A + (float)scale * ab,
                    MeshClosestFeature.Edge);
            }

            Vector3 cp = point - triangle.C;
            double d5 = Dot(ab, cp);
            double d6 = Dot(ac, cp);
            if (d6 >= 0.0 && d5 <= d6)
            {
                return new ClosestPointResult(
                    triangle.C,
                    MeshClosestFeature.Vertex);
            }

            double vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
            {
                double scale = d2 / (d2 - d6);
                return new ClosestPointResult(
                    triangle.A + (float)scale * ac,
                    MeshClosestFeature.Edge);
            }

            double va = d3 * d6 - d5 * d4;
            if (va <= 0.0 && d4 - d3 >= 0.0 && d5 - d6 >= 0.0)
            {
                double scale = (d4 - d3)
                    / ((d4 - d3) + (d5 - d6));
                return new ClosestPointResult(
                    triangle.B
                        + (float)scale * (triangle.C - triangle.B),
                    MeshClosestFeature.Edge);
            }

            double denominator = 1.0 / (va + vb + vc);
            double v = vb * denominator;
            double w = vc * denominator;
            return new ClosestPointResult(
                triangle.A + (float)v * ab + (float)w * ac,
                MeshClosestFeature.FaceInterior);
        }

        private static double DistanceSquared(
            Vector3 first,
            Vector3 second)
        {
            double x = (double)first.X - second.X;
            double y = (double)first.Y - second.Y;
            double z = (double)first.Z - second.Z;
            return x * x + y * y + z * z;
        }

        private static double DistanceSquaredToBounds(
            Vector3 point,
            Vector3 minimum,
            Vector3 maximum)
        {
            double x = AxisDistance(point.X, minimum.X, maximum.X);
            double y = AxisDistance(point.Y, minimum.Y, maximum.Y);
            double z = AxisDistance(point.Z, minimum.Z, maximum.Z);
            return x * x + y * y + z * z;
        }

        private static double AxisDistance(
            float value,
            float minimum,
            float maximum)
        {
            return value < minimum
                ? minimum - (double)value
                : value > maximum
                    ? value - (double)maximum
                    : 0.0;
        }

        private static double Dot(Vector3 first, Vector3 second)
        {
            return (double)first.X * second.X
                + (double)first.Y * second.Y
                + (double)first.Z * second.Z;
        }

        private static Vector3 ToVector(ThreeDPoint point, string name)
        {
            if (point == null || !point.IsFinite)
            {
                throw new ArgumentException(
                    name == "point"
                        ? "The query point must contain finite coordinates."
                        : "A triangle contains a non-finite coordinate.",
                    name);
            }

            Vector3 value = new Vector3(
                (float)point.X,
                (float)point.Y,
                (float)point.Z);
            if (!IsFinite(value))
            {
                throw new ArgumentException(
                    name == "point"
                        ? "The query point must contain finite coordinates."
                        : "A triangle contains a non-finite coordinate.",
                    name);
            }

            return value;
        }

        private static PointMeshDistance CreateDistance(
            TriangleEntry triangle,
            ClosestPointResult closest,
            double unsignedDistance,
            double? signedDistance,
            bool signResolved)
        {
            return new PointMeshDistance(
                triangle.Source.SourceTriangleIndex,
                ToPoint(closest.Point),
                ToPoint(triangle.Normal),
                closest.Feature,
                unsignedDistance,
                signedDistance,
                signResolved);
        }

        private static ThreeDPoint ToPoint(Vector3 value)
        {
            return new ThreeDPoint(value.X, value.Y, value.Z);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsFinite(ThreeDPoint value)
        {
            return value != null
                && value.IsFinite
                && Math.Abs(value.X) <= float.MaxValue
                && Math.Abs(value.Y) <= float.MaxValue
                && Math.Abs(value.Z) <= float.MaxValue;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.X) && !float.IsInfinity(value.X)
                && !float.IsNaN(value.Y) && !float.IsInfinity(value.Y)
                && !float.IsNaN(value.Z) && !float.IsInfinity(value.Z);
        }

        private sealed class Node
        {
            public Node(
                Vector3 minimum,
                Vector3 maximum,
                int start,
                int count,
                Node left,
                Node right)
            {
                Minimum = minimum;
                Maximum = maximum;
                Start = start;
                Count = count;
                Left = left;
                Right = right;
            }

            public Vector3 Minimum { get; }
            public Vector3 Maximum { get; }
            public int Start { get; }
            public int Count { get; }
            public Node Left { get; }
            public Node Right { get; }
        }

        private sealed class TriangleEntry
        {
            public TriangleEntry(
                MeshTriangle source,
                Vector3 a,
                Vector3 b,
                Vector3 c,
                Vector3 minimum,
                Vector3 maximum,
                Vector3 centroid,
                Vector3 normal)
            {
                Source = source;
                A = a;
                B = b;
                C = c;
                Minimum = minimum;
                Maximum = maximum;
                Centroid = centroid;
                Normal = normal;
            }

            public MeshTriangle Source { get; }
            public Vector3 A { get; }
            public Vector3 B { get; }
            public Vector3 C { get; }
            public Vector3 Minimum { get; }
            public Vector3 Maximum { get; }
            public Vector3 Centroid { get; }
            public Vector3 Normal { get; }
        }

        private struct ClosestPointResult
        {
            public ClosestPointResult(
                Vector3 point,
                MeshClosestFeature feature)
            {
                Point = point;
                Feature = feature;
            }

            public Vector3 Point { get; }
            public MeshClosestFeature Feature { get; }
        }

        private struct SearchResult
        {
            public SearchResult(
                double distanceSquared,
                long sourceTriangleIndex,
                TriangleEntry triangle,
                ClosestPointResult closest)
            {
                DistanceSquared = distanceSquared;
                SourceTriangleIndex = sourceTriangleIndex;
                Triangle = triangle;
                Closest = closest;
            }

            public double DistanceSquared { get; }
            public long SourceTriangleIndex { get; }
            public TriangleEntry Triangle { get; }
            public ClosestPointResult Closest { get; }
        }

        private sealed class RobustCandidate
        {
            public RobustCandidate(
                TriangleEntry triangle,
                ClosestPointResult closest,
                double distance,
                double orthogonality)
            {
                Triangle = triangle;
                Closest = closest;
                Distance = distance;
                Orthogonality = orthogonality;
            }

            public TriangleEntry Triangle { get; }
            public ClosestPointResult Closest { get; }
            public double Distance { get; }
            public double Orthogonality { get; }
        }

        private sealed class RobustSearchState
        {
            public RobustSearchState(double maximumDistance)
            {
                MaximumDistance = maximumDistance;
                MaximumDistanceSquared = maximumDistance * maximumDistance;
            }

            public double MaximumDistance { get; }
            public double MaximumDistanceSquared { get; }
            public RobustCandidate BestInterior { get; private set; }
            public RobustCandidate BestBoundary { get; private set; }

            public void Consider(RobustCandidate candidate)
            {
                if (candidate.Closest.Feature
                    == MeshClosestFeature.FaceInterior)
                {
                    if (BestInterior == null
                        || candidate.Distance < BestInterior.Distance
                        || candidate.Distance == BestInterior.Distance
                        && candidate.Triangle.Source.SourceTriangleIndex
                            < BestInterior.Triangle.Source.SourceTriangleIndex)
                    {
                        BestInterior = candidate;
                    }

                    return;
                }

                if (BestBoundary == null)
                {
                    BestBoundary = candidate;
                    return;
                }

                double distanceDifference =
                    candidate.Distance - BestBoundary.Distance;
                if (Math.Abs(distanceDifference)
                    <= RobustSignDistanceEpsilon)
                {
                    if (candidate.Orthogonality
                            > BestBoundary.Orthogonality
                        || candidate.Orthogonality
                            == BestBoundary.Orthogonality
                        && candidate.Triangle.Source.SourceTriangleIndex
                            < BestBoundary.Triangle.Source.SourceTriangleIndex)
                    {
                        BestBoundary = candidate;
                    }
                }
                else if (distanceDifference < 0.0)
                {
                    BestBoundary = candidate;
                }
            }
        }

        private sealed class CentroidComparer : IComparer<TriangleEntry>
        {
            private static readonly CentroidComparer X =
                new CentroidComparer(0);
            private static readonly CentroidComparer Y =
                new CentroidComparer(1);
            private static readonly CentroidComparer Z =
                new CentroidComparer(2);

            private readonly int axis;

            private CentroidComparer(int axis)
            {
                this.axis = axis;
            }

            public static CentroidComparer ForAxis(int axis)
            {
                return axis == 0 ? X : axis == 1 ? Y : Z;
            }

            public int Compare(TriangleEntry first, TriangleEntry second)
            {
                int comparison = GetAxis(first.Centroid)
                    .CompareTo(GetAxis(second.Centroid));
                return comparison != 0
                    ? comparison
                    : first.Source.SourceTriangleIndex.CompareTo(
                        second.Source.SourceTriangleIndex);
            }

            private float GetAxis(Vector3 value)
            {
                return axis == 0
                    ? value.X
                    : axis == 1
                        ? value.Y
                        : value.Z;
            }
        }

        private struct Bounds
        {
            public Bounds(Vector3 minimum, Vector3 maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            public Vector3 Minimum { get; }
            public Vector3 Maximum { get; }
        }
    }

    internal struct Vector3
    {
        public Vector3(float value)
            : this(value, value, value)
        {
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public static Vector3 Min(Vector3 first, Vector3 second)
        {
            return new Vector3(
                Math.Min(first.X, second.X),
                Math.Min(first.Y, second.Y),
                Math.Min(first.Z, second.Z));
        }

        public static Vector3 Max(Vector3 first, Vector3 second)
        {
            return new Vector3(
                Math.Max(first.X, second.X),
                Math.Max(first.Y, second.Y),
                Math.Max(first.Z, second.Z));
        }

        public static Vector3 Cross(Vector3 first, Vector3 second)
        {
            return new Vector3(
                first.Y * second.Z - first.Z * second.Y,
                first.Z * second.X - first.X * second.Z,
                first.X * second.Y - first.Y * second.X);
        }

        public static Vector3 operator +(Vector3 first, Vector3 second)
        {
            return new Vector3(
                first.X + second.X,
                first.Y + second.Y,
                first.Z + second.Z);
        }

        public static Vector3 operator -(Vector3 first, Vector3 second)
        {
            return new Vector3(
                first.X - second.X,
                first.Y - second.Y,
                first.Z - second.Z);
        }

        public static Vector3 operator *(float scale, Vector3 value)
        {
            return new Vector3(
                scale * value.X,
                scale * value.Y,
                scale * value.Z);
        }

        public static Vector3 operator /(Vector3 value, float scale)
        {
            return new Vector3(
                value.X / scale,
                value.Y / scale,
                value.Z / scale);
        }
    }
}
