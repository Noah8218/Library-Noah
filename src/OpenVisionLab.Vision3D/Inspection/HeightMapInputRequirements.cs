using System;

namespace OpenVisionLab.Vision3D.Inspection
{
    /// <summary>
    /// Exact declared input identity required by a height-map inspection.
    /// No unit conversion, alias matching, or frame transformation is implied.
    /// </summary>
    public sealed class HeightMapInputRequirements
    {
        public HeightMapInputRequirements(string planarUnit, string heightUnit, string frameId)
        {
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

            PlanarUnit = planarUnit.Trim();
            HeightUnit = heightUnit.Trim();
            FrameId = frameId.Trim();
        }

        public string PlanarUnit { get; }

        public string HeightUnit { get; }

        public string FrameId { get; }
    }
}
