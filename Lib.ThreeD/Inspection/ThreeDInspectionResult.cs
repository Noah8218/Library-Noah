using Lib.ThreeD.Geometry;
using System;
using System.Collections.Generic;

namespace Lib.ThreeD.Inspection
{
    public enum ThreeDInspectionErrorCode
    {
        None = 0,
        Unknown = 1,
        InputHeightMapInvalid = 100,
        InputContractMismatch = 105,
        InvalidRoi = 110,
        InvalidParameter = 120,
        InsufficientValidSamples = 130,
        InsufficientValidCoverage = 131,
        DegenerateGeometry = 140,
        ToolNotConfigured = 200,
        ToolExecutionException = 210
    }

    public enum ThreeDInspectionResultStatus
    {
        Passed,
        Failed,
        InvalidInput,
        InvalidParameter,
        InvalidRoi,
        InsufficientData,
        DegenerateGeometry,
        ConfigurationError,
        Exception
    }

    /// <summary>
    /// Derived measurement outcome that distinguishes an out-of-tolerance measurement from a
    /// result where no valid measurement could be produced.
    /// </summary>
    public enum ThreeDMeasurementOutcome
    {
        NotMeasured,
        Passed,
        OutOfTolerance
    }

    public sealed class ThreeDPlaneFit
    {
        public ThreeDPlaneFit(double slopeX, double slopeY, double intercept)
        {
            SlopeX = slopeX;
            SlopeY = slopeY;
            Intercept = intercept;
        }

        public double SlopeX { get; }

        public double SlopeY { get; }

        public double Intercept { get; }

        public double Evaluate(double x, double y)
        {
            return (SlopeX * x) + (SlopeY * y) + Intercept;
        }
    }

    public sealed class ThreeDInspectionResult
    {
        public bool Success { get; set; }

        /// <summary>
        /// True when measurement values were computed, including an out-of-tolerance result.
        /// </summary>
        public bool HasMeasurement { get; set; }

        public ThreeDMeasurementOutcome MeasurementOutcome => !HasMeasurement
            ? ThreeDMeasurementOutcome.NotMeasured
            : Success
                ? ThreeDMeasurementOutcome.Passed
                : ThreeDMeasurementOutcome.OutOfTolerance;

        public string Message { get; set; } = string.Empty;

        public TimeSpan Elapsed { get; set; }

        public Exception Exception { get; set; }

        public ThreeDInspectionErrorCode ErrorCode { get; set; } = ThreeDInspectionErrorCode.None;

        public ThreeDInspectionResultStatus ResultStatus { get; set; } = ThreeDInspectionResultStatus.Passed;

        public string ResultStatusName => ResultStatus.ToString();

        public int ErrorCodeValue => (int)ErrorCode;

        public string ErrorName => ErrorCode.ToString();

        public string SourceId { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public string PlanarUnit { get; set; } = string.Empty;

        public string HeightUnit { get; set; } = string.Empty;

        public string FrameId { get; set; } = string.Empty;

        public string CoordinateConvention { get; set; } = string.Empty;

        public HeightMapRoi? Roi { get; set; }

        public ThreeDPlaneFit PlaneFit { get; set; }

        public long TotalSampleCount { get; set; }

        public long ValidSampleCount { get; set; }

        public long MissingSampleCount { get; set; }

        public double ValidCoverageRatio { get; set; }

        public int MinimumValidSamples { get; set; }

        public double MinimumValidCoverageRatio { get; set; }

        public Dictionary<string, double> Metrics { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> MetricUnits { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Reads a metric and its declared unit in one operation. Returns false when the metric is
        /// absent; a metric without a declared unit returns an empty unit string.
        /// </summary>
        public bool TryGetMetric(string name, out double value, out string unit)
        {
            value = 0.0;
            unit = string.Empty;
            if (string.IsNullOrWhiteSpace(name) || !Metrics.TryGetValue(name, out value))
            {
                return false;
            }

            if (!MetricUnits.TryGetValue(name, out unit) || unit == null)
            {
                unit = string.Empty;
            }

            return true;
        }

        public static ThreeDInspectionResult CreateMeasurement(HeightMap3D source, HeightMapRoi roi, TimeSpan elapsed)
        {
            return new ThreeDInspectionResult
            {
                HasMeasurement = true,
                Elapsed = elapsed,
                SourceId = source == null ? string.Empty : source.SourceId,
                Unit = source == null ? string.Empty : source.Unit,
                PlanarUnit = source == null ? string.Empty : source.PlanarUnit,
                HeightUnit = source == null ? string.Empty : source.HeightUnit,
                FrameId = source == null ? string.Empty : source.FrameId,
                CoordinateConvention = source == null ? string.Empty : source.CoordinateConvention,
                Roi = roi
            };
        }

        public static ThreeDInspectionResult Failed(
            ThreeDInspectionErrorCode errorCode,
            string message,
            TimeSpan elapsed,
            HeightMap3D source = null,
            HeightMapRoi? roi = null,
            Exception exception = null)
        {
            ThreeDInspectionErrorCode resolvedErrorCode = errorCode == ThreeDInspectionErrorCode.None
                ? ThreeDInspectionErrorCode.Unknown
                : errorCode;

            return new ThreeDInspectionResult
            {
                Success = false,
                HasMeasurement = false,
                Message = message ?? string.Empty,
                Elapsed = elapsed,
                Exception = exception,
                ErrorCode = resolvedErrorCode,
                ResultStatus = ResolveStatus(resolvedErrorCode),
                SourceId = source == null ? string.Empty : source.SourceId,
                Unit = source == null ? string.Empty : source.Unit,
                PlanarUnit = source == null ? string.Empty : source.PlanarUnit,
                HeightUnit = source == null ? string.Empty : source.HeightUnit,
                FrameId = source == null ? string.Empty : source.FrameId,
                CoordinateConvention = source == null ? string.Empty : source.CoordinateConvention,
                Roi = roi
            };
        }

        public static ThreeDInspectionResultStatus ResolveStatus(ThreeDInspectionErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ThreeDInspectionErrorCode.None:
                    return ThreeDInspectionResultStatus.Passed;
                case ThreeDInspectionErrorCode.InputHeightMapInvalid:
                case ThreeDInspectionErrorCode.InputContractMismatch:
                    return ThreeDInspectionResultStatus.InvalidInput;
                case ThreeDInspectionErrorCode.InvalidRoi:
                    return ThreeDInspectionResultStatus.InvalidRoi;
                case ThreeDInspectionErrorCode.InvalidParameter:
                    return ThreeDInspectionResultStatus.InvalidParameter;
                case ThreeDInspectionErrorCode.InsufficientValidSamples:
                case ThreeDInspectionErrorCode.InsufficientValidCoverage:
                    return ThreeDInspectionResultStatus.InsufficientData;
                case ThreeDInspectionErrorCode.DegenerateGeometry:
                    return ThreeDInspectionResultStatus.DegenerateGeometry;
                case ThreeDInspectionErrorCode.ToolNotConfigured:
                    return ThreeDInspectionResultStatus.ConfigurationError;
                case ThreeDInspectionErrorCode.ToolExecutionException:
                    return ThreeDInspectionResultStatus.Exception;
                default:
                    return ThreeDInspectionResultStatus.Failed;
            }
        }
    }
}
