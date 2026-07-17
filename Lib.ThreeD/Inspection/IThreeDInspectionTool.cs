using Lib.ThreeD.Geometry;

namespace Lib.ThreeD.Inspection
{
    public interface IThreeDInspectionTool
    {
        string Name { get; }

        ThreeDInspectionResult Execute(HeightMap3D source);
    }
}
