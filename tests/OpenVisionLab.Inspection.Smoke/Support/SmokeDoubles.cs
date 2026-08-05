using OpenVisionLab.Inspection;
using OpenVisionLab.Vision2D;
using OpenVisionLab.Vision2D.Pipeline;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;
using OpenVisionLab.Vision3D.FeatureExtraction;
using OpenVisionLab.Vision3D.Geometry;
using OpenVisionLab.Vision3D.Inspection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenVisionLab.Inspection.Smoke
{
    internal sealed class PassThroughVisionTool : IVisionTool
    {
        public string Name => "Pass-through 2D";

        public bool WasExecuted { get; private set; }

        public VisionToolResult Execute(Mat source)
        {
            WasExecuted = true;
            return VisionToolResult.Passed(null, TimeSpan.Zero, new Dictionary<string, double> { { "Executed", 1.0 } });
        }
    }

    internal sealed class FailingVisionTool : IVisionTool
    {
        public string Name => "Failing 2D";

        public bool WasExecuted { get; private set; }

        public VisionToolResult Execute(Mat source)
        {
            WasExecuted = true;
            return VisionToolResult.Failed(VisionToolErrorCode.Unknown, "Controlled 2D failure.", TimeSpan.Zero);
        }
    }

    internal sealed class TrackingDisposableVisionTool : IVisionTool, IDisposable
    {
        public string Name => "Tracking disposable 2D";

        public Mat LastSource { get; private set; }

        public Mat ResultSnapshot { get; private set; }

        public bool WasDisposed { get; private set; }

        public VisionToolResult Execute(Mat source)
        {
            LastSource = source;
            ResultSnapshot = source?.Clone();
            return VisionToolResult.Passed(ResultSnapshot, TimeSpan.Zero);
        }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }

    internal sealed class ThrowingDisposableVisionTool : IVisionTool, IDisposable
    {
        public string Name => "Throwing disposable 2D";

        public Mat LastSource { get; private set; }

        public bool WasDisposed { get; private set; }

        public VisionToolResult Execute(Mat source)
        {
            LastSource = source;
            throw new InvalidOperationException("Controlled pipeline exception.");
        }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }

    internal sealed class ImageReturningVisionTool : IVisionTool
    {
        public string Name => "Image-returning 2D";

        public VisionToolResult Execute(Mat source)
        {
            return VisionToolResult.Passed(source?.Clone(), TimeSpan.Zero);
        }
    }

    internal sealed class SmokeEdgeMatcherProperty : IOpenCVPropertyEdgeBasedTemplateMatching
    {
        public string NAME { get; set; } = "Unique match smoke";
        public double PIXELPERMM { get; set; } = 1D;
        public bool USE_THRESHOLD { get; set; }
        public bool USE_BITWISENOT { get; set; }
        public ThresholdTypes THRESHOLD_TYPES { get; set; } = ThresholdTypes.Binary;
        public double THRESHOLD { get; set; } = 128D;
        public bool USE_ADAPTIVE_THRESHOLD { get; set; }
        public double ADAPTIVE_THRESHOLD { get; set; } = 5D;
        public ThresholdTypes ADAPTIVE_THRESHOLD_TYPES { get; set; } = ThresholdTypes.Binary;
        public AdaptiveThresholdTypes ADAPTIVE_THRESHOLD_ALGORITHM { get; set; } = AdaptiveThresholdTypes.MeanC;
        public int BlockSize { get; set; } = 11;
        public int Weight { get; set; } = 2;
        public bool USE_ROI { get; set; }
        public bool USE_MULTI_ROI { get; set; }
        public Rect CvROI { get; set; }
        public List<Rect> CvROIS { get; set; } = new List<Rect>();
        public List<Rect> CvMASKS { get; set; } = new List<Rect>();
        public double SCORE_MIN { get; set; } = 0.5D;
        public int NUM_MATCH { get; set; } = 1;
        public bool USE_UNIQUE_MATCH_VALIDATION { get; set; }
        public double UNIQUE_MATCH_MIN_SCORE_MARGIN { get; set; } = 0.03D;
        public bool ALLOW_GLOBAL_POLARITY_REVERSAL { get; set; }
        public string PATTERN_PATH { get; set; } = string.Empty;
        public int CANNY_LOW { get; set; } = 30;
        public int CANNY_HIGH { get; set; } = 100;
        public int CANNY_APERTURE_SIZE { get; set; } = 3;
        public bool USE_L2_GRADIENT { get; set; }
        public RetrievalModes CONTOUR_RETRIEVAL_MODE { get; set; } = RetrievalModes.External;
        public ContourApproximationModes CONTOUR_APPROXIMATION_MODE { get; set; } = ContourApproximationModes.ApproxSimple;
        public bool USE_FIND_ANGLE { get; set; }
        public double FIND_ANGLE { get; set; } = 0.5D;
        public int FIND_ANGLE_MAX { get; set; } = 5;
        public int FIND_ANGLE_MIN { get; set; } = -5;
        public bool USE_COARSE_TO_FINE_ANGLE_SEARCH { get; set; }
        public double COARSE_ANGLE_STEP { get; set; } = 2D;
        public int COARSE_ANGLE_TOP_K { get; set; } = 3;
        public bool USE_FIND_SCALE { get; set; }
        public double FIND_SCALE_MIN { get; set; } = 0.9D;
        public double FIND_SCALE_MAX { get; set; } = 1.1D;
        public double FIND_SCALE_STEP { get; set; } = 0.05D;
        public double GREEDINESS { get; set; } = 0.8D;
        public int SEARCH_STEP { get; set; } = 1;
        public bool USE_POSITION_REFINE { get; set; } = true;
        public bool USE_SUBPIXEL_REFINE { get; set; } = true;
        public bool USE_PYRAMID_POSITION_PROPOSAL { get; set; }
        public int PYRAMID_POSITION_TOP_N { get; set; } = 3;
        public double PYRAMID_POSITION_MIN_SCORE { get; set; } = 0.35D;
        public bool USE_HYBRID_VERIFY { get; set; }
        public int HYBRID_VERIFY_TOP_N { get; set; } = 6;
        public double HYBRID_VERIFY_IMAGE_WEIGHT { get; set; } = 0.35D;
        public int MAX_TEMPLATE_POINTS { get; set; } = 500;
        public double MIN_GRADIENT_MAGNITUDE { get; set; } = 5D;
        public bool USE_DRAW_IMAGE { get; set; } = true;
    }

    internal abstract class SmokeOpenCvPropertyBase : IOpenCVPropertyBase
    {
        public string NAME { get; set; } = "Smoke property";
        public double PIXELPERMM { get; set; } = 1d;
        public bool USE_THRESHOLD { get; set; }
        public bool USE_BITWISENOT { get; set; }
        public ThresholdTypes THRESHOLD_TYPES { get; set; } = ThresholdTypes.Binary;
        public double THRESHOLD { get; set; } = 128d;
        public bool USE_ADAPTIVE_THRESHOLD { get; set; }
        public double ADAPTIVE_THRESHOLD { get; set; } = 255d;
        public ThresholdTypes ADAPTIVE_THRESHOLD_TYPES { get; set; } = ThresholdTypes.Binary;
        public AdaptiveThresholdTypes ADAPTIVE_THRESHOLD_ALGORITHM { get; set; } = AdaptiveThresholdTypes.MeanC;
        public int BlockSize { get; set; } = 11;
        public int Weight { get; set; } = 2;
        public bool USE_ROI { get; set; }
        public bool USE_MULTI_ROI { get; set; }
        public Rect CvROI { get; set; }
        public List<Rect> CvROIS { get; set; } = new List<Rect>();
        public List<Rect> CvMASKS { get; set; } = new List<Rect>();
    }

    internal sealed class SmokeMeanProperty : SmokeOpenCvPropertyBase, IOpenCVPropertyMean
    {
        public int MEAN_MAX { get; set; } = 255;
        public int MEAN_MIN { get; set; }
        public MeanType MEAN_TYPES { get; set; } = MeanType.Mean;
    }

    internal sealed class SmokeCornerProperty : SmokeOpenCvPropertyBase, IOpenCVPropertyContour
    {
        public bool USE_APPROXPOLYDP { get; set; }
        public bool USE_DRAW_IMAGE { get; set; } = true;
        public ContourApproximationModes ApproximationModes { get; set; } = ContourApproximationModes.ApproxSimple;
        public RetrievalModes DetectMode { get; set; } = RetrievalModes.External;
        public double EPSILON { get; set; }
        public int MIN_AREA { get; set; }
        public int MAX_AREA { get; set; } = int.MaxValue;
        public System.Drawing.Color DrawColor { get; set; } = System.Drawing.Color.Red;
        public int DrawThickness { get; set; } = 1;
        public string ClrGridHtml { get; set; } = string.Empty;
    }

    internal sealed class ThrowingThreeDTool : IThreeDInspectionTool
    {
        public string Name => "Throwing 3D";

        public ThreeDInspectionResult Execute(HeightMap3D source)
        {
            throw new InvalidOperationException("Controlled 3D exception.");
        }
    }

    internal sealed class ThrowingNameThreeDTool : IThreeDInspectionTool
    {
        public string Name
        {
            get
            {
                throw new InvalidOperationException("Controlled name exception.");
            }
        }

        public ThreeDInspectionResult Execute(HeightMap3D source)
        {
            ThreeDInspectionResult result = ThreeDInspectionResult.CreateMeasurement(source, HeightMapRoi.Full(source), TimeSpan.Zero);
            result.Success = true;
            result.ResultStatus = ThreeDInspectionResultStatus.Passed;
            result.Message = "Controlled success.";
            return result;
        }
    }
}
