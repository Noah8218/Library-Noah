using System;
using System.Collections.Generic;
using System.Linq;

namespace Lib.ThreeD.FeatureExtraction
{
    public enum AcquisitionDirectionOrientation
    {
        SensorFacing = 0,
        AwayFromSensor = 1,
        Grazing = 2
    }

    public sealed class AcquisitionDirectionNormalInput
    {
        public AcquisitionDirectionNormalInput(
            int sourceOrder,
            ThreeDPoint normal)
        {
            SourceOrder = sourceOrder;
            Normal = normal;
        }

        public int SourceOrder { get; }
        public ThreeDPoint Normal { get; }
    }

    public sealed class AcquisitionDirectionOrientationItem
    {
        internal AcquisitionDirectionOrientationItem(
            int sourceOrder,
            ThreeDPoint normalizedNormal,
            double alignmentCosine,
            AcquisitionDirectionOrientation orientation)
        {
            SourceOrder = sourceOrder;
            NormalizedNormal = normalizedNormal;
            AlignmentCosine = alignmentCosine;
            Orientation = orientation;
        }

        public int SourceOrder { get; }
        public ThreeDPoint NormalizedNormal { get; }
        public double AlignmentCosine { get; }
        public AcquisitionDirectionOrientation Orientation { get; }
    }

    public sealed class AcquisitionDirectionOrientationOptions
    {
        public double GrazingAbsoluteCosineMaximum { get; set; }
    }

    public sealed class AcquisitionDirectionOrientationResult
    {
        private AcquisitionDirectionOrientationResult(
            bool success,
            string message,
            ThreeDPoint normalizedSensorToSceneDirection,
            IReadOnlyList<AcquisitionDirectionOrientationItem> items)
        {
            Success = success;
            Message = message ?? string.Empty;
            NormalizedSensorToSceneDirection =
                normalizedSensorToSceneDirection;
            Items = items ?? Array.Empty<AcquisitionDirectionOrientationItem>();
        }

        public bool Success { get; }
        public string Message { get; }
        public ThreeDPoint NormalizedSensorToSceneDirection { get; }
        public IReadOnlyList<AcquisitionDirectionOrientationItem> Items { get; }

        internal static AcquisitionDirectionOrientationResult Completed(
            ThreeDPoint normalizedSensorToSceneDirection,
            IReadOnlyList<AcquisitionDirectionOrientationItem> items)
        {
            return new AcquisitionDirectionOrientationResult(
                true,
                string.Empty,
                normalizedSensorToSceneDirection,
                items);
        }

        internal static AcquisitionDirectionOrientationResult Failed(
            string message)
        {
            return new AcquisitionDirectionOrientationResult(
                false,
                message,
                null,
                Array.Empty<AcquisitionDirectionOrientationItem>());
        }
    }

    /// <summary>
    /// Classifies declared normals against an explicit sensor-to-scene
    /// direction. The Tool normalizes finite non-zero vectors and never
    /// infers a viewpoint, coordinate frame, camera pose, or visibility.
    /// </summary>
    public sealed class AcquisitionDirectionOrientationTool
    {
        private const double ClassificationBoundaryTolerance = 1e-12;

        public const string Semantics =
            "sensor-to-scene-normal-orientation-v1";

        public AcquisitionDirectionOrientationResult Execute(
            ThreeDPoint sensorToSceneDirection,
            IReadOnlyList<AcquisitionDirectionNormalInput> normals,
            AcquisitionDirectionOrientationOptions options)
        {
            try
            {
                Validate(sensorToSceneDirection, normals, options);
                ThreeDPoint direction = Normalize(sensorToSceneDirection);
                AcquisitionDirectionOrientationItem[] items = normals
                    .OrderBy(input => input.SourceOrder)
                    .Select(input => Classify(input, direction, options))
                    .ToArray();
                return AcquisitionDirectionOrientationResult.Completed(
                    direction,
                    items);
            }
            catch (Exception exception)
            {
                return AcquisitionDirectionOrientationResult.Failed(
                    "Acquisition direction orientation failed: "
                    + exception.Message);
            }
        }

        private static AcquisitionDirectionOrientationItem Classify(
            AcquisitionDirectionNormalInput input,
            ThreeDPoint direction,
            AcquisitionDirectionOrientationOptions options)
        {
            ThreeDPoint normal = Normalize(input.Normal);
            double alignment = Math.Max(
                -1.0,
                Math.Min(1.0, Dot(normal, direction)));
            AcquisitionDirectionOrientation orientation =
                Math.Abs(alignment)
                    <= options.GrazingAbsoluteCosineMaximum
                       + ClassificationBoundaryTolerance
                    ? AcquisitionDirectionOrientation.Grazing
                    : alignment < 0.0
                        ? AcquisitionDirectionOrientation.SensorFacing
                        : AcquisitionDirectionOrientation.AwayFromSensor;
            return new AcquisitionDirectionOrientationItem(
                input.SourceOrder,
                normal,
                alignment,
                orientation);
        }

        private static void Validate(
            ThreeDPoint sensorToSceneDirection,
            IReadOnlyList<AcquisitionDirectionNormalInput> normals,
            AcquisitionDirectionOrientationOptions options)
        {
            if (!CanNormalize(sensorToSceneDirection))
            {
                throw new ArgumentException(
                    "Sensor-to-scene direction must be finite and non-zero.");
            }

            if (normals == null || normals.Count == 0)
            {
                throw new ArgumentException(
                    "At least one declared normal is required.");
            }

            if (options == null
                || !IsFinite(options.GrazingAbsoluteCosineMaximum)
                || options.GrazingAbsoluteCosineMaximum < 0.0
                || options.GrazingAbsoluteCosineMaximum >= 1.0)
            {
                throw new ArgumentException(
                    "Grazing absolute cosine maximum must be finite in [0, 1).",
                    nameof(options));
            }

            HashSet<int> sourceOrders = new HashSet<int>();
            for (int index = 0; index < normals.Count; index++)
            {
                AcquisitionDirectionNormalInput input = normals[index];
                if (input == null
                    || input.SourceOrder < 0
                    || !sourceOrders.Add(input.SourceOrder)
                    || !CanNormalize(input.Normal))
                {
                    throw new ArgumentException(
                        "Declared normals require unique non-negative orders and finite non-zero vectors.");
                }
            }
        }

        private static bool CanNormalize(ThreeDPoint value)
        {
            if (value == null || !value.IsFinite)
            {
                return false;
            }

            double scale = Math.Max(
                Math.Abs(value.X),
                Math.Max(Math.Abs(value.Y), Math.Abs(value.Z)));
            return IsFinite(scale) && scale > 0.0;
        }

        private static ThreeDPoint Normalize(ThreeDPoint value)
        {
            double scale = Math.Max(
                Math.Abs(value.X),
                Math.Max(Math.Abs(value.Y), Math.Abs(value.Z)));
            double x = value.X / scale;
            double y = value.Y / scale;
            double z = value.Z / scale;
            double length = Math.Sqrt(x * x + y * y + z * z);
            return new ThreeDPoint(x / length, y / length, z / length);
        }

        private static double Dot(ThreeDPoint first, ThreeDPoint second)
        {
            return first.X * second.X
                + first.Y * second.Y
                + first.Z * second.Z;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
