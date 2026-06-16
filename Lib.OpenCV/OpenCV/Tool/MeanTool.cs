using OpenCvSharp;
using System;
using System.Collections.Generic;
using Lib.OpenCV.Property;
using Lib.OpenCV.Result;
using Lib.Common;
using System.Reflection;

namespace Lib.OpenCV.Tool
{
    public class MeanTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyMean property;
        public List<MeanResult> results = new List<MeanResult>();

        public MeanTool() { }        
        
        public void SetProperty(IOpenCVPropertyMean property) => this.property = property;

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            if (!TryValidateAdaptiveThreshold(
                property,
                VisionToolErrorCode.MeanInvalidAdaptiveBlockSize,
                out errorCode,
                out message))
            {
                return false;
            }

            if (!TryValidateRoiSet(
                property,
                property.USE_ROI,
                true,
                VisionToolErrorCode.MeanRoiInvalid,
                "Mean",
                out errorCode,
                out message))
            {
                return false;
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

        public override void Run()
        {
            if(property.USE_MULTI_ROI)
            {
                MultiRun();
            }
            else
            {
                SingleRun();
            }
        }

        public void MultiRun()
        {
            results.Clear();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                return;
            }

            for (int i = 0; i < property.CvROIS.Count; i++)
            {
                Rect roi = NormalizeMeanRoi(property.CvROIS[i]);
                using (Mat imageMean = CreatePreprocessedImage(roi, property.USE_ROI, property))
                {
                    AddMeanResult(imageMean, roi);
                }
            }
        }

        public void SingleRun()
        {
            results.Clear();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                return;
            }

            Rect roi = NormalizeMeanRoi(property.CvROI);
            using (Mat imageMean = CreatePreprocessedImage(roi, property.USE_ROI, property))
            {
                AddMeanResult(imageMean, roi);
            }
        }

        private Rect NormalizeMeanRoi(Rect roi)
        {
            return roi.Width == 0 || roi.Height == 0
                ? new Rect(0, 0, imageSource.Width, imageSource.Height)
                : roi;
        }

        private void AddMeanResult(Mat imageMean, OpenCvSharp.Rect resultBounds)
        {
            switch (property.MEAN_TYPES)
            {
                case MeanType.Mean:
                    double meanValue = Math.Round(Cv2.Mean(imageMean).Val0, 1);
                    results.Add(new MeanResult(0, meanValue, Lib.Common.CommonConverter.RectToRectangle(resultBounds)));
                    break;
                case MeanType.MeanStdDev:
                    Cv2.MeanStdDev(imageMean, out Scalar mean, out Scalar stddev);
                    double meanStdDev = double.Parse(stddev[0].ToString("F1"));
                    results.Add(new MeanResult(0, meanStdDev, Lib.Common.CommonConverter.RectToRectangle(resultBounds)));
                    break;
            }
        }
    }
}
