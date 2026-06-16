using OpenCvSharp;

namespace Lib.OpenCV.Property
{
    public interface IOpenCVPropertyRotateScale
    {
        double Angle { get; set; }
        double ScaleXPercent { get; set; }
        double ScaleYPercent { get; set; }
        InterpolationFlags Interpolation { get; set; }
        BorderTypes BorderType { get; set; }
    }

    public class RotateScaleToolProperty : IOpenCVPropertyRotateScale
    {
        public double Angle { get; set; }
        public double ScaleXPercent { get; set; } = 100d;
        public double ScaleYPercent { get; set; } = 100d;
        public InterpolationFlags Interpolation { get; set; } = InterpolationFlags.Linear;
        public BorderTypes BorderType { get; set; } = BorderTypes.Constant;
    }
}
