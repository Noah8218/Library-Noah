using static OpenVisionLab.Core.FormulaUtil;

namespace OpenVisionLab.Vision2D.Property
{
    /// <summary>Provides a ready-to-use configuration model for LineGaugeTool; callers must teach CvROI.</summary>
    public sealed class LineGaugeToolProperty : OpenCvToolPropertyBase, IOpenCvPropertyLineGauge
    {
        public LineGaugeToolProperty() : base("Line gauge") { }

        public PROJECTION_POLARITY PRJ_PORALITY { get; set; } = PROJECTION_POLARITY.BTOW;
        public PROJECTION_DIR PRJ_DIR { get; set; } = PROJECTION_DIR.X_LTOR;
        public double CONTRAST { get; set; } = 30d;
        public double THICKNESS { get; set; } = 5d;
        public double SAMPLING_STEP { get; set; } = 10d;
        public PROJECTION_DIR VER_PRJ_DIR { get; set; } = PROJECTION_DIR.X_LTOR;
        public int POINT_RANGE { get; set; } = 10;
        public bool USE_MANUAL_ANGLE { get; set; }
        public double MANUAL_ANGLE_VALUE { get; set; }
        public bool USE_EXTEND_FIT_LINE { get; set; }
        public int EXTEND_FIT_LINE_VALUE { get; set; } = 100;
        public bool SHOW_VERTICAL_LINE { get; set; } = true;
        public bool SHOW_EDGE { get; set; } = true;
        public bool SHOW_CONTOUR { get; set; } = true;
        public bool SHOW_FITLINE { get; set; } = true;
    }
}
