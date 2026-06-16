using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lib.Common;
using Lib.OpenCV.Property;
using Lib.OpenCV.Result;
using OpenCvSharp;

namespace Lib.OpenCV.Tool
{
    public partial class MatchingTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyMatching property;
        public List<MatchingResult> results = new List<MatchingResult>();
        private Mat originalTemplate = new Mat();
        public MatchingTool() { }
        
        public void SetTemplateImage(Mat Image)
        {
            originalTemplate?.Dispose();
            imageTemplate?.Dispose();

            originalTemplate = OpenCvHelper.IsImageEmpty(Image) ? new Mat() : Image.Clone();
            imageTemplate = originalTemplate.Clone();
        }
       
        public void SetProperty(IOpenCVPropertyMatching property) => this.property = property;

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            Mat template = GetTemplateForRun();
            if (OpenCvHelper.IsImageEmpty(template))
            {
                errorCode = VisionToolErrorCode.MatchingTemplateMissing;
                message = string.IsNullOrWhiteSpace(property.PATTERN_PATH)
                    ? "Matching template image is not loaded."
                    : $"Matching template image is not loaded. Path={property.PATTERN_PATH}.";
                return false;
            }

            if (property.MAGNIFIATION <= 0)
            {
                errorCode = VisionToolErrorCode.MatchingInvalidScale;
                message = $"Matching magnification must be greater than 0. Magnification={property.MAGNIFIATION}.";
                return false;
            }

            if (property.USE_FIND_ANGLE && property.FIND_ANGLE <= 0)
            {
                errorCode = VisionToolErrorCode.MatchingInvalidAngleStep;
                message = $"Matching angle step must be greater than 0 when angle search is enabled. AngleStep={property.FIND_ANGLE}.";
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
                "Matching",
                out errorCode,
                out message))
            {
                return false;
            }

            if (!TryValidateMatchingWorkingSize(template, out errorCode, out message))
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
                errorCode = VisionToolErrorCode.MatchingNoResult;
                message = $"Matching found no result. {FormatMatchingOptions()}";
                return false;
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

        private bool TryValidateMatchingWorkingSize(Mat template, out VisionToolErrorCode errorCode, out string message)
        {
            int templateWidth = (int)(template.Width / property.MAGNIFIATION);
            int templateHeight = (int)(template.Height / property.MAGNIFIATION);

            if (templateWidth <= 0 || templateHeight <= 0)
            {
                errorCode = VisionToolErrorCode.MatchingInvalidScale;
                message = $"Matching magnification creates an empty template image. Magnification={property.MAGNIFIATION}.";
                return false;
            }

            foreach (Rect sourceBounds in GetMatchingSourceBounds())
            {
                int sourceWidth = (int)(sourceBounds.Width / property.MAGNIFIATION);
                int sourceHeight = (int)(sourceBounds.Height / property.MAGNIFIATION);

                if (sourceWidth <= 0 || sourceHeight <= 0)
                {
                    errorCode = VisionToolErrorCode.MatchingInvalidScale;
                    message = $"Matching magnification creates an empty working image. Magnification={property.MAGNIFIATION}.";
                    return false;
                }

                if (templateWidth > sourceWidth || templateHeight > sourceHeight)
                {
                    errorCode = VisionToolErrorCode.MatchingTemplateInvalid;
                    message = $"Matching template is larger than the working image. Template={templateWidth}x{templateHeight}, Source={sourceWidth}x{sourceHeight}.";
                    return false;
                }
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

        private IEnumerable<Rect> GetMatchingSourceBounds()
        {
            Rect fullImage = new Rect(0, 0, imageSource.Width, imageSource.Height);

            if (property.USE_MULTI_ROI && property.CvROIS != null && property.CvROIS.Count > 0)
            {
                foreach (Rect roi in property.CvROIS)
                {
                    yield return roi.Width > 0 && roi.Height > 0 ? roi : fullImage;
                }

                yield break;
            }

            yield return property.USE_ROI && property.CvROI.Width > 0 && property.CvROI.Height > 0
                ? property.CvROI
                : fullImage;
        }

        protected override VisionToolErrorCode ResolveExecutionErrorCode(Exception exception)
        {
            VisionToolErrorCode baseCode = base.ResolveExecutionErrorCode(exception);
            if (baseCode == VisionToolErrorCode.OpenCvExecutionFailed
                && OpenCvHelper.IsImageEmpty(imageTemplate))
            {
                return VisionToolErrorCode.MatchingTemplateInvalid;
            }

            return baseCode;
        }

        private Mat GetTemplateForRun()
        {
            return OpenCvHelper.IsImageEmpty(originalTemplate) ? imageTemplate : originalTemplate;
        }

        private Mat CreatePreparedTemplate()
        {
            Mat template = GetTemplateForRun().Clone();
            ApplyMatchingPreprocessing(template);
            return template;
        }

        private void ApplyMatchingPreprocessing(Mat image)
        {
            OpenCvHelper.SetImageChannel1(image);

            if (property.USE_THRESHOLD)
            {
                Cv2.Threshold(image, image, property.THRESHOLD, 255, property.THRESHOLD_TYPES);
            }
            else if (property.USE_ADAPTIVE_THRESHOLD)
            {
                Cv2.AdaptiveThreshold(
                    image,
                    image,
                    property.ADAPTIVE_THRESHOLD,
                    property.ADAPTIVE_THRESHOLD_ALGORITHM,
                    property.ADAPTIVE_THRESHOLD_TYPES,
                    property.BlockSize,
                    property.Weight);
            }

            if (property.USE_CANNY)
            {
                Cv2.GaussianBlur(image, image, new OpenCvSharp.Size(3, 3), 1, 1, BorderTypes.Default);
                Cv2.Canny(image, image, property.CANNY_LOW, property.CANNY_HIGH, 3, true);
            }
        }

        private Mat ResizeByMagnification(Mat image)
        {
            return image.Resize(new OpenCvSharp.Size(
                (int)(image.Width / property.MAGNIFIATION),
                (int)(image.Height / property.MAGNIFIATION)));
        }
     
        private void FindTemplate(Mat ImageSorce, Mat Template, ConcurrentBag<MatchingResult> Results_T, double angle, OpenCvSharp.Rect CvROI, bool applyRoiOffset)
        {
            using (Mat imageMatching = new Mat())
            {
                Cv2.MatchTemplate(ImageSorce, Template, imageMatching, property.MATCH_MODE, null);

                for (int attempt = 0; attempt < 64; attempt++)
                {
                    if (!TryGetBestMatch(imageMatching, out OpenCvSharp.Point matchLocation, out double qualityScore))
                    {
                        break;
                    }

                    if (ShouldValidateCandidateByImageDifference(property.MATCH_MODE)
                        && !IsValidMatchingCandidate(ImageSorce, Template, matchLocation))
                    {
                        SuppressMatchingScore(imageMatching, Template, matchLocation, property.MATCH_MODE);
                        continue;
                    }

                    int ImageTplW = Template.Width;
                    int ImageTplH = Template.Height;

                    OpenCvSharp.Point ptStart = new OpenCvSharp.Point(matchLocation.X, matchLocation.Y);
                    OpenCvSharp.Point ptEnd = new OpenCvSharp.Point(matchLocation.X + (ImageTplW), matchLocation.Y + (ImageTplH));
                    double dScore = qualityScore * 100.0D;

                    Rect2f rtBounding = applyRoiOffset
                        ? new Rect2f(ptStart.X + CvROI.X, ptStart.Y + CvROI.Y, ptEnd.X - ptStart.X, ptEnd.Y - ptStart.Y)
                        : new Rect2f(ptStart.X, ptStart.Y, ptEnd.X - ptStart.X, ptEnd.Y - ptStart.Y);

                    OpenCvSharp.Point2f ptCenter = new OpenCvSharp.Point2f((rtBounding.X + (rtBounding.X + rtBounding.Width)) / 2, (rtBounding.Y + (rtBounding.Y + rtBounding.Height)) / 2);
                    Results_T.Add(new MatchingResult(0, dScore, ptCenter, rtBounding, angle));
                    break;
                }
            }
        }

        private bool TryGetBestMatch(Mat imageMatching, out OpenCvSharp.Point matchLocation, out double qualityScore)
        {
            Cv2.MinMaxLoc(
                imageMatching,
                out double minScore,
                out double maxScore,
                out OpenCvSharp.Point minLocation,
                out OpenCvSharp.Point maxLocation);

            bool lowerScoreIsBetter = IsLowerScoreBetter(property.MATCH_MODE);
            double rawScore = lowerScoreIsBetter ? minScore : maxScore;
            matchLocation = lowerScoreIsBetter ? minLocation : maxLocation;
            qualityScore = ConvertToQualityScore(rawScore, property.MATCH_MODE);
            return qualityScore > property.SCORE_MIN;
        }

        private static bool IsLowerScoreBetter(TemplateMatchModes matchMode)
        {
            return matchMode == TemplateMatchModes.SqDiff
                || matchMode == TemplateMatchModes.SqDiffNormed;
        }

        private static double ConvertToQualityScore(double rawScore, TemplateMatchModes matchMode)
        {
            switch (matchMode)
            {
                case TemplateMatchModes.SqDiffNormed:
                    return 1.0 - Clamp01(rawScore);
                case TemplateMatchModes.SqDiff:
                    return 1.0 / (1.0 + Math.Max(0.0, rawScore));
                default:
                    return rawScore;
            }
        }

        private static double Clamp01(double value)
        {
            if (value < 0) { return 0; }
            if (value > 1) { return 1; }
            return value;
        }

        private static bool ShouldValidateCandidateByImageDifference(TemplateMatchModes matchMode)
        {
            return matchMode == TemplateMatchModes.SqDiff
                || matchMode == TemplateMatchModes.SqDiffNormed;
        }

        private static bool IsValidMatchingCandidate(Mat imageSource, Mat template, OpenCvSharp.Point location)
        {
            Rect candidateRect = new Rect(location, template.Size());
            if (candidateRect.X < 0
                || candidateRect.Y < 0
                || candidateRect.Right > imageSource.Width
                || candidateRect.Bottom > imageSource.Height)
            {
                return false;
            }

            using (Mat candidate = imageSource.SubMat(candidateRect))
            {
                Cv2.MeanStdDev(template, out _, out Scalar templateStdDev);
                Cv2.MeanStdDev(candidate, out _, out Scalar candidateStdDev);

                double templateDeviation = templateStdDev.Val0;
                double candidateDeviation = candidateStdDev.Val0;
                if (templateDeviation < 1.0)
                {
                    return true;
                }

                if (candidateDeviation < Math.Max(1.0, templateDeviation * 0.5))
                {
                    return false;
                }

                double meanAbsoluteDifference = GetMeanAbsoluteDifference(candidate, template);
                return meanAbsoluteDifference <= Math.Max(20.0, templateDeviation * 0.5);
            }
        }

        private static double GetMeanAbsoluteDifference(Mat candidate, Mat template)
        {
            using (Mat difference = new Mat())
            {
                Cv2.Absdiff(candidate, template, difference);
                Scalar mean = Cv2.Mean(difference);
                int channels = Math.Max(1, difference.Channels());
                double sum = mean.Val0;
                if (channels > 1)
                {
                    sum += mean.Val1;
                }

                if (channels > 2)
                {
                    sum += mean.Val2;
                }

                if (channels > 3)
                {
                    sum += mean.Val3;
                }

                return sum / channels;
            }
        }

        private static void SuppressMatchingScore(Mat imageMatching, Mat template, OpenCvSharp.Point location, TemplateMatchModes matchMode)
        {
            Rect suppressRect = ClampRectToImage(
                new Rect(
                    location.X - Math.Max(1, template.Width / 2),
                    location.Y - Math.Max(1, template.Height / 2),
                    Math.Max(3, template.Width),
                    Math.Max(3, template.Height)),
                imageMatching);
            if (suppressRect.Width > 0 && suppressRect.Height > 0)
            {
                Scalar suppressedScore = IsLowerScoreBetter(matchMode)
                    ? Scalar.All(double.MaxValue / 4)
                    : Scalar.All(-1);
                imageMatching.Rectangle(suppressRect, suppressedScore, -1);
            }
        }

        public Mat Rotate(Mat src, double angle, bool PaddingWhite = false)
        {
            Mat rotate = new Mat(src.Size(), src.Type());
            Mat matrix = Cv2.GetRotationMatrix2D(new Point2f(src.Width / 2, src.Height / 2), angle, 1);
            if (PaddingWhite)
            {
                Cv2.WarpAffine(src, rotate, matrix, src.Size(), InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(255, 255, 255));
            }
            else
            {
                Cv2.WarpAffine(src, rotate, matrix, src.Size(), InterpolationFlags.Linear, BorderTypes.Reflect);
            }

            return rotate;
        }

        public override void Run()
        {
            if(property.USE_MULTI_ROI)
            {
                ImagePyramidsMultiRun();
            }
            else 
            {
                ImagePyramidsSingleRun();
            }
        }

        public bool ImagePyramidsMultiRun()
        {
            swTaktTimems.Restart();
            results.Clear();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                return false;
            }

            using (Mat preparedSource = imageSource.Clone())
            using (Mat preparedTemplate = CreatePreparedTemplate())
            {
                ApplyMatchingPreprocessing(preparedSource);

                for (int i = 0; i < property.CvROIS.Count; i++)
                {
                    Rect roi = property.CvROIS[i];
                    if (roi.Width == 0 || roi.Height == 0)
                    {
                        roi = new Rect(0, 0, preparedSource.Width, preparedSource.Height);
                    }

                    if (!RunMatchingForSource(preparedSource, preparedTemplate, roi, true, true))
                    {
                        return false;
                    }
                }
            }
            swTaktTimems.Stop();
        

            return true;
        }

        public bool ImagePyramidsSingleRun()
        {
            swTaktTimems.Restart();
            results.Clear();

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                return false;
            }

            using (Mat preparedSource = imageSource.Clone())
            {
                ApplyMatchingPreprocessing(preparedSource);
                Rect roi = property.USE_ROI && property.CvROI.Width > 0 && property.CvROI.Height > 0
                    ? property.CvROI
                    : new Rect(0, 0, preparedSource.Width, preparedSource.Height);

                using (Mat preparedTemplate = CreatePreparedTemplate())
                {
                    if (!RunMatchingForSource(preparedSource, preparedTemplate, roi, property.USE_ROI, property.USE_ROI))
                    {
                        return false;
                    }
                }
            }
            swTaktTimems.Stop();

            return true;
        }

        private bool RunMatchingForSource(Mat preparedSource, Mat preparedTemplate, Rect roi, bool useRoiCrop, bool applyRoiOffset)
        {
            if (OpenCvHelper.IsImageEmpty(preparedSource) || OpenCvHelper.IsImageEmpty(preparedTemplate))
            {
                return false;
            }

            using (Mat imageSrc = useRoiCrop ? preparedSource.SubMat(roi) : preparedSource.Clone())
            using (Mat imageTpl = ResizeByMagnification(preparedTemplate))
            {
                int maxCount = property.NUM_MATCH;
                int attempts = Math.Max(maxCount * 4, maxCount + 3);
                while (maxCount > 0 && attempts > 0)
                {
                    Thread.Sleep(0);
                    attempts--;

                    using (Mat imageSubMat = ResizeByMagnification(imageSrc))
                    {
                        MatchingResult highestScoreResult = FindBestMatchingCandidate(imageSubMat, imageTpl, roi, applyRoiOffset);
                        if (highestScoreResult == null)
                        {
                            break;
                        }

                        maxCount--;
                        MatchingResult refinedResult = TryRefineMatchingResult(imageSrc, preparedTemplate, roi, highestScoreResult, applyRoiOffset);
                        if (refinedResult != null && !AddFinalMatchingResult(refinedResult))
                        {
                            maxCount++;
                        }
                    }
                }
            }

            return true;
        }

        private MatchingResult FindBestMatchingCandidate(Mat imageSubMat, Mat imageTpl, Rect roi, bool applyRoiOffset)
        {
            ConcurrentBag<MatchingResult> candidates = new ConcurrentBag<MatchingResult>();
            double angleStep = property.FIND_ANGLE;

            Task firstTask = Task.Run(() =>
            {
                FindTemplate(imageSubMat, imageTpl, candidates, 0, roi, applyRoiOffset);
            });

            if (property.USE_FIND_ANGLE)
            {
                Task plusTask = Task.Run(() =>
                {
                    Parallel.ForEach(CreatePositiveSearchAngles(angleStep), angle =>
                    {
                        using (Mat rotated = Rotate(imageTpl, angle, property.USE_PADDING_COLOR_WHITE))
                        {
                            FindTemplate(imageSubMat, rotated, candidates, angle, roi, applyRoiOffset);
                        }
                    });
                });

                Task minusTask = Task.Run(() =>
                {
                    Parallel.ForEach(CreateNegativeSearchAngles(angleStep), angle =>
                    {
                        using (Mat rotated = Rotate(imageTpl, angle, property.USE_PADDING_COLOR_WHITE))
                        {
                            FindTemplate(imageSubMat, rotated, candidates, angle, roi, applyRoiOffset);
                        }
                    });
                });

                Task.WaitAll(plusTask);
                Task.WaitAll(minusTask);
            }

            Task.WaitAll(firstTask);
            return candidates.OrderByDescending(r => r.Score).FirstOrDefault();
        }

        private IEnumerable<double> CreatePositiveSearchAngles(double angleStep)
        {
            int count = (int)(property.FIND_ANGLE_MAX / angleStep);
            for (int i = 1; i <= count; i++)
            {
                yield return angleStep * i;
            }
        }

        private IEnumerable<double> CreateNegativeSearchAngles(double angleStep)
        {
            int count = Math.Abs((int)(property.FIND_ANGLE_MIN / angleStep));
            for (int i = 1; i <= count; i++)
            {
                yield return angleStep * i * -1;
            }
        }

        private string FormatMatchingRoi()
        {
            if (property.USE_MULTI_ROI)
            {
                return $"Multi({property.CvROIS?.Count ?? 0})";
            }

            Rect roi = property.USE_ROI && property.CvROI.Width > 0 && property.CvROI.Height > 0
                ? property.CvROI
                : new Rect(0, 0, imageSource.Width, imageSource.Height);
            return $"{roi.X},{roi.Y},{roi.Width},{roi.Height}";
        }

        private string FormatMatchingOptions()
        {
            return $"Mode={property.MATCH_MODE}, ScoreMin={property.SCORE_MIN}, NumMatch={property.NUM_MATCH}, Magnification={property.MAGNIFIATION}, AngleSearch={property.USE_FIND_ANGLE}, Threshold={property.USE_THRESHOLD}, Adaptive={property.USE_ADAPTIVE_THRESHOLD}, Canny={property.USE_CANNY}, ROI={FormatMatchingRoi()}";
        }

        private MatchingResult TryRefineMatchingResult(Mat imageSrc, Mat preparedTemplate, Rect roi, MatchingResult highestScoreResult, bool applyRoiOffset)
        {
            if (CanUseCoarseMatchAsFinalResult(highestScoreResult))
            {
                SuppressCoarseMatchRegion(imageSrc, preparedTemplate, roi, highestScoreResult, applyRoiOffset);
                return highestScoreResult;
            }

            int originalMagnification = 2;
            Rect refineRect = GetRefineSearchRect(imageSrc, preparedTemplate, roi, highestScoreResult, applyRoiOffset);
            using (Mat refineImage = imageSrc.SubMat(refineRect))
            using (Mat imageSubMatPy = refineImage.Resize(new OpenCvSharp.Size((int)(refineImage.Width / originalMagnification), (int)(refineImage.Height / originalMagnification))))
            using (Mat imageTplPy = preparedTemplate.Resize(new OpenCvSharp.Size((int)(preparedTemplate.Width / originalMagnification), (int)(preparedTemplate.Height / originalMagnification))))
            using (Mat rotatedTemplate = Rotate(imageTplPy, highestScoreResult.Angle, property.USE_PADDING_COLOR_WHITE))
            using (Mat imageMatching = new Mat())
            {
                Cv2.MatchTemplate(imageSubMatPy, rotatedTemplate, imageMatching, property.MATCH_MODE, null);
                if (!TryGetBestMatch(imageMatching, out OpenCvSharp.Point ptMaxLocation, out double qualityScore))
                {
                    return null;
                }

                OpenCvSharp.Point ptStart = new OpenCvSharp.Point(ptMaxLocation.X, ptMaxLocation.Y);
                OpenCvSharp.Point ptEnd = new OpenCvSharp.Point(ptMaxLocation.X + imageTplPy.Width, ptMaxLocation.Y + imageTplPy.Height);

                ptMaxLocation.X = (int)(ptMaxLocation.X * originalMagnification);
                ptMaxLocation.Y = (int)(ptMaxLocation.Y * originalMagnification);
                ptMaxLocation.X += refineRect.X;
                ptMaxLocation.Y += refineRect.Y;

                Rect maskRect = ClampRectToImage(
                    new Rect(
                        new OpenCvSharp.Point(ptMaxLocation.X - 5, ptMaxLocation.Y - 5),
                        new OpenCvSharp.Size(preparedTemplate.Width + 10, preparedTemplate.Height + 10)),
                    imageSrc);
                if (maskRect.Width > 0 && maskRect.Height > 0)
                {
                    SuppressMatchedRegion(imageSrc, maskRect);
                }

                ptStart.X *= originalMagnification;
                ptStart.Y *= originalMagnification;
                ptEnd.X *= originalMagnification;
                ptEnd.Y *= originalMagnification;
                ptStart.X += refineRect.X;
                ptStart.Y += refineRect.Y;
                ptEnd.X += refineRect.X;
                ptEnd.Y += refineRect.Y;

                if (ShouldValidateCandidateByImageDifference(property.MATCH_MODE)
                    && !IsValidMatchingCandidate(imageSrc, preparedTemplate, ptStart))
                {
                    Rect invalidRect = ClampRectToImage(
                        new Rect(ptStart, new OpenCvSharp.Size(preparedTemplate.Width, preparedTemplate.Height)),
                        imageSrc);
                    if (invalidRect.Width > 0 && invalidRect.Height > 0)
                    {
                        SuppressMatchedRegion(imageSrc, invalidRect);
                    }

                    return null;
                }

                double refinedScore = qualityScore * 100.0D;
                Rect2f bounding = applyRoiOffset
                    ? new Rect2f(ptStart.X + roi.X, ptStart.Y + roi.Y, ptEnd.X - ptStart.X, ptEnd.Y - ptStart.Y)
                    : new Rect2f(ptStart.X, ptStart.Y, ptEnd.X - ptStart.X, ptEnd.Y - ptStart.Y);
                OpenCvSharp.Point2f center = new OpenCvSharp.Point2f(
                    (bounding.X + (bounding.X + bounding.Width)) / 2,
                    (bounding.Y + (bounding.Y + bounding.Height)) / 2);

                return new MatchingResult(0, refinedScore, center, bounding, highestScoreResult.Angle);
            }
        }

        private bool CanUseCoarseMatchAsFinalResult(MatchingResult result)
        {
            return result != null
                && IsLowerScoreBetter(property.MATCH_MODE)
                && Math.Abs(property.MAGNIFIATION - 1.0) < 0.0001
                && Math.Abs(result.Angle) < 0.0001;
        }

        private static void SuppressCoarseMatchRegion(Mat imageSrc, Mat preparedTemplate, Rect roi, MatchingResult result, bool applyRoiOffset)
        {
            int x = (int)Math.Round(result.Bounding.X);
            int y = (int)Math.Round(result.Bounding.Y);
            if (applyRoiOffset)
            {
                x -= roi.X;
                y -= roi.Y;
            }

            Rect maskRect = ClampRectToImage(
                new Rect(
                    new OpenCvSharp.Point(x - 5, y - 5),
                    new OpenCvSharp.Size(preparedTemplate.Width + 10, preparedTemplate.Height + 10)),
                imageSrc);
            if (maskRect.Width > 0 && maskRect.Height > 0)
            {
                SuppressMatchedRegion(imageSrc, maskRect);
            }
        }

        private Rect GetRefineSearchRect(Mat imageSrc, Mat preparedTemplate, Rect roi, MatchingResult coarseResult, bool applyRoiOffset)
        {
            float coarseX = coarseResult.Bounding.X;
            float coarseY = coarseResult.Bounding.Y;
            if (applyRoiOffset)
            {
                coarseX -= roi.X;
                coarseY -= roi.Y;
            }

            int left = (int)Math.Round(coarseX * property.MAGNIFIATION) - preparedTemplate.Width / 2;
            int top = (int)Math.Round(coarseY * property.MAGNIFIATION) - preparedTemplate.Height / 2;
            Rect searchRect = new Rect(
                left,
                top,
                preparedTemplate.Width * 2,
                preparedTemplate.Height * 2);

            Rect clamped = ClampRectToImage(searchRect, imageSrc);
            if (clamped.Width >= preparedTemplate.Width && clamped.Height >= preparedTemplate.Height)
            {
                return clamped;
            }

            return new Rect(0, 0, imageSrc.Width, imageSrc.Height);
        }

        private static void SuppressMatchedRegion(Mat image, Rect maskRect)
        {
            if (image == null || image.Empty())
            {
                return;
            }

            using (Mat patch = image.SubMat(maskRect))
            {
                patch.SetTo(Cv2.Mean(image));

                for (int y = 0; y < patch.Height; y += 4)
                {
                    Scalar color = ((y / 4) % 2 == 0) ? Scalar.Black : Scalar.White;
                    Cv2.Line(patch, new OpenCvSharp.Point(0, y), new OpenCvSharp.Point(patch.Width - 1, y), color, 1);
                }

                for (int x = 0; x < patch.Width; x += 4)
                {
                    Scalar color = ((x / 4) % 2 == 0) ? Scalar.White : Scalar.Black;
                    Cv2.Line(patch, new OpenCvSharp.Point(x, 0), new OpenCvSharp.Point(x, patch.Height - 1), color, 1);
                }
            }
        }

        private static Rect ClampRectToImage(Rect rect, Mat image)
        {
            if (image == null || image.Empty())
            {
                return new Rect();
            }

            int left = Math.Max(0, rect.X);
            int top = Math.Max(0, rect.Y);
            int right = Math.Min(image.Width, rect.X + rect.Width);
            int bottom = Math.Min(image.Height, rect.Y + rect.Height);
            if (right <= left || bottom <= top)
            {
                return new Rect();
            }

            return new Rect(left, top, right - left, bottom - top);
        }

        private bool AddFinalMatchingResult(MatchingResult result)
        {
            if (result == null)
            {
                return false;
            }

            if (IsDuplicateMatchingResult(result))
            {
                return false;
            }

            results.Add(new MatchingResult(
                results.Count + 1,
                result.Score,
                result.Center,
                CommonConverter.RectangleToRect(result.Bounding),
                result.Angle));
            return true;
        }

        private bool IsDuplicateMatchingResult(MatchingResult result)
        {
            double threshold = Math.Max(4, Math.Min(result.Bounding.Width, result.Bounding.Height) * 0.5);
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
    }

}


