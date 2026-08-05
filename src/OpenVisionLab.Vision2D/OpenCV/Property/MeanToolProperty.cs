namespace OpenVisionLab.Vision2D.Property
{
    /// <summary>Provides a ready-to-use configuration for MeanTool.</summary>
    public sealed class MeanToolProperty : OpenCvToolPropertyBase, IOpenCVPropertyMean
    {
        public MeanToolProperty() : base("Mean") { }

        public int MEAN_MAX { get; set; } = 240;
        public int MEAN_MIN { get; set; } = 100;
        public MeanType MEAN_TYPES { get; set; } = MeanType.Mean;
    }
}
