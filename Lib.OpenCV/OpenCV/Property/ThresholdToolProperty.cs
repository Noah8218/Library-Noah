using Lib.OpenCV;
using OpenCvSharp;

namespace Lib.OpenCV.Property
{
    public class ThresholdToolProperty : IOpenCVPropertyThreshold
    {
        public ThresholdToolMode Mode { get; set; } = ThresholdToolMode.Threshold;
        public double Threshold { get; set; } = 1;
        public double MaxValue { get; set; } = 255;
        public ThresholdTypes ThresholdType { get; set; } = ThresholdTypes.Binary;
        public int RangeMin { get; set; } = 1;
        public int RangeMax { get; set; } = 255;
        public AdaptiveThresholdTypes AdaptiveType { get; set; } = AdaptiveThresholdTypes.MeanC;
        public ThresholdTypes AdaptiveThresholdType { get; set; } = ThresholdTypes.Binary;
        public int BlockSize { get; set; } = 25;
        public int Weight { get; set; } = 5;
    }
}
