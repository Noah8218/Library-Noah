using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Lib.OpenCV.Property;
using Lib.OpenCV.Result;
using OpenCvSharp;

namespace Lib.OpenCV.Tool
{
    public sealed class EdgeBasedTemplateMatchingTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyEdgeBasedTemplateMatching property;
        public List<MatchingResult> results = new List<MatchingResult>();

        private Mat originalTemplate = new Mat();
        private VisionToolErrorCode lastMatchingErrorCode = VisionToolErrorCode.MatchingNoResult;
        private string lastMatchingMessage = "Edge based template matching found no result.";

        public void SetProperty(IOpenCVPropertyEdgeBasedTemplateMatching propertyBase) => property = propertyBase;

        public void SetTemplateImage(Mat image)
        {
            originalTemplate?.Dispose();
            imageTemplate?.Dispose();

            originalTemplate = OpenCvHelper.IsImageEmpty(image) ? new Mat() : image.Clone();
            imageTemplate = originalTemplate.Clone();
        }

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            if (OpenCvHelper.IsImageEmpty(GetTemplateForRun()))
            {
                errorCode = VisionToolErrorCode.MatchingTemplateMissing;
                message = string.IsNullOrWhiteSpace(property.PATTERN_PATH)
                    ? "Edge based matching template image is not loaded."
                    : $"Edge based matching template image is not loaded. Path={property.PATTERN_PATH}.";
                return false;
            }

            if (property.SCORE_MIN < -1 || property.SCORE_MIN > 1)
            {
                errorCode = VisionToolErrorCode.InvalidParameter;
                message = $"Edge based matching SCORE_MIN must be between -1 and 1. SCORE_MIN={property.SCORE_MIN}.";
                return false;
            }

            if (property.NUM_MATCH <= 0)
            {
                errorCode = VisionToolErrorCode.InvalidParameter;
                message = $"Edge based matching NUM_MATCH must be greater than 0. NUM_MATCH={property.NUM_MATCH}.";
                return false;
            }

            if (property.SEARCH_STEP <= 0)
            {
                errorCode = VisionToolErrorCode.InvalidParameter;
                message = $"Edge based matching SEARCH_STEP must be greater than 0. SEARCH_STEP={property.SEARCH_STEP}.";
                return false;
            }

            if (property.MAX_TEMPLATE_POINTS <= 0)
            {
                errorCode = VisionToolErrorCode.InvalidParameter;
                message = $"Edge based matching MAX_TEMPLATE_POINTS must be greater than 0. MAX_TEMPLATE_POINTS={property.MAX_TEMPLATE_POINTS}.";
                return false;
            }

            if (!TryValidateAdaptiveThreshold(
                property,
                VisionToolErrorCode.MatchingInvalidAdaptiveBlockSize,
                out errorCode,
                out message))
            {
                return false;
            }

            if (!TryValidateRoiSet(
                property,
                property.USE_ROI,
                true,
                VisionToolErrorCode.MatchingRoiInvalid,
                "Edge based matching",
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
                errorCode = lastMatchingErrorCode;
                message = lastMatchingMessage;
                return false;
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

        public override void Run()
        {
            swTaktTimems.Restart();
            results.Clear();
            ResetMatchingFailure();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                SetMatchingFailure(VisionToolErrorCode.InputImageInvalid, "Source image is not loaded.");
                return;
            }

            using (Mat template = CreatePreparedGrayImage(GetTemplateForRun()))
            {
                EdgeTemplateModel model = CreateTemplateModel(template);
                if (model.Points.Count == 0)
                {
                    SetMatchingFailure(
                        VisionToolErrorCode.MatchingTemplateInvalid,
                        $"Edge based matching template has no usable edge points. {FormatMatchingOptions()}");
                    return;
                }

                if (property.USE_MULTI_ROI)
                {
                    RunMultiRoi(model);
                }
                else
                {
                    Rect roi = NormalizeRoi(property.CvROI);
                    RunRoi(model, roi, property.USE_ROI);
                }
            }

            if (property.USE_DRAW_IMAGE || results.Count > 0)
            {
                DrawResultImage();
            }

            swTaktTimems.Stop();
        }

        private void RunMultiRoi(EdgeTemplateModel model)
        {
            if (property.CvROIS == null)
            {
                return;
            }

            foreach (Rect roi in property.CvROIS)
            {
                RunRoi(model, NormalizeRoi(roi), true);
            }
        }

        private void RunRoi(EdgeTemplateModel model, Rect roi, bool useRoi)
        {
            using (Mat source = CreatePreprocessedImage(roi, useRoi, property))
            using (GradientImage gradients = GradientImage.Create(source))
            {
                List<RectangleF> suppressedBounds = new List<RectangleF>();
                int localMatchCount = property.USE_MULTI_ROI ? Math.Max(1, property.NUM_MATCH) : property.NUM_MATCH;

                for (int i = 0; i < localMatchCount; i++)
                {
                    MatchCandidate candidate = FindBestCandidate(gradients, model, suppressedBounds);
                    if (candidate == null || candidate.Score < property.SCORE_MIN)
                    {
                        if (results.Count == 0)
                        {
                            SetMatchingFailure(
                                VisionToolErrorCode.MatchingNoResult,
                                $"Edge based matching found no result above score threshold. BestScore={(candidate?.Score * 100.0) ?? 0:0.###}, {FormatMatchingOptions()}");
                        }

                        break;
                    }

                    MatchingResult result = CreateResult(candidate, model, roi, useRoi);
                    if (!IsDuplicate(result))
                    {
                        result.Index = results.Count + 1;
                        results.Add(result);
                    }

                    suppressedBounds.Add(CreateExpandedBounds(candidate.Bounds, 0.35f));
                }
            }
        }

        private MatchCandidate FindBestCandidate(
            GradientImage gradients,
            EdgeTemplateModel model,
            List<RectangleF> suppressedBounds)
        {
            int minCenterX = (int)Math.Ceiling(model.Center.X);
            int minCenterY = (int)Math.Ceiling(model.Center.Y);
            int maxCenterX = (int)Math.Floor(gradients.Width - (model.Width - model.Center.X));
            int maxCenterY = (int)Math.Floor(gradients.Height - (model.Height - model.Center.Y));

            if (maxCenterX < minCenterX || maxCenterY < minCenterY)
            {
                return null;
            }

            MatchCandidate best = null;
            int step = Math.Max(1, property.SEARCH_STEP);

            for (int y = minCenterY; y <= maxCenterY; y += step)
            {
                for (int x = minCenterX; x <= maxCenterX; x += step)
                {
                    RectangleF bounds = CreateLocalBounds(x, y, model);
                    if (IsSuppressed(bounds, suppressedBounds))
                    {
                        continue;
                    }

                    double score = ScoreCandidate(gradients, model, x, y);
                    if (best == null || score > best.Score)
                    {
                        best = new MatchCandidate
                        {
                            Center = new Point2d(x, y),
                            Bounds = bounds,
                            Score = score
                        };
                    }
                }
            }

            return best;
        }

        private double ScoreCandidate(GradientImage gradients, EdgeTemplateModel model, int centerX, int centerY)
        {
            double partialSum = 0d;
            double minScore = property.SCORE_MIN;
            double greediness = Clamp(property.GREEDINESS, 0d, 0.999d);
            int pointCount = model.Points.Count;
            double normMinScore = minScore / pointCount;
            double normGreediness = (1d - greediness * minScore) / (1d - greediness) / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                TemplateEdgePoint point = model.Points[i];
                int x = centerX + point.OffsetX;
                int y = centerY + point.OffsetY;

                double sourceMagnitude = gradients.Magnitude.At<double>(y, x);
                if (sourceMagnitude > property.MIN_GRADIENT_MAGNITUDE)
                {
                    double sourceDx = gradients.Dx.At<double>(y, x);
                    double sourceDy = gradients.Dy.At<double>(y, x);
                    partialSum += ((sourceDx * point.Dx) + (sourceDy * point.Dy)) * point.InvMagnitude / sourceMagnitude;
                }

                int processed = i + 1;
                double partialScore = partialSum / processed;
                double breakScore = Math.Min((minScore - 1d) + normGreediness * processed, normMinScore * processed);
                if (partialScore < breakScore)
                {
                    return partialScore;
                }
            }

            return partialSum / pointCount;
        }

        private EdgeTemplateModel CreateTemplateModel(Mat template)
        {
            using (Mat edges = new Mat())
            using (GradientImage gradients = GradientImage.Create(template))
            {
                Cv2.Canny(
                    template,
                    edges,
                    property.CANNY_LOW,
                    property.CANNY_HIGH,
                    NormalizeCannyAperture(property.CANNY_APERTURE_SIZE),
                    property.USE_L2_GRADIENT);

                Cv2.FindContours(
                    edges,
                    out OpenCvSharp.Point[][] contours,
                    out _,
                    property.CONTOUR_RETRIEVAL_MODE,
                    property.CONTOUR_APPROXIMATION_MODE);

                List<TemplateEdgePoint> points = new List<TemplateEdgePoint>();
                HashSet<long> seen = new HashSet<long>();
                foreach (OpenCvSharp.Point[] contour in contours)
                {
                    foreach (OpenCvSharp.Point point in contour)
                    {
                        long key = ((long)point.Y << 32) | (uint)point.X;
                        if (!seen.Add(key))
                        {
                            continue;
                        }

                        double magnitude = gradients.Magnitude.At<double>(point.Y, point.X);
                        if (magnitude <= property.MIN_GRADIENT_MAGNITUDE)
                        {
                            continue;
                        }

                        points.Add(new TemplateEdgePoint
                        {
                            X = point.X,
                            Y = point.Y,
                            Dx = gradients.Dx.At<double>(point.Y, point.X),
                            Dy = gradients.Dy.At<double>(point.Y, point.X),
                            InvMagnitude = 1d / magnitude
                        });
                    }
                }

                points = Downsample(points, property.MAX_TEMPLATE_POINTS);
                if (points.Count == 0)
                {
                    return new EdgeTemplateModel(template.Width, template.Height, new Point2d(), points);
                }

                Point2d center = new Point2d(points.Average(point => point.X), points.Average(point => point.Y));
                foreach (TemplateEdgePoint point in points)
                {
                    point.OffsetX = (int)Math.Round(point.X - center.X);
                    point.OffsetY = (int)Math.Round(point.Y - center.Y);
                }

                return new EdgeTemplateModel(template.Width, template.Height, center, points);
            }
        }

        private Mat CreatePreparedGrayImage(Mat source)
        {
            Mat image = source.Clone();
            ApplyCommonPreprocessing(image, property);
            return image;
        }

        private Mat GetTemplateForRun()
        {
            return OpenCvHelper.IsImageEmpty(originalTemplate) ? imageTemplate : originalTemplate;
        }

        private Rect NormalizeRoi(Rect roi)
        {
            return roi.Width == 0 || roi.Height == 0
                ? new Rect(0, 0, imageSource.Width, imageSource.Height)
                : roi;
        }

        private MatchingResult CreateResult(MatchCandidate candidate, EdgeTemplateModel model, Rect roi, bool applyRoiOffset)
        {
            float x = candidate.Bounds.X;
            float y = candidate.Bounds.Y;
            if (applyRoiOffset)
            {
                x += roi.X;
                y += roi.Y;
            }

            Rect2f bounding = new Rect2f(x, y, model.Width, model.Height);
            Point2f center = new Point2f(
                (float)(candidate.Center.X + (applyRoiOffset ? roi.X : 0)),
                (float)(candidate.Center.Y + (applyRoiOffset ? roi.Y : 0)));
            return new MatchingResult(0, Math.Round(candidate.Score * 100d, 3), center, bounding, 0d);
        }

        private RectangleF CreateLocalBounds(int centerX, int centerY, EdgeTemplateModel model)
        {
            return new RectangleF(
                (float)(centerX - model.Center.X),
                (float)(centerY - model.Center.Y),
                model.Width,
                model.Height);
        }

        private bool IsDuplicate(MatchingResult result)
        {
            double threshold = Math.Max(4d, Math.Min(result.Bounding.Width, result.Bounding.Height) * 0.35d);
            foreach (MatchingResult existing in results)
            {
                double dx = existing.Center.X - result.Center.X;
                double dy = existing.Center.Y - result.Center.Y;
                if (Math.Sqrt(dx * dx + dy * dy) < threshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSuppressed(RectangleF bounds, List<RectangleF> suppressedBounds)
        {
            if (suppressedBounds == null || suppressedBounds.Count == 0)
            {
                return false;
            }

            PointF center = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            return suppressedBounds.Any(rect => rect.Contains(center));
        }

        private static RectangleF CreateExpandedBounds(RectangleF bounds, float expansionRatio)
        {
            float expandX = bounds.Width * expansionRatio;
            float expandY = bounds.Height * expansionRatio;
            return new RectangleF(
                bounds.X - expandX,
                bounds.Y - expandY,
                bounds.Width + expandX * 2,
                bounds.Height + expandY * 2);
        }

        private void DrawResultImage()
        {
            ReplaceResultImage(imageSource.Clone());
            OpenCvHelper.SetImageChannel3(imageResult);

            foreach (MatchingResult result in results)
            {
                Rect rect = new Rect(
                    (int)Math.Round(result.Bounding.X),
                    (int)Math.Round(result.Bounding.Y),
                    (int)Math.Round(result.Bounding.Width),
                    (int)Math.Round(result.Bounding.Height));
                Cv2.Rectangle(imageResult, rect, new Scalar(50, 205, 50), 2);
                Cv2.Circle(
                    imageResult,
                    new OpenCvSharp.Point((int)Math.Round(result.Center.X), (int)Math.Round(result.Center.Y)),
                    4,
                    Scalar.Red,
                    -1);
                Cv2.PutText(
                    imageResult,
                    $"#{result.Index} {result.Score:0.0}",
                    new OpenCvSharp.Point(rect.X, Math.Max(14, rect.Y - 5)),
                    HersheyFonts.HersheySimplex,
                    0.45,
                    Scalar.Yellow,
                    1,
                    LineTypes.AntiAlias);
            }
        }

        private void ResetMatchingFailure()
        {
            lastMatchingErrorCode = VisionToolErrorCode.MatchingNoResult;
            lastMatchingMessage = "Edge based template matching found no result.";
        }

        private void SetMatchingFailure(VisionToolErrorCode errorCode, string message)
        {
            if (results.Count > 0)
            {
                return;
            }

            lastMatchingErrorCode = errorCode;
            lastMatchingMessage = message ?? string.Empty;
        }

        private string FormatMatchingOptions()
        {
            string roi = property.USE_MULTI_ROI
                ? $"Multi({property.CvROIS?.Count ?? 0})"
                : FormatRoi(NormalizeRoi(property.CvROI));
            return $"ScoreMin={property.SCORE_MIN}, MatchCount={property.NUM_MATCH}, Canny={property.CANNY_LOW}..{property.CANNY_HIGH}, SearchStep={property.SEARCH_STEP}, MaxPoints={property.MAX_TEMPLATE_POINTS}, ROI={roi}";
        }

        private static string FormatRoi(Rect roi)
        {
            return $"{roi.X},{roi.Y},{roi.Width},{roi.Height}";
        }

        private static int NormalizeCannyAperture(int value)
        {
            if (value <= 3)
            {
                return 3;
            }

            if (value <= 5)
            {
                return 5;
            }

            return 7;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) { return min; }
            if (value > max) { return max; }
            return value;
        }

        private static List<TemplateEdgePoint> Downsample(List<TemplateEdgePoint> points, int maxPoints)
        {
            if (points == null || points.Count <= maxPoints)
            {
                return points ?? new List<TemplateEdgePoint>();
            }

            List<TemplateEdgePoint> sampled = new List<TemplateEdgePoint>(maxPoints);
            double step = (double)points.Count / maxPoints;
            for (int i = 0; i < maxPoints; i++)
            {
                sampled.Add(points[(int)Math.Floor(i * step)]);
            }

            return sampled;
        }

        private sealed class GradientImage : IDisposable
        {
            private GradientImage(Mat dx, Mat dy, Mat magnitude)
            {
                Dx = dx;
                Dy = dy;
                Magnitude = magnitude;
            }

            public Mat Dx { get; }
            public Mat Dy { get; }
            public Mat Magnitude { get; }
            public int Width => Dx.Width;
            public int Height => Dx.Height;

            public static GradientImage Create(Mat image)
            {
                Mat dx = new Mat();
                Mat dy = new Mat();
                Mat magnitude = new Mat();
                Mat direction = new Mat();

                Cv2.Sobel(image, dx, MatType.CV_64F, 1, 0, 3);
                Cv2.Sobel(image, dy, MatType.CV_64F, 0, 1, 3);
                Cv2.CartToPolar(dx, dy, magnitude, direction);
                direction.Dispose();

                return new GradientImage(dx, dy, magnitude);
            }

            public void Dispose()
            {
                Dx?.Dispose();
                Dy?.Dispose();
                Magnitude?.Dispose();
            }
        }

        private sealed class EdgeTemplateModel
        {
            public EdgeTemplateModel(int width, int height, Point2d center, List<TemplateEdgePoint> points)
            {
                Width = width;
                Height = height;
                Center = center;
                Points = points ?? new List<TemplateEdgePoint>();
            }

            public int Width { get; }
            public int Height { get; }
            public Point2d Center { get; }
            public List<TemplateEdgePoint> Points { get; }
        }

        private sealed class TemplateEdgePoint
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int OffsetX { get; set; }
            public int OffsetY { get; set; }
            public double Dx { get; set; }
            public double Dy { get; set; }
            public double InvMagnitude { get; set; }
        }

        private sealed class MatchCandidate
        {
            public Point2d Center { get; set; }
            public RectangleF Bounds { get; set; }
            public double Score { get; set; }
        }
    }
}

