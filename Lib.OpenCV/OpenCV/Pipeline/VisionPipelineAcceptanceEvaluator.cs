using Lib.OpenCV.Tool;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lib.OpenCV.Pipeline
{
    public sealed class VisionPipelineAcceptanceResult
    {
        public bool Passed { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }

    public static class VisionPipelineAcceptanceEvaluator
    {
        public static VisionPipelineAcceptanceResult Evaluate(VisionPipelineStep step, VisionToolResult toolResult)
        {
            if (step == null || !step.UseAcceptance)
            {
                return new VisionPipelineAcceptanceResult();
            }

            List<string> failures = new List<string>();

            bool actualSuccess = toolResult != null && toolResult.Success;
            if (actualSuccess != step.ExpectedSuccess)
            {
                failures.Add($"ExpectedSuccess={step.ExpectedSuccess}, ActualSuccess={actualSuccess}");
            }

            if (step.MaxElapsedMilliseconds > 0 && toolResult != null
                && toolResult.Elapsed.TotalMilliseconds > step.MaxElapsedMilliseconds)
            {
                failures.Add($"Elapsed {toolResult.Elapsed.TotalMilliseconds:0.0} ms > {step.MaxElapsedMilliseconds:0.0} ms");
            }

            if (!string.IsNullOrWhiteSpace(step.RequiredMessageText))
            {
                string message = toolResult?.Message ?? string.Empty;
                if (message.IndexOf(step.RequiredMessageText, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"Message does not contain '{step.RequiredMessageText}'");
                }
            }

            if (!string.IsNullOrWhiteSpace(step.AcceptanceMetricName)
                && (step.UseAcceptanceMetricMinimum || step.UseAcceptanceMetricMaximum))
            {
                if (toolResult == null || !toolResult.Metrics.TryGetValue(step.AcceptanceMetricName, out double metricValue))
                {
                    failures.Add($"Metric '{step.AcceptanceMetricName}' was not produced");
                }
                else
                {
                    if (step.UseAcceptanceMetricMinimum && metricValue < step.AcceptanceMetricMinimum)
                    {
                        failures.Add($"{step.AcceptanceMetricName} {metricValue:0.###} < {step.AcceptanceMetricMinimum:0.###}");
                    }

                    if (step.UseAcceptanceMetricMaximum && metricValue > step.AcceptanceMetricMaximum)
                    {
                        failures.Add($"{step.AcceptanceMetricName} {metricValue:0.###} > {step.AcceptanceMetricMaximum:0.###}");
                    }
                }
            }

            if (failures.Count == 0)
            {
                return new VisionPipelineAcceptanceResult
                {
                    Passed = true,
                    Message = "Acceptance passed."
                };
            }

            return new VisionPipelineAcceptanceResult
            {
                Passed = false,
                Message = string.Join("; ", failures.Distinct())
            };
        }
    }
}
