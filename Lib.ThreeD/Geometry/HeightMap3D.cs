using System;

namespace Lib.ThreeD.Geometry
{
    /// <summary>
    /// Immutable regular-grid scalar map. Columns increase X, rows increase Y, and each cell stores
    /// scalar height H. NaN represents a missing sample; infinity is rejected. Units and frame are
    /// declared metadata and are not calibration evidence.
    /// </summary>
    public sealed class HeightMap3D
    {
        private readonly double[] _values;

        public HeightMap3D(
            int rows,
            int columns,
            double originX,
            double originY,
            double columnPitch,
            double rowPitch,
            double[] values,
            string unit = "model",
            string frameId = "unspecified",
            string sourceId = "")
            : this(
                rows,
                columns,
                originX,
                originY,
                columnPitch,
                rowPitch,
                values,
                NormalizeLegacyUnit(unit),
                NormalizeLegacyUnit(unit),
                NormalizeLegacyFrameId(frameId),
                sourceId ?? string.Empty)
        {
        }

        public HeightMap3D(
            int rows,
            int columns,
            double originX,
            double originY,
            double columnPitch,
            double rowPitch,
            double[] values,
            string planarUnit,
            string heightUnit,
            string frameId,
            string sourceId)
        {
            if (rows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be positive.");
            }

            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be positive.");
            }

            long expectedValueCount = (long)rows * columns;
            if (expectedValueCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), "The height map is too large for an in-memory array.");
            }

            if (!IsFinite(originX) || !IsFinite(originY))
            {
                throw new ArgumentOutOfRangeException(nameof(originX), "Origin coordinates must be finite.");
            }

            if (!IsFinite(columnPitch) || columnPitch <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnPitch), "Column pitch must be finite and positive.");
            }

            if (!IsFinite(rowPitch) || rowPitch <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowPitch), "Row pitch must be finite and positive.");
            }

            double maximumX = originX + ((columns - 1) * columnPitch);
            double maximumY = originY + ((rows - 1) * rowPitch);
            if (!IsFinite(maximumX) || !IsFinite(maximumY))
            {
                throw new ArgumentOutOfRangeException(nameof(columnPitch), "The height-map coordinate extent must remain finite.");
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length != (int)expectedValueCount)
            {
                throw new ArgumentException("The value count must match rows multiplied by columns.", nameof(values));
            }

            for (int index = 0; index < values.Length; index++)
            {
                if (double.IsInfinity(values[index]))
                {
                    throw new ArgumentException("Height-map values cannot contain infinity.", nameof(values));
                }
            }

            if (string.IsNullOrWhiteSpace(planarUnit))
            {
                throw new ArgumentException("A planar unit is required.", nameof(planarUnit));
            }

            if (string.IsNullOrWhiteSpace(heightUnit))
            {
                throw new ArgumentException("A height unit is required.", nameof(heightUnit));
            }

            if (string.IsNullOrWhiteSpace(frameId))
            {
                throw new ArgumentException("A frame ID is required.", nameof(frameId));
            }

            Rows = rows;
            Columns = columns;
            OriginX = originX;
            OriginY = originY;
            ColumnPitch = columnPitch;
            RowPitch = rowPitch;
            PlanarUnit = planarUnit.Trim();
            HeightUnit = heightUnit.Trim();
            Unit = HeightUnit;
            FrameId = frameId.Trim();
            SourceId = sourceId ?? string.Empty;
            _values = (double[])values.Clone();
        }

        public int Rows { get; }

        public int Columns { get; }

        public double OriginX { get; }

        public double OriginY { get; }

        public double ColumnPitch { get; }

        public double RowPitch { get; }

        public string PlanarUnit { get; }

        public string HeightUnit { get; }

        /// <summary>
        /// Legacy scalar-unit alias. New code should use <see cref="HeightUnit"/>.
        /// </summary>
        public string Unit { get; }

        public string FrameId { get; }

        public string SourceId { get; }

        public string CoordinateConvention => "GridXGridYScalarHeight";

        public double GetHeight(int row, int column)
        {
            ValidateIndex(row, column);
            return _values[(row * Columns) + column];
        }

        public double GetX(int column)
        {
            if (column < 0 || column >= Columns)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            return OriginX + (column * ColumnPitch);
        }

        public double GetY(int row)
        {
            if (row < 0 || row >= Rows)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            return OriginY + (row * RowPitch);
        }

        public double[] CopyValues()
        {
            return (double[])_values.Clone();
        }

        private void ValidateIndex(int row, int column)
        {
            if (row < 0 || row >= Rows)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            if (column < 0 || column >= Columns)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string NormalizeLegacyUnit(string unit)
        {
            return string.IsNullOrWhiteSpace(unit) ? "model" : unit.Trim();
        }

        private static string NormalizeLegacyFrameId(string frameId)
        {
            return string.IsNullOrWhiteSpace(frameId) ? "unspecified" : frameId.Trim();
        }
    }
}
