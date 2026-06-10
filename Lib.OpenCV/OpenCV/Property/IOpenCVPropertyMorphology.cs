using OpenCvSharp;

namespace Lib.OpenCV.Property
{
    public interface IOpenCVPropertyMorphology
    {
        MorphShapes Shape { get; set; }
        MorphTypes Operator { get; set; }
        int KernelWidth { get; set; }
        int KernelHeight { get; set; }
        int Iterations { get; set; }
    }
}
