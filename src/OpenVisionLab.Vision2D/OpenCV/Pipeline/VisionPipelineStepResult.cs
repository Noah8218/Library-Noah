using OpenVisionLab.Vision2D.Tool;

namespace OpenVisionLab.Vision2D.Pipeline
{
    /// <summary>Contains the tool and acceptance outcome for one pipeline step.</summary>
    public class VisionPipelineStepResult
    {
        public VisionPipelineStep Step { get; set; }
        public VisionToolResult ToolResult { get; set; }
        public bool Skipped { get; set; }
        public bool AcceptancePassed { get; set; } = true;
        public string AcceptanceMessage { get; set; } = string.Empty;
        /// <summary>
        /// Gets whether the step was skipped or satisfied its tool/acceptance contract.
        /// </summary>
        public bool Success => Skipped
            || (ToolResult != null
                && AcceptancePassed
                && (Step?.UseAcceptance == true || ToolResult.Success));
    }
}
