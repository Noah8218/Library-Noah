using OpenCvSharp;

namespace Lib.OpenCV.Tool
{
    public interface IVisionTool
    {
        string Name { get; }
        VisionToolResult Execute(Mat source);
    }
}
