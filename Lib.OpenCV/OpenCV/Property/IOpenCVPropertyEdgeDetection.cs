using Lib.OpenCV;

namespace Lib.OpenCV.Property
{
    public interface IOpenCVPropertyEdgeDetection
    {
        EdgeDetectionToolType EdgeType { get; set; }
        int CannyThresholdLow { get; set; }
        int CannyThresholdHigh { get; set; }
        int CannyApertureSize { get; set; }
        bool UseL2Gradient { get; set; }
        int SobelDegreeX { get; set; }
        int SobelDegreeY { get; set; }
        int SobelKernelSize { get; set; }
        int ScharrDegreeX { get; set; }
        int ScharrDegreeY { get; set; }
        int LaplacianKernelSize { get; set; }
    }
}
