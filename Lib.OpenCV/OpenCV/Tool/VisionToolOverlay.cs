using System.Collections.Generic;
using System.Drawing;

namespace Lib.OpenCV.Tool
{
    public enum VisionToolOverlayKind
    {
        Rectangle,
        Point,
        Points,
        Line
    }

    public sealed class VisionToolOverlay
    {
        public VisionToolOverlayKind Kind { get; set; }
        public string Label { get; set; } = string.Empty;
        public RectangleF Bounds { get; set; }
        public PointF Center { get; set; }
        public PointF Start { get; set; }
        public PointF End { get; set; }
        public double Angle { get; set; }
        public List<PointF> Points { get; } = new List<PointF>();
    }
}
