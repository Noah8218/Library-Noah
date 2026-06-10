using OpenCvSharp;

namespace Lib.OpenCV.Property
{
    public class MorphologyToolProperty : IOpenCVPropertyMorphology
    {
        public MorphShapes Shape { get; set; } = MorphShapes.Rect;
        public MorphTypes Operator { get; set; } = MorphTypes.Erode;
        public int KernelWidth { get; set; } = 3;
        public int KernelHeight { get; set; } = 3;
        public int Iterations { get; set; } = 1;
    }
}
