namespace OpenVisionLab.Vision2D.Property
{
    /// <summary>Provides a ready-to-use configuration for SiftTool.</summary>
    public sealed class SiftToolProperty : OpenCvToolPropertyBase, IOpenCVPropertyFeatureSIFT
    {
        public SiftToolProperty() : base("SIFT matching") { }

        public double RANSAC_REPROJ_THRESHOLD { get; set; } = 3d;
        public double SCORE_MIN { get; set; } = 0.6d;
        public string PATTERN_PATH { get; set; } = string.Empty;
    }
}
