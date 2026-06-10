using Lib.OpenCV.Property;
using OpenCvSharp;
using System;

namespace Lib.OpenCV.Tool
{
    public class MorphologyTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyMorphology property;

        public void SetProperty(IOpenCVPropertyMorphology property) => this.property = property;

        public override void Run()
        {
            if (property == null)
            {
                throw new InvalidOperationException("Morphology property is not configured.");
            }

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                throw new InvalidOperationException("Source image is not loaded.");
            }

            int kernelWidth = Math.Max(1, property.KernelWidth);
            int kernelHeight = Math.Max(1, property.KernelHeight);
            int iterations = Math.Max(1, property.Iterations);

            imageResult = imageSource.Clone();
            using (Mat kernel = Cv2.GetStructuringElement(property.Shape, new Size(kernelWidth, kernelHeight)))
            {
                Cv2.MorphologyEx(imageResult, imageResult, property.Operator, kernel, new Point(-1, -1), iterations);
            }
        }
    }
}
