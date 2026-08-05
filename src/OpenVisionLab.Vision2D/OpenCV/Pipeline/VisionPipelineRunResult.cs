using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenVisionLab.Vision2D.Pipeline
{
    /// <summary>
    /// Owns the tool results collected for each pipeline step.
    /// </summary>
    public class VisionPipelineRunResult : IDisposable
    {
        public List<VisionPipelineStepResult> StepResults { get; } = new List<VisionPipelineStepResult>();
        public bool Success => StepResults.All(result =>
            result != null
            && (result.Skipped
                || (result.ToolResult != null && result.ToolResult.Success && result.AcceptancePassed)));

        public void Dispose()
        {
            foreach (VisionPipelineStepResult stepResult in StepResults)
            {
                stepResult?.ToolResult?.Dispose();
            }
        }
    }
}
