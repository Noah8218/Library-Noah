using Lib.OpenCV;

namespace Lib.OpenCV.Property
{
    public class EdgeDetectionToolProperty : IOpenCVPropertyEdgeDetection
    {
        public EdgeDetectionToolType EdgeType { get; set; } = EdgeDetectionToolType.Canny;
        public int CannyThresholdLow { get; set; } = 100;
        public int CannyThresholdHigh { get; set; } = 200;
        public int CannyApertureSize { get; set; } = 3;
        public bool UseL2Gradient { get; set; } = true;
        public int SobelDegreeX { get; set; }
        public int SobelDegreeY { get; set; }
        public int SobelKernelSize { get; set; } = 1;
        public int ScharrDegreeX { get; set; }
        public int ScharrDegreeY { get; set; }
        public int LaplacianKernelSize { get; set; } = 1;
    }
}
