using System.Collections.Generic;
using System.Linq;

namespace Lib.OpenCV.Pipeline
{
    public class VisionPipelineRunResult
    {
        public List<VisionPipelineStepResult> StepResults { get; } = new List<VisionPipelineStepResult>();
        public bool Success => StepResults.All(result =>
            result != null
            && (result.Skipped
                || (result.ToolResult != null && result.ToolResult.Success && result.AcceptancePassed)));
    }
}
