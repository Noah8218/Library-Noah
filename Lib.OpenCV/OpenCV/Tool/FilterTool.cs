using Lib.OpenCV;
using Lib.OpenCV.Property;
using OpenCvSharp;
using System;

namespace Lib.OpenCV.Tool
{
    public class FilterTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyFilter property;

        public void SetProperty(IOpenCVPropertyFilter property) => this.property = property;

        public override void Run()
        {
            if (property == null)
            {
                throw new InvalidOperationException("Filter property is not configured.");
            }

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                throw new InvalidOperationException("Source image is not loaded.");
            }

            int kernelWidth = Math.Max(1, property.KernelWidth);
            int kernelHeight = Math.Max(1, property.KernelHeight);
            int medianKernelSize = Math.Max(1, property.MedianKernelSize);
            int diameter = Math.Max(1, property.Diameter);

            imageResult = imageSource.Clone();

            switch (property.FilterType)
            {
                case FilterToolType.Blur:
                    Cv2.Blur(imageResult, imageResult, new Size(kernelWidth, kernelHeight), new Point(-1, -1), property.BorderType);
                    break;
                case FilterToolType.GaussianBlur:
                    Cv2.GaussianBlur(imageResult, imageResult, new Size(kernelWidth, kernelHeight), 1, 1, property.BorderType);
                    break;
                case FilterToolType.MedianBlur:
                    Cv2.MedianBlur(imageResult, imageResult, medianKernelSize);
                    break;
                case FilterToolType.BoxFilter:
                    Cv2.BoxFilter(imageResult, imageResult, MatType.CV_8UC3, new Size(kernelWidth, kernelHeight), new Point(-1, -1));
                    break;
                case FilterToolType.BilateralFilter:
                    Cv2.BilateralFilter(imageResult, imageResult, diameter, property.SigmaColor, property.SigmaSpace, property.BorderType);
                    break;
            }
        }
    }
}
