using System;
using System.Collections.Generic;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    /// <summary>
    /// One caller-owned source point with its stable grid locator and scalar
    /// evidence. Coordinate-system and source-file ownership remain outside
    /// this source-neutral library.
    /// </summary>
    public readonly struct AffinePointCloudInputPoint
    {
        public AffinePointCloudInputPoint(
            int row,
            int column,
            double rawHeight,
            double sourceX,
            double sourceY,
            double sourceZ)
        {
            Row = row;
            Column = column;
            RawHeight = rawHeight;
            SourceX = sourceX;
            SourceY = sourceY;
            SourceZ = sourceZ;
        }

        public int Row { get; }

        public int Column { get; }

        public double RawHeight { get; }

        public double SourceX { get; }

        public double SourceY { get; }

        public double SourceZ { get; }
    }

    /// <summary>
    /// Immutable transformed point retaining the caller's source locator and
    /// scalar evidence. It does not imply a re-gridded surface or mesh.
    /// </summary>
    public readonly struct AffinePointCloudPoint
    {
        public AffinePointCloudPoint(
            int row,
            int column,
            double rawHeight,
            double transformedX,
            double transformedY,
            double transformedZ)
        {
            Row = row;
            Column = column;
            RawHeight = rawHeight;
            TransformedX = transformedX;
            TransformedY = transformedY;
            TransformedZ = transformedZ;
        }

        public int Row { get; }

        public int Column { get; }

        public double RawHeight { get; }

        public double TransformedX { get; }

        public double TransformedY { get; }

        public double TransformedZ { get; }
    }

    public sealed class AffinePointCloudApplyResult
    {
        private AffinePointCloudApplyResult(bool success, string message, IReadOnlyList<AffinePointCloudPoint> points)
        {
            Success = success;
            Message = message ?? string.Empty;
            Points = points ?? Array.Empty<AffinePointCloudPoint>();
        }

        public bool Success { get; }

        public string Message { get; }

        public IReadOnlyList<AffinePointCloudPoint> Points { get; }

        internal static AffinePointCloudApplyResult Completed(IReadOnlyList<AffinePointCloudPoint> points)
        {
            return new AffinePointCloudApplyResult(true, "Completed ordered full-XYZ affine point-cloud application.", points);
        }

        internal static AffinePointCloudApplyResult Failed(string message)
        {
            return new AffinePointCloudApplyResult(false, message, Array.Empty<AffinePointCloudPoint>());
        }
    }

    /// <summary>
    /// Applies one caller-supplied full-XYZ affine matrix to ordered finite
    /// source points exactly once. This tool never reads C3D, re-grids,
    /// interpolates, triangulates, or measures.
    /// </summary>
    public sealed class AffinePointCloudApplyTool
    {
        public AffinePointCloudApplyResult Execute(
            IReadOnlyList<AffinePointCloudInputPoint> points,
            FullXyzAffineMatrix matrix,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                ValidateMatrix(matrix);
                if (points == null) throw new ArgumentNullException(nameof(points));

                List<AffinePointCloudPoint> output = new List<AffinePointCloudPoint>(points.Count);
                HashSet<long> locators = new HashSet<long>();
                for (int index = 0; index < points.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AffinePointCloudInputPoint point = points[index];
                    ValidatePoint(point, locators);
                    matrix.TransformCoordinates(
                        point.SourceX,
                        point.SourceY,
                        point.SourceZ,
                        out double transformedX,
                        out double transformedY,
                        out double transformedZ);
                    if (!IsFinite(transformedX) || !IsFinite(transformedY) || !IsFinite(transformedZ))
                    {
                        return AffinePointCloudApplyResult.Failed("Full XYZ affine application produced a non-finite transformed point.");
                    }
                    output.Add(new AffinePointCloudPoint(
                        point.Row,
                        point.Column,
                        point.RawHeight,
                        transformedX,
                        transformedY,
                        transformedZ));
                }

                return AffinePointCloudApplyResult.Completed(output);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return AffinePointCloudApplyResult.Failed("Full XYZ affine point-cloud application failed: " + exception.Message);
            }
        }

        private static void ValidateMatrix(FullXyzAffineMatrix matrix)
        {
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            double[] values =
            {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34
            };
            for (int index = 0; index < values.Length; index++)
            {
                if (!IsFinite(values[index])) throw new ArgumentException("Affine matrix contains a non-finite value.");
            }

            double determinant = matrix.M11 * ((matrix.M22 * matrix.M33) - (matrix.M23 * matrix.M32))
                - matrix.M12 * ((matrix.M21 * matrix.M33) - (matrix.M23 * matrix.M31))
                + matrix.M13 * ((matrix.M21 * matrix.M32) - (matrix.M22 * matrix.M31));
            if (!IsFinite(determinant) || determinant == 0.0)
            {
                throw new ArgumentException("Affine matrix is not invertible.");
            }
        }

        private static void ValidatePoint(AffinePointCloudInputPoint point, HashSet<long> locators)
        {
            if (point.Row < 0 || point.Column < 0 || !IsFinite(point.RawHeight)
                || !IsFinite(point.SourceX) || !IsFinite(point.SourceY) || !IsFinite(point.SourceZ))
            {
                throw new ArgumentException("Affine point-cloud application requires finite points with non-negative unique locators.");
            }

            long locator = ((long)point.Row << 32) | (uint)point.Column;
            if (!locators.Add(locator))
            {
                throw new ArgumentException("Affine point-cloud application requires unique source locators.");
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
