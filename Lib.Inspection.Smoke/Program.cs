using Lib.Inspection;
using Lib.OpenCV.Tool;
using Lib.ThreeD.Geometry;
using Lib.ThreeD.Inspection;
using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace Lib.Inspection.Smoke
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            int passed = 0;
            int total = 0;

            try
            {
                Run("Thickness pass preserves declared metadata", TestThicknessPass, ref passed, ref total);
                Run("Thickness tolerance failure retains measurement", TestThicknessToleranceFailure, ref passed, ref total);
                Run("Thickness rejects an invalid ROI", TestThicknessInvalidRoi, ref passed, ref total);
                Run("Thickness rejects insufficient valid samples", TestThicknessInsufficientSamples, ref passed, ref total);
                Run("Warpage fits an analytic plane", TestWarpageAnalyticPlane, ref passed, ref total);
                Run("Warpage tolerance failure retains measurement", TestWarpageToleranceFailure, ref passed, ref total);
                Run("Warpage rejects insufficient valid samples", TestWarpageInsufficientSamples, ref passed, ref total);
                Run("Warpage rejects collinear geometry", TestWarpageDegenerateGeometry, ref passed, ref total);
                Run("Warpage rejects an invalid limit", TestWarpageInvalidParameter, ref passed, ref total);
                Run("Combined runner executes 2D and 3D pass steps", TestCombinedRunnerPass, ref passed, ref total);
                Run("Combined runner retains later 3D evidence after 2D failure", TestCombinedRunnerContinuesAfterFailure, ref passed, ref total);
                Run("Combined runner converts a 3D exception to a result", TestCombinedRunnerCatchesThreeDException, ref passed, ref total);
                Run("Combined runner tolerates a throwing tool name", TestCombinedRunnerToleratesThrowingToolName, ref passed, ref total);
                Run("Combined runner rejects an empty configuration", TestCombinedRunnerEmptyConfiguration, ref passed, ref total);

                Console.WriteLine("Lib.Inspection.Smoke | " + passed + "/" + total + " passed");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL | " + exception.Message);
                Console.Error.WriteLine(exception);
                Console.Error.WriteLine("Lib.Inspection.Smoke | " + passed + "/" + total + " passed before failure");
                return 1;
            }
        }

        private static void Run(string name, Action test, ref int passed, ref int total)
        {
            total++;
            test();
            passed++;
            Console.WriteLine("PASS | " + name);
        }

        private static void TestThicknessPass()
        {
            HeightMap3D map = new HeightMap3D(
                2,
                3,
                0.0,
                0.0,
                1.0,
                1.0,
                new[] { 1.0, 1.1, 1.2, 1.3, double.NaN, 1.4 },
                "mm",
                "sensor-top",
                "sample-thickness");
            ThicknessInspectionTool tool = new ThicknessInspectionTool(new ThicknessInspectionOptions
            {
                MinimumThickness = 1.0,
                MaximumThickness = 1.5,
                MinimumValidSamples = 5
            });

            ThreeDInspectionResult result = tool.Execute(map);

            Require(result.Success, "Thickness pass must succeed.");
            Require(result.HasMeasurement, "Thickness pass must contain a measurement.");
            Require(result.Unit == "mm" && result.FrameId == "sensor-top" && result.SourceId == "sample-thickness", "Declared map metadata was not preserved.");
            RequireApproximately(result.Metrics["ValidSampleCount"], 5.0, 0.0, "Unexpected thickness valid sample count.");
            RequireApproximately(result.Metrics["Mean"], 1.2, 1e-12, "Unexpected thickness mean.");
            RequireApproximately(result.Metrics["Range"], 0.4, 1e-12, "Unexpected thickness range.");
        }

        private static void TestThicknessToleranceFailure()
        {
            ThicknessInspectionTool tool = new ThicknessInspectionTool(new ThicknessInspectionOptions
            {
                MinimumThickness = 1.0,
                MaximumThickness = 1.25
            });

            ThreeDInspectionResult result = tool.Execute(CreateThicknessMap());

            Require(!result.Success, "Out-of-tolerance thickness must fail.");
            Require(result.HasMeasurement, "Out-of-tolerance thickness must retain the measurement.");
            Require(result.ResultStatus == ThreeDInspectionResultStatus.Failed, "Out-of-tolerance thickness must be a failed measurement, not an input error.");
            RequireApproximately(result.Metrics["AboveUpperLimitCount"], 2.0, 0.0, "Unexpected thickness upper-limit count.");
        }

        private static void TestThicknessInvalidRoi()
        {
            ThicknessInspectionTool tool = new ThicknessInspectionTool(new ThicknessInspectionOptions
            {
                MinimumThickness = 0.0,
                MaximumThickness = 10.0,
                Roi = new HeightMapRoi(2, 0, 1, 1)
            });

            ThreeDInspectionResult result = tool.Execute(CreateThicknessMap());

            Require(result.ResultStatus == ThreeDInspectionResultStatus.InvalidRoi, "Invalid thickness ROI must be rejected.");
        }

        private static void TestThicknessInsufficientSamples()
        {
            HeightMap3D map = new HeightMap3D(
                1,
                3,
                0.0,
                0.0,
                1.0,
                1.0,
                new[] { 1.0, double.NaN, double.NaN });
            ThicknessInspectionTool tool = new ThicknessInspectionTool(new ThicknessInspectionOptions
            {
                MinimumThickness = 0.0,
                MaximumThickness = 2.0,
                MinimumValidSamples = 2
            });

            ThreeDInspectionResult result = tool.Execute(map);

            Require(result.ResultStatus == ThreeDInspectionResultStatus.InsufficientData, "Insufficient thickness samples must be rejected.");
        }

        private static void TestWarpageAnalyticPlane()
        {
            HeightMap3D map = CreatePlaneMap(3, 3, 0.5, -0.25, 2.0);
            WarpageInspectionTool tool = new WarpageInspectionTool(new WarpageInspectionOptions
            {
                MaximumPeakToValley = 1e-10,
                MaximumRms = 1e-10,
                MinimumValidSamples = 9
            });

            ThreeDInspectionResult result = tool.Execute(map);

            Require(result.Success, "An analytic plane must pass warpage inspection.");
            Require(result.PlaneFit != null, "Warpage must expose the fitted plane.");
            RequireApproximately(result.PlaneFit.SlopeX, 0.5, 1e-12, "Unexpected warpage X slope.");
            RequireApproximately(result.PlaneFit.SlopeY, -0.25, 1e-12, "Unexpected warpage Y slope.");
            RequireApproximately(result.PlaneFit.Intercept, 2.0, 1e-12, "Unexpected warpage intercept.");
            RequireApproximately(result.Metrics["PeakToValley"], 0.0, 1e-10, "Unexpected analytic-plane peak-to-valley.");
        }

        private static void TestWarpageToleranceFailure()
        {
            HeightMap3D map = new HeightMap3D(
                3,
                3,
                0.0,
                0.0,
                1.0,
                1.0,
                new[]
                {
                    0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0
                });
            WarpageInspectionTool tool = new WarpageInspectionTool(new WarpageInspectionOptions
            {
                MaximumPeakToValley = 0.1,
                MaximumRms = 0.1
            });

            ThreeDInspectionResult result = tool.Execute(map);

            Require(!result.Success, "Non-planar data must fail the tight warpage limit.");
            Require(result.HasMeasurement, "Out-of-tolerance warpage must retain the measurement.");
            Require(result.Metrics["PeakToValley"] > 0.1, "Expected a measurable warpage peak-to-valley.");
        }

        private static void TestWarpageInsufficientSamples()
        {
            HeightMap3D map = new HeightMap3D(
                2,
                2,
                0.0,
                0.0,
                1.0,
                1.0,
                new[] { double.NaN, double.NaN, double.NaN, double.NaN });
            WarpageInspectionTool tool = new WarpageInspectionTool(new WarpageInspectionOptions
            {
                MaximumPeakToValley = 1.0
            });

            ThreeDInspectionResult result = tool.Execute(map);

            Require(result.ResultStatus == ThreeDInspectionResultStatus.InsufficientData, "Warpage must reject empty finite data.");
        }

        private static void TestWarpageDegenerateGeometry()
        {
            HeightMap3D map = new HeightMap3D(
                1,
                3,
                0.0,
                0.0,
                1.0,
                1.0,
                new[] { 0.0, 1.0, 2.0 });
            WarpageInspectionTool tool = new WarpageInspectionTool(new WarpageInspectionOptions
            {
                MaximumPeakToValley = 1.0
            });

            ThreeDInspectionResult result = tool.Execute(map);

            Require(result.ResultStatus == ThreeDInspectionResultStatus.DegenerateGeometry, "Collinear warpage data must be rejected.");
        }

        private static void TestWarpageInvalidParameter()
        {
            WarpageInspectionTool tool = new WarpageInspectionTool(new WarpageInspectionOptions
            {
                MaximumPeakToValley = -0.1
            });

            ThreeDInspectionResult result = tool.Execute(CreatePlaneMap(2, 2, 0.0, 0.0, 1.0));

            Require(result.ResultStatus == ThreeDInspectionResultStatus.InvalidParameter, "Negative warpage limit must be rejected.");
        }

        private static void TestCombinedRunnerPass()
        {
            PassThroughVisionTool twoDTool = new PassThroughVisionTool();
            CombinedInspectionRunResult result = new CombinedInspectionRunner().Run(
                new CombinedInspectionInput { HeightMap = CreatePlaneMap(3, 3, 0.0, 0.0, 1.0) },
                new IVisionTool[] { twoDTool },
                new IThreeDInspectionTool[]
                {
                    new ThicknessInspectionTool(new ThicknessInspectionOptions
                    {
                        MinimumThickness = 0.0,
                        MaximumThickness = 2.0
                    }),
                    new WarpageInspectionTool(new WarpageInspectionOptions
                    {
                        MaximumPeakToValley = 1e-10,
                        MaximumRms = 1e-10
                    })
                });

            Require(twoDTool.WasExecuted, "The combined runner did not execute the 2D tool.");
            Require(result.Success, "All passing combined steps must produce a passing run.");
            Require(result.Steps.Count == 3, "The combined runner did not preserve every step result.");
            Require(result.Steps[2].Domain == CombinedInspectionDomain.ThreeD && result.Steps[2].Success, "The combined runner did not preserve the 3D result.");
        }

        private static void TestCombinedRunnerContinuesAfterFailure()
        {
            FailingVisionTool twoDTool = new FailingVisionTool();
            CombinedInspectionRunResult result = new CombinedInspectionRunner().Run(
                new CombinedInspectionInput { HeightMap = CreateThicknessMap() },
                new IVisionTool[] { twoDTool },
                new IThreeDInspectionTool[]
                {
                    new ThicknessInspectionTool(new ThicknessInspectionOptions
                    {
                        MinimumThickness = 1.0,
                        MaximumThickness = 1.5
                    })
                });

            Require(twoDTool.WasExecuted, "The failure fixture was not executed.");
            Require(!result.Success, "A failed 2D step must fail the combined run.");
            Require(result.Steps.Count == 2, "The combined runner must continue after a failed 2D step.");
            Require(result.Steps[1].Success, "The later 3D evidence was not retained.");
        }

        private static void TestCombinedRunnerCatchesThreeDException()
        {
            CombinedInspectionRunResult result = new CombinedInspectionRunner().Run(
                new CombinedInspectionInput { HeightMap = CreateThicknessMap() },
                null,
                new IThreeDInspectionTool[] { new ThrowingThreeDTool() });

            Require(!result.Success, "A throwing 3D tool must fail the combined run.");
            Require(result.Steps.Count == 1, "The throwing 3D tool did not produce a controlled result.");
            Require(result.Steps[0].ThreeDResult.ResultStatus == ThreeDInspectionResultStatus.Exception, "The 3D exception did not map to an exception result.");
        }

        private static void TestCombinedRunnerToleratesThrowingToolName()
        {
            CombinedInspectionRunResult result = new CombinedInspectionRunner().Run(
                new CombinedInspectionInput { HeightMap = CreateThicknessMap() },
                null,
                new IThreeDInspectionTool[] { new ThrowingNameThreeDTool() });

            Require(result.Success, "A successful tool with a throwing Name getter must still execute.");
            Require(result.Steps.Count == 1, "The tool with a throwing Name getter did not produce a result.");
            Require(result.Steps[0].ToolName == "Unnamed 3D tool", "The throwing Name getter did not use the stable fallback label.");
        }

        private static void TestCombinedRunnerEmptyConfiguration()
        {
            CombinedInspectionRunResult result = new CombinedInspectionRunner().Run(new CombinedInspectionInput(), null, null);

            Require(!result.Success, "An empty combined configuration must not pass.");
            Require(result.Steps.Count == 0, "An empty combined configuration must not create synthetic tool results.");
        }

        private static HeightMap3D CreateThicknessMap()
        {
            return new HeightMap3D(
                2,
                3,
                0.0,
                0.0,
                1.0,
                1.0,
                new[] { 1.0, 1.1, 1.2, 1.3, double.NaN, 1.4 },
                "mm",
                "sensor-top",
                "sample-thickness");
        }

        private static HeightMap3D CreatePlaneMap(int rows, int columns, double slopeX, double slopeY, double intercept)
        {
            double[] values = new double[rows * columns];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    values[(row * columns) + column] = (slopeX * column) + (slopeY * row) + intercept;
                }
            }

            return new HeightMap3D(rows, columns, 0.0, 0.0, 1.0, 1.0, values, "mm", "fixture", "analytic-plane");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void RequireApproximately(double actual, double expected, double tolerance, string message)
        {
            if (Math.Abs(actual - expected) > tolerance)
            {
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
            }
        }

        private sealed class PassThroughVisionTool : IVisionTool
        {
            public string Name => "Pass-through 2D";

            public bool WasExecuted { get; private set; }

            public VisionToolResult Execute(Mat source)
            {
                WasExecuted = true;
                return VisionToolResult.Passed(null, TimeSpan.Zero, new Dictionary<string, double> { { "Executed", 1.0 } });
            }
        }

        private sealed class FailingVisionTool : IVisionTool
        {
            public string Name => "Failing 2D";

            public bool WasExecuted { get; private set; }

            public VisionToolResult Execute(Mat source)
            {
                WasExecuted = true;
                return VisionToolResult.Failed(VisionToolErrorCode.Unknown, "Controlled 2D failure.", TimeSpan.Zero);
            }
        }

        private sealed class ThrowingThreeDTool : IThreeDInspectionTool
        {
            public string Name => "Throwing 3D";

            public ThreeDInspectionResult Execute(HeightMap3D source)
            {
                throw new InvalidOperationException("Controlled 3D exception.");
            }
        }

        private sealed class ThrowingNameThreeDTool : IThreeDInspectionTool
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
}
