using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Property
{
    public enum AutoMPointCandidateMode
    {
        Grid,
        WholeAnalysisRoi,
        GridAndWhole
    }

    public interface IAutoMPointToolProperty
    {
        bool UseAnalysisRoi { get; set; }
        Rect AnalysisRoi { get; set; }
        AutoMPointCandidateMode CandidateMode { get; set; }
        int PatternWidth { get; set; }
        int PatternHeight { get; set; }
        int CandidateStride { get; set; }
        int MaximumFinalists { get; set; }
        int MaximumResults { get; set; }
        double MaximumCandidateOverlap { get; set; }

        double MinimumContrastStdDev { get; set; }
        double MinimumEdgeDensity { get; set; }
        double MinimumQuadrantBalance { get; set; }
        double MinimumOrientationBalance { get; set; }
        double MinimumFeatureQuality { get; set; }

        int CannyLow { get; set; }
        int CannyHigh { get; set; }
        double MatchingMinimumScore { get; set; }
        double MinimumUniquenessMargin { get; set; }
        int MaximumTemplatePoints { get; set; }
        int SearchStep { get; set; }
        bool UsePositionRefine { get; set; }
        bool UseSubpixelRefine { get; set; }
        bool UsePyramidPositionProposal { get; set; }
        bool UseHybridVerify { get; set; }

        bool UseAngleSearch { get; set; }
        int AngleMinimum { get; set; }
        int AngleMaximum { get; set; }
        double AngleStep { get; set; }
        bool UseScaleSearch { get; set; }
        double ScaleMinimum { get; set; }
        double ScaleMaximum { get; set; }
        double ScaleStep { get; set; }

        int SyntheticTranslationPixels { get; set; }
        double SyntheticRotationDegrees { get; set; }
        double SyntheticScaleRatio { get; set; }
        double MinimumSyntheticSuccessRate { get; set; }
        double MaximumPositionErrorPixels { get; set; }
        double MaximumAngleErrorDegrees { get; set; }
        double MaximumScaleErrorRatio { get; set; }
        double MaximumRuntimeMilliseconds { get; set; }
        int MinimumRepresentativeImageCount { get; set; }
        double MinimumRepresentativeSuccessRate { get; set; }
    }

    public sealed class AutoMPointToolProperty : IAutoMPointToolProperty
    {
        public bool UseAnalysisRoi { get; set; }
        public Rect AnalysisRoi { get; set; }
        public AutoMPointCandidateMode CandidateMode { get; set; } = AutoMPointCandidateMode.Grid;
        public int PatternWidth { get; set; } = 96;
        public int PatternHeight { get; set; } = 96;
        public int CandidateStride { get; set; } = 16;
        public int MaximumFinalists { get; set; } = 8;
        public int MaximumResults { get; set; } = 5;
        public double MaximumCandidateOverlap { get; set; } = 0.1d;

        public double MinimumContrastStdDev { get; set; } = 8d;
        public double MinimumEdgeDensity { get; set; } = 0.01d;
        public double MinimumQuadrantBalance { get; set; } = 0.03d;
        public double MinimumOrientationBalance { get; set; } = 0.08d;
        public double MinimumFeatureQuality { get; set; } = 0.15d;

        public int CannyLow { get; set; } = 30;
        public int CannyHigh { get; set; } = 90;
        public double MatchingMinimumScore { get; set; } = 0.55d;
        public double MinimumUniquenessMargin { get; set; } = 0.05d;
        public int MaximumTemplatePoints { get; set; } = 300;
        public int SearchStep { get; set; } = 2;
        public bool UsePositionRefine { get; set; } = true;
        public bool UseSubpixelRefine { get; set; } = true;
        public bool UsePyramidPositionProposal { get; set; } = true;
        public bool UseHybridVerify { get; set; } = true;

        public bool UseAngleSearch { get; set; }
        public int AngleMinimum { get; set; } = -8;
        public int AngleMaximum { get; set; } = 8;
        public double AngleStep { get; set; } = 1d;
        public bool UseScaleSearch { get; set; }
        public double ScaleMinimum { get; set; } = 0.9d;
        public double ScaleMaximum { get; set; } = 1.1d;
        public double ScaleStep { get; set; } = 0.05d;

        public int SyntheticTranslationPixels { get; set; } = 4;
        public double SyntheticRotationDegrees { get; set; } = 2d;
        public double SyntheticScaleRatio { get; set; } = 1.02d;
        public double MinimumSyntheticSuccessRate { get; set; } = 1d;
        public double MaximumPositionErrorPixels { get; set; } = 2.5d;
        public double MaximumAngleErrorDegrees { get; set; } = 1.5d;
        public double MaximumScaleErrorRatio { get; set; } = 0.03d;
        public double MaximumRuntimeMilliseconds { get; set; }
        public int MinimumRepresentativeImageCount { get; set; } = 3;
        public double MinimumRepresentativeSuccessRate { get; set; } = 0.75d;
    }
}
