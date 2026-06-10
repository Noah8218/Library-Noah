using Lib.OpenCV;
using Lib.OpenCV.Property;
using OpenCvSharp;
using System;

namespace Lib.OpenCV.Tool
{
    public class EdgeDetectionTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyEdgeDetection property;

        public void SetProperty(IOpenCVPropertyEdgeDetection property) => this.property = property;

        public override void Run()
        {
            if (property == null)
            {
                throw new InvalidOperationException("Edge detection property is not configured.");
            }

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                throw new InvalidOperationException("Source image is not loaded.");
            }

            imageResult = imageSource.Clone();

            switch (property.EdgeType)
            {
                case EdgeDetectionToolType.Canny:
                    Cv2.Canny(
                        imageResult,
                        imageResult,
                        property.CannyThresholdLow,
                        property.CannyThresholdHigh,
                        NormalizeOddKernel(property.CannyApertureSize),
                        property.UseL2Gradient);
                    break;
                case EdgeDetectionToolType.Sobel:
                    Cv2.Sobel(
                        imageResult,
                        imageResult,
                        MatType.CV_8U,
                        property.SobelDegreeX,
                        property.SobelDegreeY,
                        NormalizeSobelKernel(property.SobelKernelSize),
                        1,
                        0,
                        BorderTypes.Default);
                    break;
                case EdgeDetectionToolType.Scharr:
                    Cv2.Scharr(
                        imageResult,
                        imageResult,
                        MatType.CV_8U,
                        property.ScharrDegreeX,
                        property.ScharrDegreeY,
                        1,
                        0,
                        BorderTypes.Default);
                    break;
                case EdgeDetectionToolType.Laplacian:
                    Cv2.Laplacian(
                        imageResult,
                        imageResult,
                        MatType.CV_8U,
                        NormalizeOddKernel(property.LaplacianKernelSize),
                        1,
                        0,
                        BorderTypes.Default);
                    break;
            }
        }

        private static int NormalizeOddKernel(int kernelSize)
        {
            int normalized = Math.Max(1, kernelSize);
            return normalized % 2 == 0 ? normalized + 1 : normalized;
        }

        private static int NormalizeSobelKernel(int kernelSize)
        {
            int normalized = NormalizeOddKernel(kernelSize);
            return Math.Min(31, normalized);
        }
    }
}
