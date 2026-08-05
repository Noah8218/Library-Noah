using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Property
{
    /// <summary>Provides a ready-to-use configuration for MatchingTool.</summary>
    public sealed class MatchingToolProperty : OpenCvToolPropertyBase, IOpenCVPropertyMatching
    {
        public MatchingToolProperty() : base("Matching") { }

        public TemplateMatchModes MATCH_MODE { get; set; } = TemplateMatchModes.CCoeffNormed;
        public double SCORE_MIN { get; set; } = 0.6d;
        public double MAGNIFIATION { get; set; } = 1d;
        public int NUM_MATCH { get; set; } = 3;
        public bool USE_FIND_SCALE { get; set; }
        public double FIND_SCALE_MIN { get; set; } = 0.9d;
        public double FIND_SCALE_MAX { get; set; } = 1.1d;
        public double FIND_SCALE_STEP { get; set; } = 0.05d;
        public bool USE_FIND_ANGLE { get; set; } = true;
        public double FIND_ANGLE { get; set; } = 0.1d;
        public int FIND_ANGLE_MAX { get; set; } = 10;
        public int FIND_ANGLE_MIN { get; set; } = -10;
        public bool USE_COARSE_TO_FINE_ANGLE_SEARCH { get; set; }
        public double COARSE_ANGLE_STEP { get; set; } = 5d;
        public int COARSE_ANGLE_TOP_K { get; set; } = 3;
        public bool USE_PYRAMID_POSITION_PROPOSAL { get; set; }
        public int PYRAMID_POSITION_TOP_N { get; set; } = 8;
        public double PYRAMID_POSITION_MIN_SCORE { get; set; } = 0.7d;
        public string PATTERN_PATH { get; set; } = string.Empty;
        public bool USE_CANNY { get; set; }
        public int CANNY_HIGH { get; set; } = 60;
        public int CANNY_LOW { get; set; } = 30;
        public bool USE_PADDING_COLOR_WHITE { get; set; }
    }
}
