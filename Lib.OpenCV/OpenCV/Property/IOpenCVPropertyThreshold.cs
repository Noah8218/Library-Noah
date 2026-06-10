using Lib.OpenCV;
using OpenCvSharp;

namespace Lib.OpenCV.Property
{
    public interface IOpenCVPropertyThreshold
    {
        ThresholdToolMode Mode { get; set; }
        double Threshold { get; set; }
        double MaxValue { get; set; }
        ThresholdTypes ThresholdType { get; set; }
        int RangeMin { get; set; }
        int RangeMax { get; set; }
        AdaptiveThresholdTypes AdaptiveType { get; set; }
        ThresholdTypes AdaptiveThresholdType { get; set; }
        int BlockSize { get; set; }
        int Weight { get; set; }
    }
}
