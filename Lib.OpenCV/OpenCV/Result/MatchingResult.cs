using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Lib.OpenCV.Result
{
    public class MatchingResult
    {
        public int Index { get; set; } = 0;
        public double Score { get; set; } = 0.0D;
        public double Angle { get; set; } = 0.0D;
        public double Scale { get; set; } = 1.0D;
        public OpenCvSharp.Point2f Center { get; set; } = new OpenCvSharp.Point2f();
        public RectangleF Bounding { get; set; } = new RectangleF();

        public MatchingResult(int nIndex, double dScore, OpenCvSharp.Point2f ptCenter, Rect2f rt, double dAngle = 0.0D, double dScale = 1.0D)
        {
            Index = nIndex;
            Score = dScore;
            Center = new OpenCvSharp.Point2f(ptCenter.X, ptCenter.Y);
            Bounding = new RectangleF(rt.X, rt.Y, rt.Width, rt.Height);
            Angle = dAngle;
            Scale = dScale <= 0D ? 1.0D : dScale;
        }
    }
}
