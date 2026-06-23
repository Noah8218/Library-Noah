using OpenCvSharp;

namespace Lib.OpenCV.Property
{
    public interface IOpenCVPropertyEdgeBasedTemplateMatching : IOpenCVPropertyBase
    {
        double SCORE_MIN { get; set; }
        int NUM_MATCH { get; set; }
        string PATTERN_PATH { get; set; }
        int CANNY_LOW { get; set; }
        int CANNY_HIGH { get; set; }
        int CANNY_APERTURE_SIZE { get; set; }
        bool USE_L2_GRADIENT { get; set; }
        RetrievalModes CONTOUR_RETRIEVAL_MODE { get; set; }
        ContourApproximationModes CONTOUR_APPROXIMATION_MODE { get; set; }
        double GREEDINESS { get; set; }
        int SEARCH_STEP { get; set; }
        int MAX_TEMPLATE_POINTS { get; set; }
        double MIN_GRADIENT_MAGNITUDE { get; set; }
        bool USE_DRAW_IMAGE { get; set; }
    }
}
