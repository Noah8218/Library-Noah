using OpenVisionLab.Vision2D;
using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Property
{
    public interface IOpenCVPropertyThreshold
    {
        ThresholdToolMode Mode { get; set; }
        double Threshold { get; set; }
        double MaxValue { get; set; }
        ThresholdTypes ThresholdType { get; set; }
        int RangeMin { get; set; }
        int RangeMax { get; set; }
        bool Invert { get; set; }
        AdaptiveThresholdTypes AdaptiveType { get; set; }
        ThresholdTypes AdaptiveThresholdType { get; set; }
        int BlockSize { get; set; }
        int Weight { get; set; }
    }
}
