using System;

namespace OpenVisionLab.Vision3D.Geometry
{
    /// <summary>
    /// Inclusive start, exclusive extent ROI in height-map row and column coordinates.
    /// Invalid values are intentionally representable so tools can return controlled errors.
    /// </summary>
    public struct HeightMapRoi
    {
        public HeightMapRoi(int row, int column, int rowCount, int columnCount)
        {
            Row = row;
            Column = column;
            RowCount = rowCount;
            ColumnCount = columnCount;
        }

        public int Row { get; set; }

        public int Column { get; set; }

        public int RowCount { get; set; }

        public int ColumnCount { get; set; }

        public bool IsValidFor(HeightMap3D source)
        {
            if (source == null || Row < 0 || Column < 0 || RowCount <= 0 || ColumnCount <= 0)
            {
                return false;
            }

            return (long)Row + RowCount <= source.Rows
                && (long)Column + ColumnCount <= source.Columns;
        }

        public static HeightMapRoi Full(HeightMap3D source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new HeightMapRoi(0, 0, source.Rows, source.Columns);
        }
    }
}
