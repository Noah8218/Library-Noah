using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lib.OpenCV.Property;
using Lib.OpenCV.Tool;
using OpenCvSharp;
using OpenCvSharp.Blob;

namespace Lib.OpenCV.Blob
{
    public partial class BlobTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyBlob property;
        public List<BlobResult> results = new List<BlobResult>();

        public BlobTool() { }

        public void SetProperty(IOpenCVPropertyBlob propertyBase) => property = propertyBase;

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            if (!TryValidateAreaRange(
                property.MIN_AREA,
                property.MAX_AREA,
                VisionToolErrorCode.BlobInvalidAreaRange,
                "Blob",
                out errorCode,
                out message))
            {
                return false;
            }

            if (!TryValidateAdaptiveThreshold(
                property,
                VisionToolErrorCode.BlobInvalidAdaptiveBlockSize,
                out errorCode,
                out message))
            {
                return false;
            }

            if (!TryValidateRoiSet(
                property,
                property.USE_ROI,
                true,
                VisionToolErrorCode.BlobRoiInvalid,
                "Blob",
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
                errorCode = VisionToolErrorCode.BlobNoResult;
                message = $"Blob found no result. Area={property.MIN_AREA}..{property.MAX_AREA}, ROI={FormatBlobRoi()}";
                return false;
            }

            errorCode = VisionToolErrorCode.None;
            message = string.Empty;
            return true;
        }

        protected override VisionToolErrorCode ResolveExecutionErrorCode(System.Exception exception)
        {
            VisionToolErrorCode baseCode = base.ResolveExecutionErrorCode(exception);
            return baseCode == VisionToolErrorCode.OpenCvExecutionFailed
                ? VisionToolErrorCode.BlobLabelingFailed
                : baseCode;
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

        protected bool SingleRun()
        {
            swTaktTimems.Restart();
            results.Clear();

            if (!PrepareSourceImage())
            {
                return false;
            }

            Rect roi = NormalizeSingleRoi();
            results = ReindexBlobs(RunBlobLabeling(roi, property.USE_ROI)
                .OrderBy(result => result.Index))
                .ToList();

            swTaktTimems.Stop();
            return true;
        }

        protected bool MultiRun()
        {
            swTaktTimems.Restart();
            results.Clear();

            if (!PrepareSourceImage())
            {
                return false;
            }

            for (int i = 0; i < property.CvROIS.Count; i++)
            {
                Rect roi = NormalizeMultiRoi(i);
                results.AddRange(RunBlobLabeling(roi, true)
                    .OrderBy(result => result.Index));
            }

            results = ReindexBlobs(results).ToList();
            swTaktTimems.Stop();
            return true;
        }

        private bool PrepareSourceImage()
        {
            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                return false;
            }

            return true;
        }

        private Rect NormalizeSingleRoi()
        {
            return NormalizeBlobRoi(property.CvROI);
        }

        private Rect NormalizeMultiRoi(int index)
        {
            return NormalizeBlobRoi(property.CvROIS[index]);
        }

        private Rect NormalizeBlobRoi(Rect roi)
        {
            return roi.Width == 0 || roi.Height == 0
                ? new Rect(0, 0, imageSource.Width, imageSource.Height)
                : roi;
        }

        private string FormatBlobRoi()
        {
            if (property.USE_MULTI_ROI)
            {
                return $"Multi({property.CvROIS?.Count ?? 0})";
            }

            Rect roi = NormalizeBlobRoi(property.CvROI);
            return $"{roi.X},{roi.Y},{roi.Width},{roi.Height}";
        }

        private static IEnumerable<BlobResult> ReindexBlobs(IEnumerable<BlobResult> source)
        {
            int index = 1;
            foreach (BlobResult result in source)
            {
                result.Index = index++;
                yield return result;
            }
        }

        private List<BlobResult> RunBlobLabeling(Rect roi, bool useRoi)
        {
            using (Mat imageBlob = CreatePreprocessedImage(roi, useRoi, property))
            {
                CvBlobs blobs = new CvBlobs();
                blobs.Label(imageBlob);
                blobs.FilterByArea(property.MIN_AREA, property.MAX_AREA);

                ConcurrentBag<BlobResult> detectedBlobs = new ConcurrentBag<BlobResult>();
                Parallel.ForEach(blobs, (item, state, index) =>
                {
                    CvBlob blob = item.Value;
                    Rect bounds = useRoi
                        ? new Rect(blob.Rect.X + roi.X, blob.Rect.Y + roi.Y, blob.Rect.Width, blob.Rect.Height)
                        : blob.Rect;
                    Point2d center = useRoi
                        ? new Point2d(blob.Centroid.X + roi.X, blob.Centroid.Y + roi.Y)
                        : blob.Centroid;

                    if (!IsMasked(bounds))
                    {
                        detectedBlobs.Add(new BlobResult((int)index, blob.Area, center, bounds, blob.Angle()));
                    }
                });

                return detectedBlobs.ToList();
            }
        }

        private bool IsMasked(Rect bounds)
        {
            if (property.CvMASKS == null || property.CvMASKS.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < property.CvMASKS.Count; i++)
            {
                if (property.CvMASKS[i].Contains(bounds))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
