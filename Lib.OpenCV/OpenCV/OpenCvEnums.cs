namespace Lib.OpenCV
{
    public enum MeanType
    {
        Mean,
        MeanStdDev,
    }

    public enum FilterToolType
    {
        Blur,
        BoxFilter,
        MedianBlur,
        GaussianBlur,
        BilateralFilter
    }

    public enum ThresholdToolMode
    {
        Threshold,
        Range,
        Adaptive
    }

    public enum EdgeDetectionToolType
    {
        Canny,
        Sobel,
        Scharr,
        Laplacian
    }
}
