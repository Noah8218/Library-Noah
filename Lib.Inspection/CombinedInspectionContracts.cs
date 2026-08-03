using Lib.OpenCV.Tool;
using Lib.ThreeD.Geometry;
using Lib.ThreeD.Inspection;
using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace Lib.Inspection
{
    public enum CombinedInspectionDomain
    {
        TwoD,
        ThreeD
    }

    /// <summary>
    /// The caller retains ownership of Image and HeightMap. The combined runner never disposes them.
    /// </summary>
    public sealed class CombinedInspectionInput
    {
        public Mat Image { get; set; }

        public HeightMap3D HeightMap { get; set; }
    }

    public sealed class CombinedInspectionStepResult
    {
        public CombinedInspectionDomain Domain { get; internal set; }

        public string ToolName { get; internal set; } = string.Empty;

        public VisionToolResult VisionResult { get; internal set; }

        public ThreeDInspectionResult ThreeDResult { get; internal set; }

        public bool Success
        {
            get
            {
                return Domain == CombinedInspectionDomain.TwoD
                    ? VisionResult != null && VisionResult.Success
                    : ThreeDResult != null && ThreeDResult.Success;
            }
        }

        public string Message
        {
            get
            {
                return Domain == CombinedInspectionDomain.TwoD
                    ? VisionResult == null ? string.Empty : VisionResult.Message
                    : ThreeDResult == null ? string.Empty : ThreeDResult.Message;
            }
        }
    }

    /// <summary>
    /// Owns the 2D VisionResult instances collected by the combined runner.
    /// </summary>
    public sealed class CombinedInspectionRunResult : IDisposable
    {
        public bool Success { get; internal set; }

        public string Message { get; internal set; } = string.Empty;

        public TimeSpan Elapsed { get; internal set; }

        public List<CombinedInspectionStepResult> Steps { get; } = new List<CombinedInspectionStepResult>();

        public int FailedStepCount
        {
            get
            {
                int failedStepCount = 0;
                foreach (CombinedInspectionStepResult step in Steps)
                {
                    if (!step.Success)
                    {
                        failedStepCount++;
                    }
                }

                return failedStepCount;
            }
        }

        public void Dispose()
        {
            foreach (CombinedInspectionStepResult step in Steps)
            {
                step?.VisionResult?.Dispose();
            }
        }
    }
}
