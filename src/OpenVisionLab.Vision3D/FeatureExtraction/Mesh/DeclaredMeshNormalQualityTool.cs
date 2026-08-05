using System;
using System.Collections.Generic;
using System.Threading;

namespace OpenVisionLab.Vision3D.FeatureExtraction
{
    public enum DeclaredMeshNormalQualityState
    {
        Unavailable = 0,
        Valid = 1,
        Invalid = 2
    }

    public sealed class DeclaredMeshNormalQualityResult
    {
        internal DeclaredMeshNormalQualityResult(
            DeclaredMeshNormalQualityState state,
            int positionCount,
            int triangleCount,
            int normalCount,
            int finiteNormalCount,
            int nonZeroNormalCount,
            int unitLengthNormalCount,
            int invalidIndexCount,
            int degenerateTriangleCount,
            int comparableCornerCount,
            int consistentCornerCount,
            int reversedCornerCount,
            double minimumNormalLength,
            double maximumNormalLength,
            double meanNormalLength,
            double minimumAlignment,
            double meanAlignment)
        {
            State = state;
            PositionCount = positionCount;
            TriangleCount = triangleCount;
            NormalCount = normalCount;
            FiniteNormalCount = finiteNormalCount;
            NonZeroNormalCount = nonZeroNormalCount;
            UnitLengthNormalCount = unitLengthNormalCount;
            InvalidIndexCount = invalidIndexCount;
            DegenerateTriangleCount = degenerateTriangleCount;
            ComparableCornerCount = comparableCornerCount;
            ConsistentCornerCount = consistentCornerCount;
            ReversedCornerCount = reversedCornerCount;
            MinimumNormalLength = minimumNormalLength;
            MaximumNormalLength = maximumNormalLength;
            MeanNormalLength = meanNormalLength;
            MinimumAlignment = minimumAlignment;
            MeanAlignment = meanAlignment;
        }

        public DeclaredMeshNormalQualityState State { get; }
        public int PositionCount { get; }
        public int TriangleCount { get; }
        public int NormalCount { get; }
        public int FiniteNormalCount { get; }
        public int NonZeroNormalCount { get; }
        public int UnitLengthNormalCount { get; }
        public int InvalidIndexCount { get; }
        public int DegenerateTriangleCount { get; }
        public int ComparableCornerCount { get; }
        public int ConsistentCornerCount { get; }
        public int ReversedCornerCount { get; }
        public double MinimumNormalLength { get; }
        public double MaximumNormalLength { get; }
        public double MeanNormalLength { get; }
        public double MinimumAlignment { get; }
        public double MeanAlignment { get; }
    }

    /// <summary>
    /// Evaluates a caller-declared per-position normal channel against mesh
    /// topology. Source identity, format, repair policy, and persistence stay
    /// with the caller.
    /// </summary>
    public sealed class DeclaredMeshNormalQualityTool
    {
        private const double ZeroLengthSquared = 1e-20;

        public DeclaredMeshNormalQualityResult Execute(
            IReadOnlyList<ThreeDPoint> positions,
            IReadOnlyList<int> triangleIndices,
            IReadOnlyList<ThreeDPoint> declaredNormals,
            IReadOnlyList<bool> normalPresence,
            double unitLengthTolerance,
            double minimumAlignmentCosine,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Validate(
                positions,
                triangleIndices,
                declaredNormals,
                normalPresence,
                unitLengthTolerance,
                minimumAlignmentCosine);

            int triangleCount = triangleIndices.Count / 3;
            int normalCount = CountPresent(declaredNormals, normalPresence);
            if (normalCount == 0)
            {
                return new DeclaredMeshNormalQualityResult(
                    DeclaredMeshNormalQualityState.Unavailable,
                    positions.Count,
                    triangleCount,
                    0, 0, 0, 0, 0, 0, 0, 0, 0,
                    double.NaN, double.NaN, double.NaN,
                    double.NaN, double.NaN);
            }

            int finiteNormalCount = 0;
            int nonZeroNormalCount = 0;
            int unitLengthNormalCount = 0;
            double minimumNormalLength = double.PositiveInfinity;
            double maximumNormalLength = double.NegativeInfinity;
            double normalLengthSum = 0.0;
            for (int index = 0; index < declaredNormals.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsPresent(index, declaredNormals, normalPresence)) continue;
                ThreeDPoint normal = declaredNormals[index];
                if (!IsFinite(normal)) continue;
                finiteNormalCount++;
                double lengthSquared = LengthSquared(normal);
                double length = Math.Sqrt(lengthSquared);
                minimumNormalLength = Math.Min(minimumNormalLength, length);
                maximumNormalLength = Math.Max(maximumNormalLength, length);
                normalLengthSum += length;
                if (lengthSquared <= ZeroLengthSquared) continue;
                nonZeroNormalCount++;
                if (Math.Abs(length - 1.0) <= unitLengthTolerance)
                {
                    unitLengthNormalCount++;
                }
            }

            int invalidIndexCount = 0;
            int degenerateTriangleCount = 0;
            int comparableCornerCount = 0;
            int consistentCornerCount = 0;
            int reversedCornerCount = 0;
            double minimumAlignment = double.PositiveInfinity;
            double alignmentSum = 0.0;
            for (int offset = 0; offset + 2 < triangleIndices.Count; offset += 3)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int aIndex = triangleIndices[offset];
                int bIndex = triangleIndices[offset + 1];
                int cIndex = triangleIndices[offset + 2];
                int invalid = (InRange(aIndex, positions.Count) ? 0 : 1)
                    + (InRange(bIndex, positions.Count) ? 0 : 1)
                    + (InRange(cIndex, positions.Count) ? 0 : 1);
                if (invalid > 0)
                {
                    invalidIndexCount += invalid;
                    continue;
                }

                ThreeDPoint geometric = Cross(
                    Subtract(positions[bIndex], positions[aIndex]),
                    Subtract(positions[cIndex], positions[aIndex]));
                double geometricLengthSquared = LengthSquared(geometric);
                if (!IsFinite(geometric)
                    || geometricLengthSquared <= ZeroLengthSquared)
                {
                    degenerateTriangleCount++;
                    continue;
                }

                double geometricLength = Math.Sqrt(geometricLengthSquared);
                int[] cornerIndices = { aIndex, bIndex, cIndex };
                for (int corner = 0; corner < cornerIndices.Length; corner++)
                {
                    int index = cornerIndices[corner];
                    if (!IsPresent(index, declaredNormals, normalPresence)) continue;
                    ThreeDPoint normal = declaredNormals[index];
                    double normalLengthSquared = IsFinite(normal)
                        ? LengthSquared(normal)
                        : 0.0;
                    if (!IsFinite(normal)
                        || normalLengthSquared <= ZeroLengthSquared)
                    {
                        continue;
                    }

                    double alignment = Dot(normal, geometric)
                        / (Math.Sqrt(normalLengthSquared) * geometricLength);
                    comparableCornerCount++;
                    minimumAlignment = Math.Min(minimumAlignment, alignment);
                    alignmentSum += alignment;
                    if (alignment >= minimumAlignmentCosine)
                    {
                        consistentCornerCount++;
                    }
                    if (alignment < 0.0) reversedCornerCount++;
                }
            }

            bool dense = positions.Count > 0
                && declaredNormals.Count == positions.Count
                && normalCount == positions.Count;
            bool allComparable = comparableCornerCount == triangleCount * 3;
            bool valid = triangleIndices.Count > 0
                && triangleIndices.Count % 3 == 0
                && dense
                && finiteNormalCount == normalCount
                && nonZeroNormalCount == normalCount
                && unitLengthNormalCount == normalCount
                && invalidIndexCount == 0
                && degenerateTriangleCount == 0
                && allComparable
                && consistentCornerCount == comparableCornerCount;

            return new DeclaredMeshNormalQualityResult(
                valid
                    ? DeclaredMeshNormalQualityState.Valid
                    : DeclaredMeshNormalQualityState.Invalid,
                positions.Count,
                triangleCount,
                normalCount,
                finiteNormalCount,
                nonZeroNormalCount,
                unitLengthNormalCount,
                invalidIndexCount,
                degenerateTriangleCount,
                comparableCornerCount,
                consistentCornerCount,
                reversedCornerCount,
                finiteNormalCount == 0 ? double.NaN : minimumNormalLength,
                finiteNormalCount == 0 ? double.NaN : maximumNormalLength,
                finiteNormalCount == 0 ? double.NaN : normalLengthSum / finiteNormalCount,
                comparableCornerCount == 0 ? double.NaN : minimumAlignment,
                comparableCornerCount == 0 ? double.NaN : alignmentSum / comparableCornerCount);
        }

        private static void Validate(
            IReadOnlyList<ThreeDPoint> positions,
            IReadOnlyList<int> triangleIndices,
            IReadOnlyList<ThreeDPoint> declaredNormals,
            IReadOnlyList<bool> normalPresence,
            double unitLengthTolerance,
            double minimumAlignmentCosine)
        {
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (triangleIndices == null) throw new ArgumentNullException(nameof(triangleIndices));
            if (declaredNormals == null) throw new ArgumentNullException(nameof(declaredNormals));
            if (normalPresence != null
                && normalPresence.Count > 0
                && normalPresence.Count != declaredNormals.Count)
            {
                throw new ArgumentException(
                    "Normal presence must be empty or match the normal storage count.",
                    nameof(normalPresence));
            }
            if (!IsFinite(unitLengthTolerance) || unitLengthTolerance < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(unitLengthTolerance));
            }
            if (!IsFinite(minimumAlignmentCosine)
                || minimumAlignmentCosine < -1.0
                || minimumAlignmentCosine > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumAlignmentCosine));
            }
        }

        private static int CountPresent(
            IReadOnlyList<ThreeDPoint> normals,
            IReadOnlyList<bool> presence)
        {
            if (presence == null || presence.Count == 0) return normals.Count;
            int count = 0;
            for (int index = 0; index < presence.Count; index++)
            {
                if (presence[index]) count++;
            }
            return count;
        }

        private static bool IsPresent(
            int index,
            IReadOnlyList<ThreeDPoint> normals,
            IReadOnlyList<bool> presence)
        {
            return InRange(index, normals.Count)
                && (presence == null || presence.Count == 0 || presence[index]);
        }

        private static bool InRange(int index, int count)
        {
            return index >= 0 && index < count;
        }

        private static bool IsFinite(ThreeDPoint point)
        {
            return point != null && IsFinite(point.X) && IsFinite(point.Y) && IsFinite(point.Z);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static ThreeDPoint Subtract(ThreeDPoint left, ThreeDPoint right)
        {
            return new ThreeDPoint(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        private static ThreeDPoint Cross(ThreeDPoint left, ThreeDPoint right)
        {
            return new ThreeDPoint(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }

        private static double Dot(ThreeDPoint left, ThreeDPoint right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        private static double LengthSquared(ThreeDPoint point)
        {
            return Dot(point, point);
        }
    }
}
