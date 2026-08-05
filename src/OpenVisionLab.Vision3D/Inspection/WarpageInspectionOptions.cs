using OpenVisionLab.Vision3D.Geometry;

namespace OpenVisionLab.Vision3D.Inspection
{
    public sealed class WarpageInspectionOptions
    {
        public HeightMapRoi? Roi { get; set; }

        public double MaximumPeakToValley { get; set; }

        public double? MaximumRms { get; set; }

        public int MinimumValidSamples { get; set; } = 3;

        public double MinimumValidCoverageRatio { get; set; }

        public HeightMapInputRequirements InputRequirements { get; set; }
    }
}
