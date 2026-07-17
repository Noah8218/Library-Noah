using Lib.ThreeD.Geometry;

namespace Lib.ThreeD.Inspection
{
    internal static class ThreeDInspectionMath
    {
        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static HeightMapRoi ResolveRoi(HeightMap3D source, HeightMapRoi? configuredRoi)
        {
            return configuredRoi ?? HeightMapRoi.Full(source);
        }
    }
}
