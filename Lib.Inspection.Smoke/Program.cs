using Lib.Inspection;
using Lib.OpenCV.Tool;
using Lib.ThreeD.FeatureExtraction;
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
                Run("Datum plane evaluates an analytic raw-height surface", TestDatumPlaneAnalyticSurface, ref passed, ref total);
                Run("Datum plane retains measurement for a local-limit failure", TestDatumPlaneToleranceFailure, ref passed, ref total);
                Run("Datum plane rejects a near-vertical height-field orientation", TestDatumPlaneNearVertical, ref passed, ref total);
                Run("Datum plane treats missing cells separately from valid samples", TestDatumPlaneMissingSamples, ref passed, ref total);
                Run("Two-point line constructs an ordered full-XYZ segment", TestTwoPointLine, ref passed, ref total);
                Run("Two-point line rejects a zero-length segment", TestTwoPointLineZeroLength, ref passed, ref total);
                Run("Three-point plane preserves authored normal orientation", TestThreePointPlane, ref passed, ref total);
                Run("Three-point plane reverses normal when pick order reverses", TestThreePointPlaneOrder, ref passed, ref total);
                Run("Three-point plane rejects collinear and near-collinear support", TestThreePointPlaneDegenerate, ref passed, ref total);
                Run("Line intersection recovers a perpendicular corner", TestLineIntersection, ref passed, ref total);
                Run("Line intersection rejects parallel geometry", TestLineIntersectionParallel, ref passed, ref total);
                Run("Full XYZ affine solve recovers an analytic matrix", TestFullXyzAffineSolve, ref passed, ref total);
                Run("Full XYZ affine solve rejects a taught condition limit", TestFullXyzAffineCondition, ref passed, ref total);
                Run("Full XYZ affine apply preserves locator order and exact transformed XYZ", TestFullXyzAffineApply, ref passed, ref total);
                Run("Full XYZ affine apply rejects duplicate source locators", TestFullXyzAffineApplyDuplicateLocator, ref passed, ref total);
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

        private static void TestTwoPointLine()
        {
            TwoPointLineResult result = new TwoPointLineTool().Execute(
                new TwoPointLineInput(new ThreeDPoint(1.0, 2.0, 3.0), new ThreeDPoint(4.0, 6.0, 3.0)));

            Require(result.Success, "Two-point line must succeed for distinct finite points.");
            RequireApproximately(result.SegmentLength, 5.0, 1e-12, "Unexpected two-point segment length.");
            RequireApproximately(result.Direction.X, 0.6, 1e-12, "Unexpected two-point X direction.");
            RequireApproximately(result.Direction.Y, 0.8, 1e-12, "Unexpected two-point Y direction.");
            Require(result.SegmentStart.X == 1.0 && result.SegmentEnd.X == 4.0, "Two-point authored order was not retained.");
        }

        private static void TestTwoPointLineZeroLength()
        {
            ThreeDPoint point = new ThreeDPoint(1.0, 2.0, 3.0);
            TwoPointLineResult result = new TwoPointLineTool().Execute(new TwoPointLineInput(point, point));

            Require(!result.Success, "Two-point line must reject a zero-length segment.");
        }

        private static void TestDatumPlaneAnalyticSurface()
        {
            ThreeDInspectionResult result = new DatumPlaneRawHeightDeviationInspectionTool(
                new DatumPlaneRawHeightDeviationInspectionOptions
                {
                    PlaneNormalX = -2.0,
                    PlaneNormalY = 1.0,
                    PlaneNormalZ = -3.0,
                    PlaneOffset = -5.0,
                    MaximumPeakToValleyRawHeight = 0.000001
                }).Execute(CreatePlaneMap(3, 3, 2.0, 3.0, 5.0));

            Require(result.Success && result.HasMeasurement, "Analytic datum-plane surface must pass.");
            RequireApproximately(result.Metrics["PeakToValleyRawHeight"], 0.0, 1e-12, "Unexpected datum-plane P2V.");
            RequireApproximately(result.Metrics["RmsRawHeightResidual"], 0.0, 1e-12, "Unexpected datum-plane RMS.");
            RequireApproximately(result.Metrics["PlaneNormalY"], 1.0 / Math.Sqrt(14.0), 1e-12, "Datum-plane normal must be normalized.");
        }

        private static void TestDatumPlaneToleranceFailure()
        {
            HeightMap3D source = CreatePlaneMap(3, 3, 2.0, 3.0, 5.0);
            double[] values = source.CopyValues();
            values[values.Length - 1] += 0.1;
            source = new HeightMap3D(3, 3, 0.0, 0.0, 1.0, 1.0, values, "raw-height", "frame", "datum-failure");
            ThreeDInspectionResult result = new DatumPlaneRawHeightDeviationInspectionTool(
                new DatumPlaneRawHeightDeviationInspectionOptions
                {
                    PlaneNormalX = -2.0,
                    PlaneNormalY = 1.0,
                    PlaneNormalZ = -3.0,
                    PlaneOffset = -5.0,
                    MaximumPeakToValleyRawHeight = 0.001
                }).Execute(source);

            Require(!result.Success && result.HasMeasurement && result.ResultStatus == ThreeDInspectionResultStatus.Failed, "Out-of-limit datum-plane result must retain measurement evidence.");
            Require(result.Metrics["PeakToValleyRawHeight"] > 0.001, "Datum-plane failure must expose the P2V evidence.");
        }

        private static void TestDatumPlaneNearVertical()
        {
            ThreeDInspectionResult result = new DatumPlaneRawHeightDeviationInspectionTool(
                new DatumPlaneRawHeightDeviationInspectionOptions
                {
                    PlaneNormalX = 1.0,
                    PlaneNormalY = 0.01,
                    PlaneNormalZ = 0.0,
                    PlaneOffset = 0.0,
                    MaximumPeakToValleyRawHeight = 1.0
                }).Execute(CreatePlaneMap(2, 2, 0.0, 0.0, 1.0));

            Require(!result.HasMeasurement && result.ErrorCode == ThreeDInspectionErrorCode.DegenerateGeometry, "Near-vertical plane must be rejected before raw-height residual evaluation.");
        }

        private static void TestDatumPlaneMissingSamples()
        {
            HeightMap3D source = new HeightMap3D(2, 2, 0.0, 0.0, 1.0, 1.0, new[] { 5.0, double.NaN, 5.0, 6.0 }, "raw-height", "frame", "datum-missing");
            ThreeDInspectionResult result = new DatumPlaneRawHeightDeviationInspectionTool(
                new DatumPlaneRawHeightDeviationInspectionOptions
                {
                    PlaneNormalX = -1.0,
                    PlaneNormalY = 1.0,
                    PlaneNormalZ = 0.0,
                    PlaneOffset = -5.0,
                    MaximumPeakToValleyRawHeight = 0.000001,
                    MinimumValidSamples = 3
                }).Execute(source);

            Require(result.Success && result.HasMeasurement, "Three finite datum-plane samples must remain measurable.");
            RequireApproximately(result.Metrics["ValidSampleCount"], 3.0, 1e-12, "Unexpected datum-plane valid count.");
            RequireApproximately(result.Metrics["MissingSampleCount"], 1.0, 1e-12, "Unexpected datum-plane missing count.");
        }

        private static void TestThreePointPlane()
        {
            ThreePointPlaneResult result = new ThreePointPlaneTool().Execute(
                new ThreePointPlaneInput(
                    new ThreeDPoint(1.0, 2.0, 3.0),
                    new ThreeDPoint(4.0, 2.0, 3.0),
                    new ThreeDPoint(1.0, 6.0, 3.0)));

            Require(result.Success, "Three-point plane must succeed for a non-collinear ordered triple.");
            RequireApproximately(result.Normal.X, 0.0, 1e-12, "Unexpected three-point plane normal X.");
            RequireApproximately(result.Normal.Y, 0.0, 1e-12, "Unexpected three-point plane normal Y.");
            RequireApproximately(result.Normal.Z, 1.0, 1e-12, "Unexpected three-point plane normal Z.");
            RequireApproximately(result.PlaneOffset, -3.0, 1e-12, "Unexpected three-point plane offset.");
            Require(result.SupportFirst.X == 1.0 && result.SupportSecond.X == 4.0 && result.SupportThird.Y == 6.0, "Three-point support order was not retained.");
        }

        private static void TestThreePointPlaneOrder()
        {
            ThreePointPlaneResult result = new ThreePointPlaneTool().Execute(
                new ThreePointPlaneInput(
                    new ThreeDPoint(1.0, 2.0, 3.0),
                    new ThreeDPoint(1.0, 6.0, 3.0),
                    new ThreeDPoint(4.0, 2.0, 3.0)));

            Require(result.Success, "Reordered non-collinear three-point plane must remain valid.");
            RequireApproximately(result.Normal.Z, -1.0, 1e-12, "Reordered support must reverse the oriented normal.");
            RequireApproximately(result.PlaneOffset, 3.0, 1e-12, "Reordered support must reverse the oriented plane offset.");
        }

        private static void TestThreePointPlaneDegenerate()
        {
            ThreePointPlaneResult collinear = new ThreePointPlaneTool().Execute(
                new ThreePointPlaneInput(
                    new ThreeDPoint(0.0, 0.0, 0.0),
                    new ThreeDPoint(1.0, 0.0, 0.0),
                    new ThreeDPoint(2.0, 0.0, 0.0)));
            ThreePointPlaneResult nearCollinear = new ThreePointPlaneTool().Execute(
                new ThreePointPlaneInput(
                    new ThreeDPoint(0.0, 0.0, 0.0),
                    new ThreeDPoint(1.0, 0.0, 0.0),
                    new ThreeDPoint(2.0, 1e-13, 0.0)));

            Require(!collinear.Success && !nearCollinear.Success, "Collinear and near-collinear support must be rejected.");
        }

        private static void TestLineIntersection()
        {
            LineIntersectionResult result = new LineIntersectionTool().Execute(
                CreateLine(new ThreeDPoint(0.0, 0.0, 0.0), new ThreeDPoint(1.0, 0.0, 0.0), new ThreeDPoint(-2.0, 0.0, 0.0), new ThreeDPoint(2.0, 0.0, 0.0)),
                CreateLine(new ThreeDPoint(0.0, 0.0, 0.0), new ThreeDPoint(0.0, 1.0, 0.0), new ThreeDPoint(0.0, -2.0, 0.0), new ThreeDPoint(0.0, 2.0, 0.0)),
                new LineIntersectionOptions
                {
                    MaximumClosestApproachDistance = 0.001,
                    MinimumAcuteAngleDegrees = 45.0,
                    MaximumSupportExtension = 0.0
                });

            Require(result.Success, "Perpendicular full-XYZ lines must intersect.");
            RequireApproximately(result.ClosestApproachDistance, 0.0, 1e-12, "Unexpected line-intersection gap.");
            RequireApproximately(result.AcuteAngleDegrees, 90.0, 1e-12, "Unexpected line-intersection acute angle.");
            RequireApproximately(result.CornerAnchor.X, 0.0, 1e-12, "Unexpected line-intersection corner X.");
        }

        private static void TestLineIntersectionParallel()
        {
            LineIntersectionResult result = new LineIntersectionTool().Execute(
                CreateLine(new ThreeDPoint(0.0, 0.0, 0.0), new ThreeDPoint(1.0, 0.0, 0.0), new ThreeDPoint(-2.0, 0.0, 0.0), new ThreeDPoint(2.0, 0.0, 0.0)),
                CreateLine(new ThreeDPoint(0.0, 1.0, 0.0), new ThreeDPoint(1.0, 0.0, 0.0), new ThreeDPoint(-2.0, 1.0, 0.0), new ThreeDPoint(2.0, 1.0, 0.0)),
                new LineIntersectionOptions
                {
                    MaximumClosestApproachDistance = 10.0,
                    MinimumAcuteAngleDegrees = 1.0,
                    MaximumSupportExtension = 1.0
                });

            Require(!result.Success, "Parallel full-XYZ lines must be rejected.");
        }

        private static ThreeDLineGeometry CreateLine(ThreeDPoint anchor, ThreeDPoint direction, ThreeDPoint start, ThreeDPoint end)
        {
            return new ThreeDLineGeometry(anchor, direction, start, end);
        }

        private static void TestFullXyzAffineSolve()
        {
            FullXyzAffineSolveResult result = new FullXyzAffineSolveTool().Execute(
                CreateAffinePairs(),
                new FullXyzAffineSolveOptions { MaximumConditionEstimate = 1000.0, ArithmeticResidualWarning = 1e-10 });

            Require(result.Success, "Full XYZ affine solve must recover four independent pairs.");
            RequireApproximately(result.Matrix.M11, 2.0, 1e-12, "Unexpected affine M11.");
            RequireApproximately(result.Matrix.M12, 0.5, 1e-12, "Unexpected affine M12.");
            RequireApproximately(result.Matrix.M13, -0.25, 1e-12, "Unexpected affine M13.");
            RequireApproximately(result.Matrix.M14, 10.0, 1e-12, "Unexpected affine M14.");
            RequireApproximately(result.ArithmeticMaximumResidual, 0.0, 1e-10, "Exact affine residual must be zero.");
        }

        private static void TestFullXyzAffineCondition()
        {
            FullXyzAffineSolveResult result = new FullXyzAffineSolveTool().Execute(
                CreateAffinePairs(),
                new FullXyzAffineSolveOptions { MaximumConditionEstimate = 0.5, ArithmeticResidualWarning = 0.0 });

            Require(!result.Success, "Full XYZ affine solve must reject an exceeded taught condition limit.");
        }

        private static void TestFullXyzAffineApply()
        {
            FullXyzAffineMatrix matrix = new FullXyzAffineMatrix(
                2.0, 0.5, -0.25, 10.0,
                -1.0, 3.0, 0.75, 20.0,
                0.25, -0.5, 4.0, 30.0);
            AffinePointCloudApplyResult result = new AffinePointCloudApplyTool().Execute(
                new[]
                {
                    new AffinePointCloudInputPoint(2, 3, 7.0, new ThreeDPoint(3.0, 7.0, 2.0)),
                    new AffinePointCloudInputPoint(5, 11, -2.0, new ThreeDPoint(11.0, -2.0, 5.0))
                },
                matrix);

            Require(result.Success && result.Points.Count == 2, "Full XYZ affine apply must transform every supplied finite point.");
            Require(result.Points[0].Row == 2 && result.Points[0].Column == 3 && result.Points[0].RawHeight == 7.0, "Full XYZ affine apply must preserve the source locator and raw scalar.");
            RequireApproximately(result.Points[0].Transformed.X, 19.0, 1e-12, "Unexpected transformed X.");
            RequireApproximately(result.Points[0].Transformed.Y, 39.5, 1e-12, "Unexpected transformed Y.");
            RequireApproximately(result.Points[0].Transformed.Z, 35.25, 1e-12, "Unexpected transformed Z.");
        }

        private static void TestFullXyzAffineApplyDuplicateLocator()
        {
            AffinePointCloudApplyResult result = new AffinePointCloudApplyTool().Execute(
                new[]
                {
                    new AffinePointCloudInputPoint(0, 0, 1.0, new ThreeDPoint(0.0, 1.0, 0.0)),
                    new AffinePointCloudInputPoint(0, 0, 2.0, new ThreeDPoint(0.0, 2.0, 0.0))
                },
                new FullXyzAffineMatrix(
                    1.0, 0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0));

            Require(!result.Success, "Full XYZ affine apply must reject duplicate source locators.");
        }

        private static IReadOnlyList<FullXyzAffineCorrespondence> CreateAffinePairs()
        {
            return new[]
            {
                new FullXyzAffineCorrespondence(new ThreeDPoint(0.0, 0.0, 0.0), new ThreeDPoint(10.0, 20.0, 30.0)),
                new FullXyzAffineCorrespondence(new ThreeDPoint(1.0, 0.0, 0.0), new ThreeDPoint(12.0, 19.0, 30.2)),
                new FullXyzAffineCorrespondence(new ThreeDPoint(0.0, 1.0, 0.0), new ThreeDPoint(10.5, 23.0, 29.7)),
                new FullXyzAffineCorrespondence(new ThreeDPoint(0.0, 0.0, 1.0), new ThreeDPoint(9.75, 20.75, 34.0))
            };
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
