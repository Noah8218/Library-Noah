using OpenCvSharp;
namespace Lib.OpenCV.Property
{
    public interface IOpenCVPropertyMatching : IOpenCVPropertyBase
    {        
        TemplateMatchModes MATCH_MODE { get; set; }
        double SCORE_MIN { get; set; }
        double MAGNIFIATION { get; set; }
        int NUM_MATCH { get; set; } 
        bool USE_FIND_SCALE { get; set; }
        double FIND_SCALE_MIN { get; set; }
        double FIND_SCALE_MAX { get; set; }
        double FIND_SCALE_STEP { get; set; }
        bool USE_FIND_ANGLE { get; set; }
        double FIND_ANGLE { get; set; } 
        int FIND_ANGLE_MAX { get; set; } 
        int FIND_ANGLE_MIN { get; set; } 
        bool USE_COARSE_TO_FINE_ANGLE_SEARCH { get; set; }
        double COARSE_ANGLE_STEP { get; set; }
        int COARSE_ANGLE_TOP_K { get; set; }
        bool USE_PYRAMID_POSITION_PROPOSAL { get; set; }
        int PYRAMID_POSITION_TOP_N { get; set; }
        double PYRAMID_POSITION_MIN_SCORE { get; set; }
        string PATTERN_PATH { get; set; } 
        bool USE_CANNY { get; set; }         
        int CANNY_HIGH { get; set; } 
        int CANNY_LOW { get; set; }         
        bool USE_PADDING_COLOR_WHITE { get; set; }         
    }
}
