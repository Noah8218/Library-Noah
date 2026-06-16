using Lib.OpenCV.Property;
using OpenCvSharp;
using System;

namespace Lib.OpenCV.Tool
{
    public class RotateScaleTool : OpenCvAlgorithmBase
    {
        public IOpenCVPropertyRotateScale property;

        public void SetProperty(IOpenCVPropertyRotateScale property) => this.property = property;

        protected override bool TryValidateBeforeRun(out VisionToolErrorCode errorCode, out string message)
        {
            if (!base.TryValidateBeforeRun(out errorCode, out message))
            {
                return false;
            }

            if (property.ScaleXPercent <= 0 || property.ScaleYPercent <= 0)
            {
                errorCode = VisionToolErrorCode.RotateScaleInvalidScale;
                message = $"RotateScale scale percent must be greater than 0. ScaleX={property.ScaleXPercent}, ScaleY={property.ScaleYPercent}.";
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
                throw new InvalidOperationException("RotateScale property is not configured.");
            }

            if (OpenCvHelper.IsImageEmpty(imageSource))
            {
                throw new InvalidOperationException("Source image is not loaded.");
            }

            imageResult = Transform(
                imageSource,
                property.Angle,
                property.ScaleXPercent,
                property.ScaleYPercent,
                property.Interpolation,
                property.BorderType);
        }

        public static Mat Transform(
            Mat source,
            double angle,
            double scaleXPercent,
            double scaleYPercent,
            InterpolationFlags interpolation,
            BorderTypes borderType)
        {
            if (OpenCvHelper.IsImageEmpty(source))
            {
                throw new ArgumentException("Source image is empty.", nameof(source));
            }

            if (scaleXPercent <= 0 || scaleYPercent <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scaleXPercent), "Scale percent must be greater than 0.");
            }

            double scaleX = scaleXPercent / 100d;
            double scaleY = scaleYPercent / 100d;
            int width = Math.Max(1, (int)Math.Round(source.Width * scaleX));
            int height = Math.Max(1, (int)Math.Round(source.Height * scaleY));

            Mat scaled = new Mat();
            if (width != source.Width || height != source.Height)
            {
                Cv2.Resize(source, scaled, new Size(width, height), 0d, 0d, interpolation);
            }
            else
            {
                scaled = source.Clone();
            }

            if (Math.Abs(angle) < 0.0001d)
            {
                return scaled;
            }

            Mat rotated = new Mat(scaled.Size(), scaled.Type());
            using (Mat matrix = Cv2.GetRotationMatrix2D(new Point2f(scaled.Width / 2f, scaled.Height / 2f), angle, 1d))
            {
                Cv2.WarpAffine(scaled, rotated, matrix, scaled.Size(), interpolation, borderType);
            }

            scaled.Dispose();
            return rotated;
        }
    }
}
