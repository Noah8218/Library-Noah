using Lib.Common;
using System.Drawing;

namespace Lib.OpenCV.Result
{
    [System.Obsolete("Legacy compatibility result. Use MeanResult for new OpenVisionLab code.", false)]
    public class CResultMean
    {
        public int index { get; set; } = 0;
        public double meanValue { get; set; } = 0.0D;
        public Rectangle Bounding { get; set; } = new Rectangle();
        public PointF Center { get; set; } = new PointF();

        public CResultMean(int index, double Mean, Rectangle Bounding)
        {
            this.index = index;
            this.meanValue = Mean;
            this.Bounding = Bounding;
            this.Center = CConverter.Center(Bounding);
        }
    }
}
