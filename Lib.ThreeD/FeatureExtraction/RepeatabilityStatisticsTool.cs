using System;
using System.Collections.Generic;

namespace Lib.ThreeD.FeatureExtraction
{
    public enum RepeatabilityNegativeVariancePolicy
    {
        ClampNearZero = 0,
        ClampAnyNegative = 1
    }

    public sealed class RepeatabilityStatisticsResult
    {
        internal RepeatabilityStatisticsResult(
            bool success,
            string message,
            int count,
            double mean,
            double minimum,
            double maximum,
            double sampleStandardDeviation,
            double sixSigmaSpread,
            double range)
        {
            Success = success;
            Message = message;
            Count = count;
            Mean = mean;
            Minimum = minimum;
            Maximum = maximum;
            SampleStandardDeviation = sampleStandardDeviation;
            SixSigmaSpread = sixSigmaSpread;
            Range = range;
        }

        public bool Success { get; }

        public string Message { get; }

        public int Count { get; }

        public double Mean { get; }

        public double Minimum { get; }

        public double Maximum { get; }

        public double SampleStandardDeviation { get; }

        public double SixSigmaSpread { get; }

        public double Range { get; }
    }

    /// <summary>
    /// Calculates source-neutral descriptive statistics for a repeated scalar
    /// measurement. The tool does not own study identity, unit/frame policy,
    /// acceptance limits, Gauge R&amp;R claims, or product decisions.
    /// </summary>
    public sealed class RepeatabilityStatisticsTool
    {
        private const double NegativeVarianceTolerance = -1e-12;

        public RepeatabilityStatisticsResult Execute(
            IReadOnlyList<double> values,
            RepeatabilityNegativeVariancePolicy negativeVariancePolicy =
                RepeatabilityNegativeVariancePolicy.ClampNearZero)
        {
            if (values == null)
            {
                return Error(0, "Repeatability values are required.");
            }

            if (values.Count < 2)
            {
                return Error(values.Count, "At least two values are required for sample standard deviation.");
            }

            if (negativeVariancePolicy != RepeatabilityNegativeVariancePolicy.ClampNearZero
                && negativeVariancePolicy != RepeatabilityNegativeVariancePolicy.ClampAnyNegative)
            {
                return Error(values.Count, "Negative-variance policy is not supported.");
            }

            int count = 0;
            double mean = 0.0;
            double sumSquaredDelta = 0.0;
            double minimum = double.PositiveInfinity;
            double maximum = double.NegativeInfinity;
            for (int index = 0; index < values.Count; index++)
            {
                double value = values[index];
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return Error(values.Count, "Repeatability values must be finite.");
                }

                count++;
                double delta = value - mean;
                mean += delta / count;
                sumSquaredDelta += delta * (value - mean);
                minimum = Math.Min(minimum, value);
                maximum = Math.Max(maximum, value);
            }

            if (!IsFinite(sumSquaredDelta))
            {
                return Error(values.Count, "Repeatability statistics produced a non-finite value or overflow.");
            }

            double variance = sumSquaredDelta / (count - 1);
            if (variance < 0.0
                && (negativeVariancePolicy == RepeatabilityNegativeVariancePolicy.ClampAnyNegative
                    || variance > NegativeVarianceTolerance))
            {
                variance = 0.0;
            }

            double sampleStandardDeviation = Math.Sqrt(variance);
            double sixSigmaSpread = 6.0 * sampleStandardDeviation;
            double range = maximum - minimum;
            if (!IsFinite(mean)
                || !IsFinite(minimum)
                || !IsFinite(maximum)
                || !IsFinite(sampleStandardDeviation)
                || !IsFinite(sixSigmaSpread)
                || !IsFinite(range))
            {
                return Error(values.Count, "Repeatability statistics produced a non-finite value or overflow.");
            }

            return new RepeatabilityStatisticsResult(
                true,
                "Repeatability statistics calculated.",
                count,
                mean,
                minimum,
                maximum,
                sampleStandardDeviation,
                sixSigmaSpread,
                range);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static RepeatabilityStatisticsResult Error(int count, string message)
        {
            return new RepeatabilityStatisticsResult(
                false,
                message,
                count,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN);
        }
    }
}
