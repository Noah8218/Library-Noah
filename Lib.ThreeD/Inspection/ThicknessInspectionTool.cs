using Lib.ThreeD.Geometry;
using System;
using System.Diagnostics;

namespace Lib.ThreeD.Inspection
{
    /// <summary>
    /// Evaluates declared height-map scalar values as thickness. The caller owns the data meaning,
    /// calibration, unit, and reference-plane definition.
    /// </summary>
    public sealed class ThicknessInspectionTool : IThreeDInspectionTool
    {
        public ThicknessInspectionTool()
            : this(new ThicknessInspectionOptions())
        {
        }

        public ThicknessInspectionTool(ThicknessInspectionOptions options)
        {
            Options = options;
        }

        public string Name => "Thickness";

        public ThicknessInspectionOptions Options { get; }

        public ThreeDInspectionResult Execute(HeightMap3D source)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                if (source == null)
                {
                    return Failure(ThreeDInspectionErrorCode.InputHeightMapInvalid, "A height map is required.", stopwatch);
                }

                if (Options == null)
                {
                    return Failure(ThreeDInspectionErrorCode.InvalidParameter, "Thickness options are required.", stopwatch, source);
                }

                if (!ThreeDInspectionMath.IsFinite(Options.MinimumThickness)
                    || !ThreeDInspectionMath.IsFinite(Options.MaximumThickness)
                    || Options.MinimumThickness > Options.MaximumThickness
                    || Options.MinimumValidSamples <= 0)
                {
                    return Failure(ThreeDInspectionErrorCode.InvalidParameter, "Thickness limits or minimum sample count are invalid.", stopwatch, source);
                }

                HeightMapRoi roi = ThreeDInspectionMath.ResolveRoi(source, Options.Roi);
                if (!roi.IsValidFor(source))
                {
                    return Failure(ThreeDInspectionErrorCode.InvalidRoi, "The thickness ROI is outside the height map.", stopwatch, source, roi);
                }

                long validSampleCount = 0;
                long belowLowerLimitCount = 0;
                long aboveUpperLimitCount = 0;
                double minimum = double.PositiveInfinity;
                double maximum = double.NegativeInfinity;
                double mean = 0.0;

                for (int row = roi.Row; row < roi.Row + roi.RowCount; row++)
                {
                    for (int column = roi.Column; column < roi.Column + roi.ColumnCount; column++)
                    {
                        double value = source.GetHeight(row, column);
                        if (double.IsNaN(value))
                        {
                            continue;
                        }

                        if (!ThreeDInspectionMath.IsFinite(value))
                        {
                            return Failure(ThreeDInspectionErrorCode.InputHeightMapInvalid, "The height map contains a non-finite sample.", stopwatch, source, roi);
                        }

                        validSampleCount++;
                        mean += (value - mean) / validSampleCount;
                        minimum = Math.Min(minimum, value);
                        maximum = Math.Max(maximum, value);

                        if (value < Options.MinimumThickness)
                        {
                            belowLowerLimitCount++;
                        }

                        if (value > Options.MaximumThickness)
                        {
                            aboveUpperLimitCount++;
                        }
                    }
                }

                if (validSampleCount < Options.MinimumValidSamples)
                {
                    return Failure(
                        ThreeDInspectionErrorCode.InsufficientValidSamples,
                        "The thickness ROI does not contain enough finite samples.",
                        stopwatch,
                        source,
                        roi);
                }

                stopwatch.Stop();
                ThreeDInspectionResult result = ThreeDInspectionResult.CreateMeasurement(source, roi, stopwatch.Elapsed);
                result.Metrics["ValidSampleCount"] = validSampleCount;
                result.Metrics["Minimum"] = minimum;
                result.Metrics["Maximum"] = maximum;
                result.Metrics["Mean"] = mean;
                result.Metrics["Range"] = maximum - minimum;
                result.Metrics["LowerLimit"] = Options.MinimumThickness;
                result.Metrics["UpperLimit"] = Options.MaximumThickness;
                result.Metrics["BelowLowerLimitCount"] = belowLowerLimitCount;
                result.Metrics["AboveUpperLimitCount"] = aboveUpperLimitCount;

                if (belowLowerLimitCount == 0 && aboveUpperLimitCount == 0)
                {
                    result.Success = true;
                    result.ResultStatus = ThreeDInspectionResultStatus.Passed;
                    result.Message = "All valid thickness samples are within tolerance.";
                }
                else
                {
                    result.Success = false;
                    result.ResultStatus = ThreeDInspectionResultStatus.Failed;
                    result.Message = "One or more valid thickness samples are outside tolerance.";
                }

                return result;
            }
            catch (Exception exception)
            {
                return Failure(
                    ThreeDInspectionErrorCode.ToolExecutionException,
                    "Thickness inspection failed with an unexpected exception.",
                    stopwatch,
                    source,
                    null,
                    exception);
            }
        }

        private static ThreeDInspectionResult Failure(
            ThreeDInspectionErrorCode errorCode,
            string message,
            Stopwatch stopwatch,
            HeightMap3D source = null,
            HeightMapRoi? roi = null,
            Exception exception = null)
        {
            stopwatch.Stop();
            return ThreeDInspectionResult.Failed(errorCode, message, stopwatch.Elapsed, source, roi, exception);
        }
    }
}
