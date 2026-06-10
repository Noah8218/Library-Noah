using Lib.OpenCV.Tool;

namespace Lib.OpenCV.Pipeline
{
    public class VisionPipelineStepResult
    {
        public VisionPipelineStep Step { get; set; }
        public VisionToolResult ToolResult { get; set; }
        public bool Skipped { get; set; }
        public bool AcceptancePassed { get; set; } = true;
        public string AcceptanceMessage { get; set; } = string.Empty;
    }
}
