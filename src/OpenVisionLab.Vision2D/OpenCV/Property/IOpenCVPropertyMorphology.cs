using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Property
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
