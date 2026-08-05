using OpenVisionLab.Vision2D;
using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Property
{
    public interface IOpenCVPropertyFilter
    {
        FilterToolType FilterType { get; set; }
        int KernelWidth { get; set; }
        int KernelHeight { get; set; }
        int MedianKernelSize { get; set; }
        int Diameter { get; set; }
        int SigmaColor { get; set; }
        int SigmaSpace { get; set; }
        BorderTypes BorderType { get; set; }
    }
}
