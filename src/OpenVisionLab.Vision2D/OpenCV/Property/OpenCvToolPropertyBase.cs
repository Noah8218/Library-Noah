using OpenCvSharp;
using System.Collections.Generic;

namespace OpenVisionLab.Vision2D.Property
{
    /// <summary>Provides safe whole-image defaults for tools that use the common OpenCV preprocessing contract.</summary>
    public abstract class OpenCvToolPropertyBase : IOpenCVPropertyBase
    {
        protected OpenCvToolPropertyBase(string name)
        {
            NAME = name;
        }

        public string NAME { get; set; }
        public double PIXELPERMM { get; set; } = 1d;
        public bool USE_THRESHOLD { get; set; }
        public bool USE_BITWISENOT { get; set; }
        public ThresholdTypes THRESHOLD_TYPES { get; set; } = ThresholdTypes.Binary;
        public double THRESHOLD { get; set; } = 128d;
        public bool USE_ADAPTIVE_THRESHOLD { get; set; }
        public double ADAPTIVE_THRESHOLD { get; set; } = 255d;
        public ThresholdTypes ADAPTIVE_THRESHOLD_TYPES { get; set; } = ThresholdTypes.Binary;
        public AdaptiveThresholdTypes ADAPTIVE_THRESHOLD_ALGORITHM { get; set; } = AdaptiveThresholdTypes.GaussianC;
        public int BlockSize { get; set; } = 25;
        public int Weight { get; set; } = 5;
        public bool USE_ROI { get; set; }
        public bool USE_MULTI_ROI { get; set; }
        public Rect CvROI { get; set; } = new Rect();
        public List<Rect> CvROIS { get; set; } = new List<Rect>();
        public List<Rect> CvMASKS { get; set; } = new List<Rect>();
    }
}
