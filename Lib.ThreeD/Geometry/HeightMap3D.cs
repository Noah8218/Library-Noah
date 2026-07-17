using System;

namespace Lib.ThreeD.Geometry
{
    /// <summary>
    /// Immutable regular-grid scalar map. NaN represents a missing sample; infinity is rejected.
    /// Unit and frame are declared metadata and are not calibration evidence.
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

            Rows = rows;
            Columns = columns;
            OriginX = originX;
            OriginY = originY;
            ColumnPitch = columnPitch;
            RowPitch = rowPitch;
            Unit = string.IsNullOrWhiteSpace(unit) ? "model" : unit.Trim();
            FrameId = string.IsNullOrWhiteSpace(frameId) ? "unspecified" : frameId.Trim();
            SourceId = sourceId ?? string.Empty;
            _values = (double[])values.Clone();
        }

        public int Rows { get; }

        public int Columns { get; }

        public double OriginX { get; }

        public double OriginY { get; }

        public double ColumnPitch { get; }

        public double RowPitch { get; }

        public string Unit { get; }

        public string FrameId { get; }

        public string SourceId { get; }

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
    }
}
