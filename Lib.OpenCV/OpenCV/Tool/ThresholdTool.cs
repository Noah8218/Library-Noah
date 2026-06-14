using Lib.OpenCV;
using Lib.OpenCV.Property;
using OpenCvSharp;
using System;

namespace Lib.OpenCV.Tool
{
    public class ThresholdTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyThreshold property;

        public void SetProperty(IOpenCVPropertyThreshold property) => this.property = property;

        public override void Run()
        {
            if (property == null)
            {
                throw new InvalidOperationException("Threshold property is not configured.");
            }

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                throw new InvalidOperationException("Source image is not loaded.");
            }
            
            switch (property.Mode)
            {
                case ThresholdToolMode.Threshold:
                    RunThreshold();
                    break;
                case ThresholdToolMode.Range:
                    RunRange();
                    break;
                case ThresholdToolMode.Adaptive:
                    RunAdaptive();
                    break;
            }
        }

        private void RunThreshold()
        {
            imageResult = new Mat();
            Cv2.Threshold(imageSource, imageResult, property.Threshold, property.MaxValue, property.ThresholdType);
        }

        private void RunRange()
        {
            imageResult = new Mat();
            Scalar min = new Scalar(property.RangeMin, property.RangeMin, property.RangeMin);
            Scalar max = new Scalar(property.RangeMax, property.RangeMax, property.RangeMax);
            Cv2.InRange(imageSource, min, max, imageResult);
            if (property.Invert)
            {
                Cv2.BitwiseNot(imageResult, imageResult);
            }
        }

        private void RunAdaptive()
        {
            int blockSize = NormalizeAdaptiveBlockSize(property.BlockSize);
            using (Mat graySource = imageSource.Clone())
            {
                OpenCvHelper.SetImageChannel1(graySource);
                imageResult = new Mat();
                Cv2.AdaptiveThreshold(
                    graySource,
                    imageResult,
                    property.MaxValue,
                    property.AdaptiveType,
                    property.AdaptiveThresholdType,
                    blockSize,
                    property.Weight);
            }
        }

        private static int NormalizeAdaptiveBlockSize(int blockSize)
        {
            int normalized = Math.Max(3, blockSize);
            return normalized % 2 == 0 ? normalized + 1 : normalized;
        }
    }
}
