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
        bool USE_FIND_ANGLE { get; set; }
        double FIND_ANGLE { get; set; }
        int FIND_ANGLE_MAX { get; set; }
        int FIND_ANGLE_MIN { get; set; }
        bool USE_COARSE_TO_FINE_ANGLE_SEARCH { get; set; }
        double COARSE_ANGLE_STEP { get; set; }
        int COARSE_ANGLE_TOP_K { get; set; }
        bool USE_FIND_SCALE { get; set; }
        double FIND_SCALE_MIN { get; set; }
        double FIND_SCALE_MAX { get; set; }
        double FIND_SCALE_STEP { get; set; }
        double GREEDINESS { get; set; }
        int SEARCH_STEP { get; set; }
        bool USE_POSITION_REFINE { get; set; }
        bool USE_SUBPIXEL_REFINE { get; set; }
        bool USE_PYRAMID_POSITION_PROPOSAL { get; set; }
        int PYRAMID_POSITION_TOP_N { get; set; }
        double PYRAMID_POSITION_MIN_SCORE { get; set; }
        bool USE_HYBRID_VERIFY { get; set; }
        int HYBRID_VERIFY_TOP_N { get; set; }
        double HYBRID_VERIFY_IMAGE_WEIGHT { get; set; }
        int MAX_TEMPLATE_POINTS { get; set; }
        double MIN_GRADIENT_MAGNITUDE { get; set; }
        bool USE_DRAW_IMAGE { get; set; }
    }
}
