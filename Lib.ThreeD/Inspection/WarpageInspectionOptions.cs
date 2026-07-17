using Lib.ThreeD.Geometry;

namespace Lib.ThreeD.Inspection
{
    public sealed class WarpageInspectionOptions
    {
        public HeightMapRoi? Roi { get; set; }

        public double MaximumPeakToValley { get; set; }

        public double? MaximumRms { get; set; }

        public int MinimumValidSamples { get; set; } = 3;
    }
}
