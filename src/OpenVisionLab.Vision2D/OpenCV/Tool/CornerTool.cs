using System;
using System.Collections.Generic;
using OpenVisionLab.Core;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Result;
using OpenCvSharp;

namespace OpenVisionLab.Vision2D.Tool
{
    public partial class CornerTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyContour property;

        public List<CornerResult> results = new List<CornerResult>();        
        public CornerTool() { }

        public void SetProperty(IOpenCVPropertyContour property) => this.property = property;

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            if (!TryValidateAdaptiveThreshold(
                property,
                VisionToolErrorCode.CornerInvalidAdaptiveBlockSize,
                out errorCode,
                out message))
            {
                return false;
            }

            if (!TryValidateRoiSet(
                property,
                property.USE_ROI,
                true,
                VisionToolErrorCode.CornerRoiInvalid,
                "Corner",
                out errorCode,
                out message))
            {
                return false;
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

        protected override bool TryValidateAfterRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (results.Count == 0)
            {
                errorCode = VisionToolErrorCode.CornerNoResult;
                message = "Corner found no result.";
                return false;
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

        public override void Run()
        {
            results.Clear();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                return;
            }

            ReplaceResultImage(imageSource.Clone());
            OpenCvHelper.SetImageChannel3(imageResult);

            if (property.USE_MULTI_ROI)
            {
                foreach (Rect configuredRoi in property.CvROIS)
                {
                    DetectCorners(NormalizeCornerRoi(configuredRoi), true);
                }
            }
            else
            {
                DetectCorners(NormalizeCornerRoi(property.CvROI), property.USE_ROI);
            }
        }

        private Rect NormalizeCornerRoi(Rect roi)
        {
            return roi.Width == 0 || roi.Height == 0
                ? new Rect(0, 0, imageSource.Width, imageSource.Height)
                : roi;
        }

        private void DetectCorners(Rect roi, bool useRoi)
        {
            using (Mat imageCorner = CreatePreprocessedImage(roi, useRoi, property))
            {
                Point2f[] corners = Cv2.GoodFeaturesToTrack(imageCorner, 1000, 0.1, 5, null, 3, true, 0);
                if (corners == null || corners.Length == 0)
                {
                    return;
                }

                Point2f[] refinedCorners = Cv2.CornerSubPix(
                    imageCorner,
                    corners,
                    new OpenCvSharp.Size(3, 3),
                    new OpenCvSharp.Size(-1, -1),
                    TermCriteria.Both(10, 0.03));

                foreach (Point2f corner in corners)
                {
                    Point2f global = ToGlobalPoint(corner, roi, useRoi);
                    Cv2.Circle(imageResult, new OpenCvSharp.Point((int)global.X, (int)global.Y), 5, Scalar.Yellow, Cv2.FILLED);
                }

                foreach (Point2f corner in refinedCorners ?? corners)
                {
                    Point2f global = ToGlobalPoint(corner, roi, useRoi);
                    int x = Math.Max(0, Math.Min(imageSource.Width - 1, (int)Math.Round(global.X)));
                    int y = Math.Max(0, Math.Min(imageSource.Height - 1, (int)Math.Round(global.Y)));

                    Cv2.Circle(imageResult, new OpenCvSharp.Point((int)global.X, (int)global.Y), 5, Scalar.Red, Cv2.FILLED);
                    results.Add(new CornerResult(0d, new Point2d(global.X, global.Y), new Rect(x, y, 1, 1)));
                }
            }
        }

        private static Point2f ToGlobalPoint(Point2f point, Rect roi, bool useRoi)
        {
            return useRoi
                ? new Point2f(point.X + roi.X, point.Y + roi.Y)
                : point;
        }
    }
}

