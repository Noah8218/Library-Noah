using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Property
{
    /// <summary>Provides a ready-to-use configuration for EdgeBasedTemplateMatchingTool.</summary>
    public sealed class EdgeBasedTemplateMatchingToolProperty : OpenCvToolPropertyBase, IOpenCVPropertyEdgeBasedTemplateMatching
    {
        public EdgeBasedTemplateMatchingToolProperty() : base("Edge based matching") { }

        public double SCORE_MIN { get; set; } = 0.75d;
        public int NUM_MATCH { get; set; } = 1;
        public bool USE_UNIQUE_MATCH_VALIDATION { get; set; }
        public double UNIQUE_MATCH_MIN_SCORE_MARGIN { get; set; } = 0.03d;
        public bool ALLOW_GLOBAL_POLARITY_REVERSAL { get; set; }
        public string PATTERN_PATH { get; set; } = string.Empty;
        public int CANNY_LOW { get; set; } = 30;
        public int CANNY_HIGH { get; set; } = 90;
        public int CANNY_APERTURE_SIZE { get; set; } = 3;
        public bool USE_L2_GRADIENT { get; set; } = true;
        public RetrievalModes CONTOUR_RETRIEVAL_MODE { get; set; } = RetrievalModes.External;
        public ContourApproximationModes CONTOUR_APPROXIMATION_MODE { get; set; } = ContourApproximationModes.ApproxNone;
        public bool USE_FIND_ANGLE { get; set; }
        public double FIND_ANGLE { get; set; } = 1d;
        public int FIND_ANGLE_MAX { get; set; } = 10;
        public int FIND_ANGLE_MIN { get; set; } = -10;
        public bool USE_COARSE_TO_FINE_ANGLE_SEARCH { get; set; }
        public double COARSE_ANGLE_STEP { get; set; } = 5d;
        public int COARSE_ANGLE_TOP_K { get; set; } = 3;
        public bool USE_FIND_SCALE { get; set; }
        public double FIND_SCALE_MIN { get; set; } = 0.9d;
        public double FIND_SCALE_MAX { get; set; } = 1.1d;
        public double FIND_SCALE_STEP { get; set; } = 0.05d;
        public double GREEDINESS { get; set; } = 0.9d;
        public int SEARCH_STEP { get; set; } = 2;
        public bool USE_POSITION_REFINE { get; set; }
        public bool USE_SUBPIXEL_REFINE { get; set; }
        public bool USE_PYRAMID_POSITION_PROPOSAL { get; set; }
        public int PYRAMID_POSITION_TOP_N { get; set; } = 6;
        public double PYRAMID_POSITION_MIN_SCORE { get; set; } = 0.7d;
        public bool USE_HYBRID_VERIFY { get; set; }
        public int HYBRID_VERIFY_TOP_N { get; set; } = 5;
        public double HYBRID_VERIFY_IMAGE_WEIGHT { get; set; } = 0.35d;
        public int MAX_TEMPLATE_POINTS { get; set; } = 300;
        public double MIN_GRADIENT_MAGNITUDE { get; set; } = 1d;
        public bool USE_DRAW_IMAGE { get; set; } = true;
    }
}
