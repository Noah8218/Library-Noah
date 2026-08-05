using OpenVisionLab.Vision3D.Geometry;

namespace OpenVisionLab.Vision3D.Inspection
{
    public interface IThreeDInspectionTool
    {
        string Name { get; }

        ThreeDInspectionResult Execute(HeightMap3D source);
    }
}
