using System;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    public sealed class TwoPointLineInput
    {
        public TwoPointLineInput(ThreeDPoint firstPoint, ThreeDPoint secondPoint)
        {
            FirstPoint = firstPoint;
            SecondPoint = secondPoint;
        }

        public ThreeDPoint FirstPoint { get; }

        public ThreeDPoint SecondPoint { get; }
    }

    public sealed class TwoPointLineResult
    {
        private TwoPointLineResult(
            bool success,
            string message,
            ThreeDPoint anchor,
            ThreeDPoint direction,
            ThreeDPoint segmentStart,
            ThreeDPoint segmentEnd,
            double segmentLength)
        {
            Success = success;
            Message = message ?? string.Empty;
            Anchor = anchor;
            Direction = direction;
            SegmentStart = segmentStart;
            SegmentEnd = segmentEnd;
            SegmentLength = segmentLength;
        }

        public bool Success { get; }

        public string Message { get; }

        public ThreeDPoint Anchor { get; }

        public ThreeDPoint Direction { get; }

        public ThreeDPoint SegmentStart { get; }

        public ThreeDPoint SegmentEnd { get; }

        public double SegmentLength { get; }

        internal static TwoPointLineResult Completed(
            ThreeDPoint anchor,
            ThreeDPoint direction,
            ThreeDPoint segmentStart,
            ThreeDPoint segmentEnd,
            double segmentLength)
        {
            return new TwoPointLineResult(true, "Completed ordered two-point source-coordinate line construction.", anchor, direction, segmentStart, segmentEnd, segmentLength);
        }

        internal static TwoPointLineResult Failed(string message)
        {
            return new TwoPointLineResult(false, message, null, null, null, null, double.NaN);
        }
    }

    /// <summary>
    /// Pure ordered two-point full-XYZ segment construction. It performs no
    /// picking, fitting, snapping, calibration, or acceptance evaluation.
    /// </summary>
    public sealed class TwoPointLineTool
    {
        public TwoPointLineResult Execute(TwoPointLineInput input, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (input == null || input.FirstPoint == null || input.SecondPoint == null)
                {
                    return TwoPointLineResult.Failed("Two-point line requires two explicit points.");
                }
                if (!input.FirstPoint.IsFinite || !input.SecondPoint.IsFinite)
                {
                    return TwoPointLineResult.Failed("Two-point line requires finite point coordinates.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                double dx = input.SecondPoint.X - input.FirstPoint.X;
                double dy = input.SecondPoint.Y - input.FirstPoint.Y;
                double dz = input.SecondPoint.Z - input.FirstPoint.Z;
                double length = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                if (double.IsNaN(length) || double.IsInfinity(length) || length <= 0.0)
                {
                    return TwoPointLineResult.Failed("Two-point line rejects a zero-length or non-finite segment.");
                }

                return TwoPointLineResult.Completed(
                    input.FirstPoint,
                    new ThreeDPoint(dx / length, dy / length, dz / length),
                    input.FirstPoint,
                    input.SecondPoint,
                    length);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return TwoPointLineResult.Failed("Two-point line execution failed: " + exception.Message);
            }
        }
    }
}
