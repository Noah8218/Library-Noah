using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Property
{
    public interface IAffineTransformToolProperty
    {
        double SourcePoint1X { get; set; }
        double SourcePoint1Y { get; set; }
        double SourcePoint2X { get; set; }
        double SourcePoint2Y { get; set; }
        double SourcePoint3X { get; set; }
        double SourcePoint3Y { get; set; }

        double DestinationPoint1X { get; set; }
        double DestinationPoint1Y { get; set; }
        double DestinationPoint2X { get; set; }
        double DestinationPoint2Y { get; set; }
        double DestinationPoint3X { get; set; }
        double DestinationPoint3Y { get; set; }

        int OutputWidth { get; set; }
        int OutputHeight { get; set; }
        InterpolationFlags Interpolation { get; set; }
        BorderTypes BorderType { get; set; }
        double BorderValue { get; set; }
        double MinimumSourceTriangleArea { get; set; }
        double MinimumDestinationTriangleArea { get; set; }
        double MinimumValidPixelRatio { get; set; }
    }

    public class AffineTransformToolProperty : IAffineTransformToolProperty
    {
        public double SourcePoint1X { get; set; }
        public double SourcePoint1Y { get; set; }
        public double SourcePoint2X { get; set; } = 100d;
        public double SourcePoint2Y { get; set; }
        public double SourcePoint3X { get; set; }
        public double SourcePoint3Y { get; set; } = 100d;

        public double DestinationPoint1X { get; set; }
        public double DestinationPoint1Y { get; set; }
        public double DestinationPoint2X { get; set; } = 100d;
        public double DestinationPoint2Y { get; set; }
        public double DestinationPoint3X { get; set; }
        public double DestinationPoint3Y { get; set; } = 100d;

        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }
        public InterpolationFlags Interpolation { get; set; } = InterpolationFlags.Linear;
        public BorderTypes BorderType { get; set; } = BorderTypes.Constant;
        public double BorderValue { get; set; }
        public double MinimumSourceTriangleArea { get; set; } = 1d;
        public double MinimumDestinationTriangleArea { get; set; } = 1d;
        public double MinimumValidPixelRatio { get; set; }
    }
}
