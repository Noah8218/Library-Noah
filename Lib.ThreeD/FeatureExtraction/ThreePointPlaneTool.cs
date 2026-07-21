using System;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    public sealed class ThreePointPlaneInput
    {
        public ThreePointPlaneInput(ThreeDPoint firstPoint, ThreeDPoint secondPoint, ThreeDPoint thirdPoint)
        {
            FirstPoint = firstPoint;
            SecondPoint = secondPoint;
            ThirdPoint = thirdPoint;
        }

        public ThreeDPoint FirstPoint { get; }

        public ThreeDPoint SecondPoint { get; }

        public ThreeDPoint ThirdPoint { get; }
    }

    public sealed class ThreePointPlaneResult
    {
        private ThreePointPlaneResult(
            bool success,
            string message,
            ThreeDPoint anchor,
            ThreeDPoint normal,
            double planeOffset,
            ThreeDPoint supportFirst,
            ThreeDPoint supportSecond,
            ThreeDPoint supportThird,
            double normalizedCrossMagnitude)
        {
            Success = success;
            Message = message ?? string.Empty;
            Anchor = anchor;
            Normal = normal;
            PlaneOffset = planeOffset;
            SupportFirst = supportFirst;
            SupportSecond = supportSecond;
            SupportThird = supportThird;
            NormalizedCrossMagnitude = normalizedCrossMagnitude;
        }

        public bool Success { get; }

        public string Message { get; }

        public ThreeDPoint Anchor { get; }

        public ThreeDPoint Normal { get; }

        public double PlaneOffset { get; }

        public ThreeDPoint SupportFirst { get; }

        public ThreeDPoint SupportSecond { get; }

        public ThreeDPoint SupportThird { get; }

        public double NormalizedCrossMagnitude { get; }

        internal static ThreePointPlaneResult Completed(
            ThreeDPoint anchor,
            ThreeDPoint normal,
            double planeOffset,
            ThreeDPoint supportFirst,
            ThreeDPoint supportSecond,
            ThreeDPoint supportThird,
            double normalizedCrossMagnitude)
        {
            return new ThreePointPlaneResult(
                true,
                "Completed ordered three-point source-coordinate plane construction.",
                anchor,
                normal,
                planeOffset,
                supportFirst,
                supportSecond,
                supportThird,
                normalizedCrossMagnitude);
        }

        internal static ThreePointPlaneResult Failed(string message)
        {
            return new ThreePointPlaneResult(false, message, null, null, double.NaN, null, null, null, double.NaN);
        }
    }

    /// <summary>
    /// Pure ordered three-point full-XYZ plane construction. Point order fixes
    /// normal direction; this tool performs no picking, fitting, calibration,
    /// tolerance, or acceptance evaluation.
    /// </summary>
    public sealed class ThreePointPlaneTool
    {
        private const double MinimumNormalizedCrossMagnitude = 1e-12;

        public ThreePointPlaneResult Execute(ThreePointPlaneInput input, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (input == null || input.FirstPoint == null || input.SecondPoint == null || input.ThirdPoint == null)
                {
                    return ThreePointPlaneResult.Failed("Three-point plane requires three explicit points.");
                }
                if (!input.FirstPoint.IsFinite || !input.SecondPoint.IsFinite || !input.ThirdPoint.IsFinite)
                {
                    return ThreePointPlaneResult.Failed("Three-point plane requires finite point coordinates.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                double ux = input.SecondPoint.X - input.FirstPoint.X;
                double uy = input.SecondPoint.Y - input.FirstPoint.Y;
                double uz = input.SecondPoint.Z - input.FirstPoint.Z;
                double vx = input.ThirdPoint.X - input.FirstPoint.X;
                double vy = input.ThirdPoint.Y - input.FirstPoint.Y;
                double vz = input.ThirdPoint.Z - input.FirstPoint.Z;
                double uLength = Math.Sqrt((ux * ux) + (uy * uy) + (uz * uz));
                double vLength = Math.Sqrt((vx * vx) + (vy * vy) + (vz * vz));
                if (!IsFinitePositive(uLength) || !IsFinitePositive(vLength))
                {
                    return ThreePointPlaneResult.Failed("Three-point plane rejects duplicate or non-finite support points.");
                }

                double crossX = (uy * vz) - (uz * vy);
                double crossY = (uz * vx) - (ux * vz);
                double crossZ = (ux * vy) - (uy * vx);
                double crossLength = Math.Sqrt((crossX * crossX) + (crossY * crossY) + (crossZ * crossZ));
                double normalizedCrossMagnitude = crossLength / (uLength * vLength);
                if (!IsFinitePositive(crossLength) || !IsFinitePositive(normalizedCrossMagnitude)
                    || normalizedCrossMagnitude <= MinimumNormalizedCrossMagnitude)
                {
                    return ThreePointPlaneResult.Failed("Three-point plane rejects collinear or numerically degenerate support points.");
                }

                ThreeDPoint normal = new ThreeDPoint(crossX / crossLength, crossY / crossLength, crossZ / crossLength);
                double planeOffset = -((normal.X * input.FirstPoint.X) + (normal.Y * input.FirstPoint.Y) + (normal.Z * input.FirstPoint.Z));
                if (!normal.IsFinite || double.IsNaN(planeOffset) || double.IsInfinity(planeOffset))
                {
                    return ThreePointPlaneResult.Failed("Three-point plane produced non-finite plane geometry.");
                }

                return ThreePointPlaneResult.Completed(
                    input.FirstPoint,
                    normal,
                    planeOffset,
                    input.FirstPoint,
                    input.SecondPoint,
                    input.ThirdPoint,
                    normalizedCrossMagnitude);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return ThreePointPlaneResult.Failed("Three-point plane execution failed: " + exception.Message);
            }
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
        }
    }
}
