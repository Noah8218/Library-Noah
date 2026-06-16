using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lib.Common;
using Lib.OpenCV.Property;
using Lib.OpenCV.Result;
using OpenCvSharp;

namespace Lib.OpenCV.Tool
{
    public partial class ContourTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyContour property;

        public List<ContourResult> results = new List<ContourResult>();
        
        public ContourTool() { }

        public void SetProperty(IOpenCVPropertyContour propertyBase) => property = propertyBase;

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            if (!TryValidateAreaRange(
                property.MIN_AREA,
                property.MAX_AREA,
                VisionToolErrorCode.ContourInvalidAreaRange,
                "Contour",
                out errorCode,
                out message))
            {
                return false;
            }

            if (!TryValidateAdaptiveThreshold(
                property,
                VisionToolErrorCode.ContourInvalidAdaptiveBlockSize,
                out errorCode,
                out message))
            {
                return false;
            }

            if (!TryValidateRoiSet(
                property,
                property.USE_ROI,
                true,
                VisionToolErrorCode.ContourRoiInvalid,
                "Contour",
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
            if (results == null || results.Count == 0)
            {
                errorCode = VisionToolErrorCode.ContourNoResult;
                message = $"Contour found no result. Area={property.MIN_AREA}..{property.MAX_AREA}, ROI={FormatContourRoi()}";
                return false;
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

        public override void Run()
        {
            if (property.USE_MULTI_ROI)
            {
                MultiRun();
            }
            else
            {
                SingleRun();
            }
        }

        public bool SingleRun()
        {
            OpenCvSharp.Point[][] Contours;
            HierarchyIndex[] hierarchy;

            int MinArea = property.MIN_AREA;
            int MaxArea = property.MAX_AREA;

            results.Clear();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {

                return false;
            }

            Rect roi = NormalizeContourRoi(property.CvROI);

            if (property.USE_DRAW_IMAGE)
            {
                ReplaceResultImage(imageSource.Clone());
                OpenCvHelper.SetImageChannel3(imageResult);
            }

            using (Mat imageSrc = CreateWorkingContourImage(roi, property.USE_ROI))
            {
                Cv2.FindContours(imageSrc, out Contours, out hierarchy, property.DetectMode, property.ApproximationModes, null);
            }

            AddRoiToContourPoints(Contours, roi, property.USE_ROI);

            ConcurrentBag<ContourResult> filteredContours = new ConcurrentBag<ContourResult>();
            ConcurrentBag<OpenCvSharp.Point[]> drawContours = new ConcurrentBag<OpenCvSharp.Point[]>();

            Parallel.ForEach(Contours, (item, state, index) =>
            {
                if (TryCreateContourResult(item, index, MinArea, MaxArea, true, out ContourResult result, out OpenCvSharp.Point[] drawContour))
                {
                    filteredContours.Add(result);
                    drawContours.Add(drawContour);
                }
            });

            if (property.USE_DRAW_IMAGE) { Cv2.DrawContours(imageResult, drawContours.ToArray(), -1, new Scalar(property.DrawColor.B, property.DrawColor.G, property.DrawColor.R, property.DrawColor.A), property.DrawThickness, LineTypes.Link4); }
            results = ReindexContours(filteredContours.OrderBy(c => c.Index)).ToList();

        
            return true;
        }

        public bool MultiRun()
        {
            OpenCvSharp.Point[][] Contours;
            HierarchyIndex[] hierarchy;

            int MinArea = property.MIN_AREA;
            int MaxArea = property.MAX_AREA;

            results.Clear();
            List<OpenCvSharp.Point[]> drawContoursList = new List<OpenCvSharp.Point[]>();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {

                return false;
            }

            if (property.USE_DRAW_IMAGE)
            {
                ReplaceResultImage(imageSource.Clone());
                OpenCvHelper.SetImageChannel3(imageResult);
            }

            for (int i = 0; i < property.CvROIS.Count; i++)
            {
                Rect roi = NormalizeContourRoi(property.CvROIS[i]);

                using (Mat imageSrc = CreateWorkingContourImage(roi, true))
                {
                    Cv2.FindContours(imageSrc, out Contours, out hierarchy, property.DetectMode, property.ApproximationModes, null);
                }

                AddRoiToContourPoints(Contours, roi, true);

                ConcurrentBag<ContourResult> filteredContours = new ConcurrentBag<ContourResult>();
                ConcurrentBag<OpenCvSharp.Point[]> drawContours = new ConcurrentBag<OpenCvSharp.Point[]>();

                Parallel.ForEach(Contours, (item, state, index) =>
                {
                    if (TryCreateContourResult(item, index, MinArea, MaxArea, false, out ContourResult result, out OpenCvSharp.Point[] drawContour))
                    {
                        filteredContours.Add(result);
                        drawContours.Add(drawContour);
                    }
                });

                results.AddRange(filteredContours.OrderBy(c => c.Index));
                drawContoursList.AddRange(drawContours);
            }

            if (property.USE_DRAW_IMAGE) { Cv2.DrawContours(imageResult, drawContoursList.ToArray(), -1, new Scalar(property.DrawColor.B, property.DrawColor.G, property.DrawColor.R, property.DrawColor.A), property.DrawThickness, LineTypes.Link4); }
            results = ReindexContours(results).ToList();
            
        

            return true;
        }

        private Mat CreateWorkingContourImage(OpenCvSharp.Rect roi, bool useRoi)
        {
            return CreatePreprocessedImage(roi, useRoi, property);
        }

        private bool TryCreateContourResult(
            OpenCvSharp.Point[] contour,
            long sourceIndex,
            int minArea,
            int maxArea,
            bool useDrawContourAsResult,
            out ContourResult result,
            out OpenCvSharp.Point[] drawContour)
        {
            result = null;
            drawContour = null;

            double contourArea = Cv2.ContourArea(contour, false);
            if (contourArea < minArea || contourArea > maxArea)
            {
                return false;
            }

            OpenCvSharp.Point[] contourForCalc;
            if (property.USE_APPROXPOLYDP)
            {
                double peri = Cv2.ArcLength(contour, true);
                OpenCvSharp.Point[] approxPoints = Cv2.ApproxPolyDP(contour, property.EPSILON * peri, true);
                contourForCalc = approxPoints;
                drawContour = approxPoints;
            }
            else
            {
                contourForCalc = contour;
                drawContour = contour;
            }

            Rect bounds = Cv2.BoundingRect(contourForCalc);
            RotatedRect rotatedRect = Cv2.MinAreaRect(contourForCalc);
            OpenCvSharp.Point center = new OpenCvSharp.Point(
                bounds.X + bounds.Width / 2,
                bounds.Y + bounds.Height / 2);
            OpenCvSharp.Point[] resultContour = useDrawContourAsResult ? drawContour : contour;

            result = new ContourResult(
                (int)sourceIndex,
                contourArea,
                center,
                bounds,
                resultContour,
                Math.Round(rotatedRect.Angle, 1));
            return true;
        }

        private Rect NormalizeContourRoi(Rect roi)
        {
            return roi.Width == 0 || roi.Height == 0
                ? new Rect(0, 0, imageSource.Width, imageSource.Height)
                : roi;
        }

        private string FormatContourRoi()
        {
            if (property.USE_MULTI_ROI)
            {
                return $"Multi({property.CvROIS?.Count ?? 0})";
            }

            Rect roi = NormalizeContourRoi(property.CvROI);
            return $"{roi.X},{roi.Y},{roi.Width},{roi.Height}";
        }

        private static IEnumerable<ContourResult> ReindexContours(IEnumerable<ContourResult> source)
        {
            int index = 1;
            foreach (ContourResult result in source)
            {
                result.Index = index++;
                yield return result;
            }
        }

        private void AddRoiToContourPoints(OpenCvSharp.Point[][] Contours, OpenCvSharp.Rect CvROI, bool applyOffset)
        {
            if (applyOffset)
            {
                for (int i = 0; i < Contours.Length; i++)
                {
                    for (int j = 0; j < Contours[i].Length; j++)
                    {
                        Contours[i][j].X = Contours[i][j].X + CvROI.X;
                        Contours[i][j].Y = Contours[i][j].Y + CvROI.Y;
                    }
                }
            }
        }

        public bool SquareRun()
        {
            results.Clear();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                return false;
            }

            Rect roi = NormalizeContourRoi(property.CvROI);

            using (Mat imageSrc = imageSource.Clone())
            {
                if (OpenCvHelper.IsImageEmpty(imageSource)) return false;
                ReplaceResultImage(imageSrc.Clone());

                if (imageSrc.Channels() == 4) Cv2.CvtColor(imageSrc, imageSrc, ColorConversionCodes.RGBA2GRAY);
                if (imageSrc.Channels() == 3) Cv2.CvtColor(imageSrc, imageSrc, ColorConversionCodes.RGB2GRAY);
                if (imageResult.Channels() == 1) Cv2.CvtColor(imageResult, imageResult, ColorConversionCodes.GRAY2RGB);

                using (Mat imageContour = CreateWorkingContourImage(roi, property.USE_ROI))
                {
                    OpenCvSharp.Point[][] contours;
                    HierarchyIndex[] hierarchy;

                    Cv2.FindContours(imageContour, out contours, out hierarchy, property.DetectMode, property.ApproximationModes, null);
                    AddRoiToContourPoints(contours, roi, property.USE_ROI);

                    List<ContourResult> squareResults = new List<ContourResult>();
                    for (int i = 0; i < contours.Length; i++)
                    {
                        if (TryCreateSquareContourResult(
                            contours[i],
                            i,
                            property.MIN_AREA,
                            property.MAX_AREA,
                            out ContourResult result,
                            out OpenCvSharp.Point[] squarePoints))
                        {
                            squareResults.Add(result);
                            for (int j = 0; j < squarePoints.Length; j++)
                            {
                                Cv2.Circle(imageResult, squarePoints[j], 5, Scalar.Yellow, Cv2.FILLED);
                            }

                            Cv2.Polylines(imageResult, new[] { squarePoints }, true, Scalar.Yellow, 1, LineTypes.AntiAlias, 0);
                        }
                    }

                    results = ReindexContours(squareResults.OrderBy(result => result.Index)).ToList();
                }
            }

            return true;
        }

        private bool TryCreateSquareContourResult(
            OpenCvSharp.Point[] contour,
            long sourceIndex,
            int minArea,
            int maxArea,
            out ContourResult result,
            out OpenCvSharp.Point[] squarePoints)
        {
            result = null;
            squarePoints = null;

            double contourArea = Cv2.ContourArea(contour, false);
            if (contourArea < minArea || contourArea > maxArea)
            {
                return false;
            }

            double peri = Cv2.ArcLength(contour, true);
            OpenCvSharp.Point[] approxPoints = Cv2.ApproxPolyDP(contour, property.EPSILON * peri, true);
            if (approxPoints.Length != 4 || !Cv2.IsContourConvex(approxPoints) || !HasNearRightAngle(approxPoints))
            {
                return false;
            }

            Rect bounds = Cv2.BoundingRect(approxPoints);
            RotatedRect rotatedRect = Cv2.MinAreaRect(approxPoints);
            OpenCvSharp.Point center = new OpenCvSharp.Point(
                bounds.X + bounds.Width / 2,
                bounds.Y + bounds.Height / 2);

            squarePoints = approxPoints;
            result = new ContourResult(
                (int)sourceIndex,
                contourArea,
                center,
                bounds,
                squarePoints,
                Math.Round(rotatedRect.Angle, 1));
            return true;
        }

        private static bool HasNearRightAngle(OpenCvSharp.Point[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                double angle = FormulaUtil.threePointAngle(
                    points[i],
                    points[(i + points.Length - 1) % points.Length],
                    points[(i + 1) % points.Length]);
                if (Math.Abs(angle - 90d) > 5d)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

