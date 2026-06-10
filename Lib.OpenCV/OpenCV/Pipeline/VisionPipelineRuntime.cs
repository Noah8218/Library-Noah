using Lib.OpenCV.Tool;
using OpenCvSharp;
using System;

namespace Lib.OpenCV.Pipeline
{
    public class VisionPipelineRuntime
    {
        private readonly Func<VisionPipelineStep, IVisionTool> toolFactory;

        public VisionPipelineRuntime()
            : this(VisionPipelineToolFactory.Create)
        {
        }

        public VisionPipelineRuntime(Func<VisionPipelineStep, IVisionTool> toolFactory)
        {
            this.toolFactory = toolFactory ?? throw new ArgumentNullException(nameof(toolFactory));
        }

        public VisionPipelineRunResult Run(VisionPipeline pipeline, VisionPipelineContext context)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            VisionPipelineRunResult runResult = new VisionPipelineRunResult();

            foreach (VisionPipelineStep step in pipeline.Steps)
            {
                if (step == null || !step.Enabled)
                {
                    runResult.StepResults.Add(new VisionPipelineStepResult
                    {
                        Step = step,
                        Skipped = true,
                        AcceptancePassed = true,
                        AcceptanceMessage = "Step is disabled."
                    });
                    continue;
                }

                IVisionTool tool = toolFactory(step);
                if (tool == null)
                {
                    throw new InvalidOperationException($"Vision tool factory returned null for step '{step?.Name}'.");
                }

                Mat input = context.GetLayer(step.InputLayer);
                VisionToolResult toolResult = tool.Execute(input);
                VisionPipelineAcceptanceResult acceptance = VisionPipelineAcceptanceEvaluator.Evaluate(step, toolResult);

                runResult.StepResults.Add(new VisionPipelineStepResult
                {
                    Step = step,
                    ToolResult = toolResult,
                    AcceptancePassed = acceptance.Passed,
                    AcceptanceMessage = acceptance.Message
                });

                if (!toolResult.Success || !acceptance.Passed)
                {
                    break;
                }

                context.SetLayer(step.OutputLayer, toolResult.ResultImage);
            }

            return runResult;
        }
    }
}
