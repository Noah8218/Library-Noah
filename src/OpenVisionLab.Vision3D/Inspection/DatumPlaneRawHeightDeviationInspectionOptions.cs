using OpenVisionLab.Vision3D.Geometry;

namespace OpenVisionLab.Vision3D.Inspection
{
    /// <summary>
    /// Explicit raw-height datum-plane contract. Plane coefficients describe
    /// n.x * grid-X + n.y * raw-height + n.z * grid-Y + d = 0.
    /// They are source-coordinate values, not calibration evidence.
    /// </summary>
    public sealed class DatumPlaneRawHeightDeviationInspectionOptions
    {
        public HeightMapRoi? Roi { get; set; }

        public double PlaneNormalX { get; set; }

        public double PlaneNormalY { get; set; }

        public double PlaneNormalZ { get; set; }

        public double PlaneOffset { get; set; }

        public double MaximumPeakToValleyRawHeight { get; set; }

        public int MinimumValidSamples { get; set; } = 3;

        public double MinimumAbsoluteNormalY { get; set; } = 0.1;

        public double MinimumValidCoverageRatio { get; set; }

        public HeightMapInputRequirements InputRequirements { get; set; }
    }
}
