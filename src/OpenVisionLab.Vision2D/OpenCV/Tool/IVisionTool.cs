using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Tool
{
    public interface IVisionTool
    {
        string Name { get; }
        VisionToolResult Execute(Mat source);
    }
}
