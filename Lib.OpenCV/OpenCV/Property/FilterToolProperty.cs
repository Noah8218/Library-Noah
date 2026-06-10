using Lib.OpenCV;
using OpenCvSharp;

namespace Lib.OpenCV.Property
{
    public class FilterToolProperty : IOpenCVPropertyFilter
    {
        public FilterToolType FilterType { get; set; } = FilterToolType.Blur;
        public int KernelWidth { get; set; } = 3;
        public int KernelHeight { get; set; } = 3;
        public int MedianKernelSize { get; set; } = 3;
        public int Diameter { get; set; } = 3;
        public int SigmaColor { get; set; } = 3;
        public int SigmaSpace { get; set; } = 3;
        public BorderTypes BorderType { get; set; } = BorderTypes.Reflect101;
    }
}
