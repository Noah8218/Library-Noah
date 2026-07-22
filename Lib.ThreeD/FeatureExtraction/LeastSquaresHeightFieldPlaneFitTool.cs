using System;
using System.Collections.Generic;

namespace Lib.ThreeD.FeatureExtraction
{
    /// <summary>
    /// Source-neutral sample for fitting display Y as a function of display X/Z.
    /// RawHeight is carried independently for callers that retain sensor values.
    /// </summary>
    public sealed class HeightFieldPlaneFitSample
    {
        public HeightFieldPlaneFitSample(ThreeDPoint position, double rawHeight)
        {
            Position = position;
            RawHeight = rawHeight;
        }

        public ThreeDPoint Position { get; }

        public double RawHeight { get; }
    }

    public sealed class LeastSquaresHeightFieldPlaneFitResult
    {
        public LeastSquaresHeightFieldPlaneFitResult(
            double slopeX,
            double slopeZ,
            double intercept,
            ThreeDPoint normal,
            double offset,
            int sampleCount,
            double rootMeanSquareDistance,
            ThreeDPoint target,
            ThreeDPoint targetProjection,
            double targetSignedDistance,
            double targetRawHeight,
            double targetRawReferenceHeight)
        {
            SlopeX = slopeX;
            SlopeZ = slopeZ;
            Intercept = intercept;
            Normal = normal;
            Offset = offset;
            SampleCount = sampleCount;
            RootMeanSquareDistance = rootMeanSquareDistance;
            Target = target;
            TargetProjection = targetProjection;
            TargetSignedDistance = targetSignedDistance;
            TargetRawHeight = targetRawHeight;
            TargetRawReferenceHeight = targetRawReferenceHeight;
        }

        public double SlopeX { get; }
        public double SlopeZ { get; }
        public double Intercept { get; }
        public ThreeDPoint Normal { get; }
        public double Offset { get; }
        public int SampleCount { get; }
        public double RootMeanSquareDistance { get; }
        public ThreeDPoint Target { get; }
        public ThreeDPoint TargetProjection { get; }
        public double TargetSignedDistance { get; }
        public double TargetAbsoluteDistance => Math.Abs(TargetSignedDistance);
        public double TargetRawHeight { get; }
        public double TargetRawReferenceHeight { get; }
        public double TargetRawHeightDelta => TargetRawHeight - TargetRawReferenceHeight;

        public double EvaluateY(double x, double z)
        {
            return (SlopeX * x) + (SlopeZ * z) + Intercept;
        }
    }

    /// <summary>
    /// Fits Y = slopeX * X + slopeZ * Z + intercept by least squares.
    /// The finite/degenerate contracts and float-compatible distance arithmetic
    /// are deterministic so existing desktop adapters can preserve their output.
    /// </summary>
    public sealed class LeastSquaresHeightFieldPlaneFitTool
    {
        public LeastSquaresHeightFieldPlaneFitResult Execute(IReadOnlyList<HeightFieldPlaneFitSample> samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (samples.Count < 3)
            {
                throw new ArgumentException("Plane fitting requires at least three samples.", nameof(samples));
            }

            double meanX = 0.0;
            double meanY = 0.0;
            double meanZ = 0.0;
            double meanRaw = 0.0;
            foreach (HeightFieldPlaneFitSample sample in samples)
            {
                if (!IsFinite(sample))
                {
                    throw new ArgumentException("Plane fitting samples must contain finite coordinates and raw heights.", nameof(samples));
                }

                meanX += sample.Position.X;
                meanY += sample.Position.Y;
                meanZ += sample.Position.Z;
                meanRaw += sample.RawHeight;
            }

            meanX /= samples.Count;
            meanY /= samples.Count;
            meanZ /= samples.Count;
            meanRaw /= samples.Count;

            double sumXX = 0.0;
            double sumXZ = 0.0;
            double sumZZ = 0.0;
            double sumXY = 0.0;
            double sumZY = 0.0;
            double sumXRaw = 0.0;
            double sumZRaw = 0.0;
            foreach (HeightFieldPlaneFitSample sample in samples)
            {
                double x = sample.Position.X - meanX;
                double y = sample.Position.Y - meanY;
                double z = sample.Position.Z - meanZ;
                double raw = sample.RawHeight - meanRaw;
                sumXX += x * x;
                sumXZ += x * z;
                sumZZ += z * z;
                sumXY += x * y;
                sumZY += z * y;
                sumXRaw += x * raw;
                sumZRaw += z * raw;
            }

            double determinant = (sumXX * sumZZ) - (sumXZ * sumXZ);
            double determinantScale = Math.Max(1.0, Math.Abs(sumXX * sumZZ));
            if (Math.Abs(determinant) <= determinantScale * 1e-12)
            {
                throw new ArgumentException("Plane fitting samples must span two horizontal axes.", nameof(samples));
            }

            double slopeX = ((sumXY * sumZZ) - (sumZY * sumXZ)) / determinant;
            double slopeZ = ((sumZY * sumXX) - (sumXY * sumXZ)) / determinant;
            double intercept = meanY - (slopeX * meanX) - (slopeZ * meanZ);
            double rawSlopeX = ((sumXRaw * sumZZ) - (sumZRaw * sumXZ)) / determinant;
            double rawSlopeZ = ((sumZRaw * sumXX) - (sumXRaw * sumXZ)) / determinant;
            double rawIntercept = meanRaw - (rawSlopeX * meanX) - (rawSlopeZ * meanZ);

            double normalLength = Math.Sqrt((slopeX * slopeX) + 1.0 + (slopeZ * slopeZ));
            ThreeDPoint normal = new ThreeDPoint(
                (float)(-slopeX / normalLength),
                (float)(1.0 / normalLength),
                (float)(-slopeZ / normalLength));
            double offset = -intercept / normalLength;

            HeightFieldPlaneFitSample target = samples[0];
            double targetSignedDistance = SignedDistance(target.Position, normal, offset);
            double squaredDistanceSum = targetSignedDistance * targetSignedDistance;
            for (int index = 1; index < samples.Count; index++)
            {
                double signedDistance = SignedDistance(samples[index].Position, normal, offset);
                squaredDistanceSum += signedDistance * signedDistance;
                if (Math.Abs(signedDistance) > Math.Abs(targetSignedDistance))
                {
                    target = samples[index];
                    targetSignedDistance = signedDistance;
                }
            }

            ThreeDPoint projection = Project(target.Position, normal, targetSignedDistance);
            double rawReference = (rawSlopeX * target.Position.X) + (rawSlopeZ * target.Position.Z) + rawIntercept;
            return new LeastSquaresHeightFieldPlaneFitResult(
                slopeX,
                slopeZ,
                intercept,
                normal,
                offset,
                samples.Count,
                Math.Sqrt(squaredDistanceSum / samples.Count),
                target.Position,
                projection,
                targetSignedDistance,
                target.RawHeight,
                rawReference);
        }

        internal static double SignedDistance(ThreeDPoint point, ThreeDPoint normal, double offset)
        {
            float dot = ((float)normal.X * (float)point.X)
                + ((float)normal.Y * (float)point.Y)
                + ((float)normal.Z * (float)point.Z);
            return dot + offset;
        }

        internal static ThreeDPoint Project(ThreeDPoint point, ThreeDPoint normal, double signedDistance)
        {
            float distance = (float)signedDistance;
            return new ThreeDPoint(
                (float)point.X - ((float)normal.X * distance),
                (float)point.Y - ((float)normal.Y * distance),
                (float)point.Z - ((float)normal.Z * distance));
        }

        private static bool IsFinite(HeightFieldPlaneFitSample sample)
        {
            return sample != null
                && sample.Position != null
                && sample.Position.IsFinite
                && IsFinite(sample.RawHeight);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
