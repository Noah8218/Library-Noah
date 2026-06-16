using Lib.OpenCV.Property;
using OpenCvSharp;
using System;

namespace Lib.OpenCV.Tool
{
    public class MorphologyTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyMorphology property;

        public void SetProperty(IOpenCVPropertyMorphology property) => this.property = property;

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            if (property.KernelWidth <= 0 || property.KernelHeight <= 0)
            {
                errorCode = VisionToolErrorCode.MorphologyInvalidKernel;
                message = $"Morphology kernel size must be greater than 0. Width={property.KernelWidth}, Height={property.KernelHeight}.";
                return false;
            }

            if (property.Iterations < 1)
            {
                errorCode = VisionToolErrorCode.MorphologyInvalidIterations;
                message = $"Morphology iterations must be greater than 0. Iterations={property.Iterations}.";
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
                throw new InvalidOperationException("Morphology property is not configured.");
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
            using (Mat kernel = Cv2.GetStructuringElement(property.Shape, new Size(property.KernelWidth, property.KernelHeight)))
            {
                Cv2.MorphologyEx(imageResult, imageResult, property.Operator, kernel, new Point(-1, -1), property.Iterations);
            }
        }
    }
}
