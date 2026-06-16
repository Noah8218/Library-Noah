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

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            switch (property.EdgeType)
            {
                case EdgeDetectionToolType.Canny:
                    if (property.CannyThresholdLow < 0
                        || property.CannyThresholdHigh < 0
                        || property.CannyThresholdLow > property.CannyThresholdHigh)
                    {
                        errorCode = VisionToolErrorCode.EdgeDetectionInvalidThreshold;
                        message = $"Canny thresholds must satisfy 0 <= Low <= High. Low={property.CannyThresholdLow}, High={property.CannyThresholdHigh}.";
                        return false;
                    }

                    if (!IsCannyApertureSize(property.CannyApertureSize))
                    {
                        errorCode = VisionToolErrorCode.EdgeDetectionInvalidKernel;
                        message = $"Canny aperture size must be 3, 5, or 7. Aperture={property.CannyApertureSize}.";
                        return false;
                    }
                    break;
                case EdgeDetectionToolType.Sobel:
                    if (!IsValidDerivative(property.SobelDegreeX, property.SobelDegreeY))
                    {
                        errorCode = VisionToolErrorCode.EdgeDetectionInvalidDerivative;
                        message = $"Sobel derivative must be non-negative and not both zero. X={property.SobelDegreeX}, Y={property.SobelDegreeY}.";
                        return false;
                    }

                    if (!IsOddKernelInRange(property.SobelKernelSize, 1, 31))
                    {
                        errorCode = VisionToolErrorCode.EdgeDetectionInvalidKernel;
                        message = $"Sobel kernel size must be an odd number from 1 to 31. Kernel={property.SobelKernelSize}.";
                        return false;
                    }
                    break;
                case EdgeDetectionToolType.Scharr:
                    if (!IsValidScharrDerivative(property.ScharrDegreeX, property.ScharrDegreeY))
                    {
                        errorCode = VisionToolErrorCode.EdgeDetectionInvalidDerivative;
                        message = $"Scharr derivative must be (1,0) or (0,1). X={property.ScharrDegreeX}, Y={property.ScharrDegreeY}.";
                        return false;
                    }
                    break;
                case EdgeDetectionToolType.Laplacian:
                    if (!IsOddKernelInRange(property.LaplacianKernelSize, 1, 31))
                    {
                        errorCode = VisionToolErrorCode.EdgeDetectionInvalidKernel;
                        message = $"Laplacian kernel size must be an odd number from 1 to 31. Kernel={property.LaplacianKernelSize}.";
                        return false;
                    }
                    break;
                default:
                    errorCode = VisionToolErrorCode.InvalidParameter;
                    message = $"Unsupported edge detection type: {property.EdgeType}.";
                    return false;
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

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

            if (!TryValidateBeforeRun(out _, out string validationMessage))
            {
                throw new InvalidOperationException(validationMessage);
            }

            ReplaceResultImage(imageSource.Clone());

            switch (property.EdgeType)
            {
                case EdgeDetectionToolType.Canny:
                    Cv2.Canny(
                        imageResult,
                        imageResult,
                        property.CannyThresholdLow,
                        property.CannyThresholdHigh,
                        property.CannyApertureSize,
                        property.UseL2Gradient);
                    break;
                case EdgeDetectionToolType.Sobel:
                    Cv2.Sobel(
                        imageResult,
                        imageResult,
                        MatType.CV_8U,
                        property.SobelDegreeX,
                        property.SobelDegreeY,
                        property.SobelKernelSize,
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
                        property.LaplacianKernelSize,
                        1,
                        0,
                        BorderTypes.Default);
                    break;
            }
        }

        private static bool IsCannyApertureSize(int kernelSize)
        {
            return kernelSize == 3 || kernelSize == 5 || kernelSize == 7;
        }

        private static bool IsOddKernelInRange(int kernelSize, int min, int max)
        {
            return kernelSize >= min && kernelSize <= max && kernelSize % 2 == 1;
        }

        private static bool IsValidDerivative(int degreeX, int degreeY)
        {
            return degreeX >= 0 && degreeY >= 0 && degreeX + degreeY > 0;
        }

        private static bool IsValidScharrDerivative(int degreeX, int degreeY)
        {
            return (degreeX == 1 && degreeY == 0)
                || (degreeX == 0 && degreeY == 1);
        }
    }
}
