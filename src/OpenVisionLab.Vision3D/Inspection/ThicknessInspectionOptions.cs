using OpenVisionLab.Vision3D.Geometry;

namespace OpenVisionLab.Vision3D.Inspection
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
