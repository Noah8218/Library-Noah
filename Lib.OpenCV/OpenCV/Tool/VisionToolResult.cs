using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lib.OpenCV.Tool
{
    public class VisionToolResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Mat ResultImage { get; set; }
        public TimeSpan Elapsed { get; set; }
        public Exception Exception { get; set; }
        public Dictionary<string, double> Metrics { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        public List<VisionToolOverlay> Overlays { get; } = new List<VisionToolOverlay>();

        public static VisionToolResult Passed(
            Mat resultImage,
            TimeSpan elapsed,
            IDictionary<string, double> metrics = null,
            IEnumerable<VisionToolOverlay> overlays = null)
        {
            VisionToolResult result = new VisionToolResult
            {
                Success = true,
                ResultImage = resultImage,
                Elapsed = elapsed
            };

            if (metrics != null)
            {
                foreach (KeyValuePair<string, double> metric in metrics)
                {
                    if (string.IsNullOrWhiteSpace(metric.Key)) { continue; }
                    result.Metrics[metric.Key] = metric.Value;
                }
            }

            if (overlays != null)
            {
                result.Overlays.AddRange(overlays.Where(overlay => overlay != null));
            }

            return result;
        }

        public static VisionToolResult Failed(string message, TimeSpan elapsed, Exception exception = null)
        {
            return new VisionToolResult
            {
                Success = false,
                Message = message ?? string.Empty,
                Elapsed = elapsed,
                Exception = exception
            };
        }
    }
}
