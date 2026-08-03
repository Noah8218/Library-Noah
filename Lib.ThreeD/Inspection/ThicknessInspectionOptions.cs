using Lib.ThreeD.Geometry;

namespace Lib.ThreeD.Inspection
{
    public sealed class ThicknessInspectionOptions
    {
        public HeightMapRoi? Roi { get; set; }

        public double MinimumThickness { get; set; }

        public double MaximumThickness { get; set; }

        public int MinimumValidSamples { get; set; } = 1;

        public double MinimumValidCoverageRatio { get; set; }

        public HeightMapInputRequirements InputRequirements { get; set; }
    }
}
