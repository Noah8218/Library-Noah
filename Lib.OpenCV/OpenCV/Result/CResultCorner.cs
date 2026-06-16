using OpenCvSharp;
using System.Drawing;

namespace Lib.OpenCV.Result
{
    [System.Obsolete("Legacy compatibility result. Use CornerResult for new OpenVisionLab code.", false)]
    public class CResultCorner
    {
        public double Area { get; set; } = 0.0D;
        public double Angle { get; set; } = 0.0D;
        public OpenCvSharp.Point2d Center { get; set; } = new OpenCvSharp.Point2d();
        public Rectangle Bounding { get; set; } = new Rectangle();

        public CResultCorner(double Area, OpenCvSharp.Point2d Center, Rect rt, double dAngle = 0.0D)
        {
            this.Area = Area;
            this.Center = new OpenCvSharp.Point2d(Center.X, Center.Y);
            Bounding = new Rectangle(rt.X, rt.Y, rt.Width, rt.Height);
            Angle = dAngle;
        }
    }
}
