using Lib.Inspection;
using Lib.OpenCV.Tool;
using Lib.ThreeD.FeatureExtraction;
using Lib.ThreeD.Geometry;
using Lib.ThreeD.Inspection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

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
                Run("Reference-grid re-sampling projects U/V/H cells and preserves holes", TestReferenceGridProjectionAndHoles, ref passed, ref total);
                Run("Reference-grid re-sampling chooses deterministic collision winners", TestReferenceGridCollisionTieBreak, ref passed, ref total);
                Run("Reference-grid re-sampling rejects half-open upper-bound overflow", TestReferenceGridOutOfBounds, ref passed, ref total);
                Run("Reference-grid re-sampling rejects invalid frame axes", TestReferenceGridInvalidAxes, ref passed, ref total);
                Run("Median filter removes a spike with the declared kernel", TestDeterministicMedianFilterSpike, ref passed, ref total);
                Run("Median filter preserves missing cells and clipped borders", TestDeterministicMedianFilterMissingAndBorder, ref passed, ref total);
                Run("Height-difference edge retains strongest pair and exact-tie order", TestDeterministicHeightDifferenceEdge, ref passed, ref total);
                Run("Height-difference edge skips missing pairs and requires support", TestDeterministicHeightDifferenceEdgeMissingAndSupport, ref passed, ref total);
                Run("Deterministic line fit preserves full-XYZ inliers and direction", TestDeterministicLineFit, ref passed, ref total);
                Run("Deterministic line fit rejects insufficient support", TestDeterministicLineFitSupportFailure, ref passed, ref total);
                Run("Least-squares height-field plane fit preserves analytic coefficients", TestLeastSquaresHeightFieldPlaneFit, ref passed, ref total);
                Run("Plane flatness measures independent reference and surface samples", TestPlaneFlatnessInspection, ref passed, ref total);
                Run("Plane flatness rejects degenerate reference geometry", TestPlaneFlatnessDegenerateReference, ref passed, ref total);
                Run("Point pair measures dimensions relative to the height axis", TestPointPairDimensions, ref passed, ref total);
                Run("Point pair honors a rotated height axis", TestPointPairDimensionsRotatedAxis, ref passed, ref total);
                Run("Point pair rejects coincident positions", TestPointPairDimensionsCoincident, ref passed, ref total);
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
                    new AffinePointCloudInputPoint(2, 3, 7.0, 3.0, 7.0, 2.0),
                    new AffinePointCloudInputPoint(5, 11, -2.0, 11.0, -2.0, 5.0)
                },
                matrix);

            Require(result.Success && result.Points.Count == 2, "Full XYZ affine apply must transform every supplied finite point.");
            Require(result.Points[0].Row == 2 && result.Points[0].Column == 3 && result.Points[0].RawHeight == 7.0, "Full XYZ affine apply must preserve the source locator and raw scalar.");
            RequireApproximately(result.Points[0].TransformedX, 19.0, 1e-12, "Unexpected transformed X.");
            RequireApproximately(result.Points[0].TransformedY, 39.5, 1e-12, "Unexpected transformed Y.");
            RequireApproximately(result.Points[0].TransformedZ, 35.25, 1e-12, "Unexpected transformed Z.");
        }

        private static void TestFullXyzAffineApplyDuplicateLocator()
        {
            AffinePointCloudApplyResult result = new AffinePointCloudApplyTool().Execute(
                new[]
                {
                    new AffinePointCloudInputPoint(0, 0, 1.0, 0.0, 1.0, 0.0),
                    new AffinePointCloudInputPoint(0, 0, 2.0, 0.0, 2.0, 0.0)
                },
                new FullXyzAffineMatrix(
                    1.0, 0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0));

            Require(!result.Success, "Full XYZ affine apply must reject duplicate source locators.");
        }

        private static void TestReferenceGridProjectionAndHoles()
        {
            ReferenceGridRegridResult result = new ReferenceGridRegridTool().Execute(
                new[]
                {
                    new ReferenceGridInputPoint(2, 4, 0.10, 0.10, 10.0),
                    new ReferenceGridInputPoint(2, 5, 1.10, 0.10, 20.0),
                    new ReferenceGridInputPoint(3, 4, 0.10, 1.10, 30.0)
                },
                CreateReferenceGridProfile(2, 2, 0.70));

            Require(result.Success && result.Cells.Count == 4, "Reference-grid re-sampling must emit every authored row-major cell.");
            RequireApproximately(result.Cells[0].Height, 10.0, 1e-12, "Unexpected first projected height.");
            RequireApproximately(result.Cells[1].Height, 20.0, 1e-12, "Unexpected second projected height.");
            RequireApproximately(result.Cells[2].Height, 30.0, 1e-12, "Unexpected third projected height.");
            Require(!double.IsNaN(result.Cells[0].PlanarDistanceSquared), "Reference-grid populated cells must retain winner planar-distance evidence.");
            Require(double.IsNaN(result.Cells[3].Height) && result.Cells[3].SourceRow == -1 && double.IsNaN(result.Cells[3].PlanarDistanceSquared), "Reference-grid holes must remain missing without fill.");
            RequireApproximately(result.CoverageRatio, 0.75, 1e-12, "Unexpected reference-grid coverage.");
            Require(result.MeetsMinimumCoverage, "Coverage must meet the authored Publish minimum.");
        }

        private static void TestReferenceGridCollisionTieBreak()
        {
            ReferenceGridRegridResult result = new ReferenceGridRegridTool().Execute(
                new[]
                {
                    new ReferenceGridInputPoint(9, 9, 0.75, 0.50, 90.0),
                    new ReferenceGridInputPoint(3, 8, 0.25, 0.50, 30.0),
                    new ReferenceGridInputPoint(3, 7, 0.25, 0.50, 20.0)
                },
                CreateReferenceGridProfile(1, 1, 1.0));

            Require(result.Success && result.CollisionCount == 2 && result.PopulatedCellCount == 1, "Reference-grid collisions must be counted without adding cells.");
            Require(result.Cells[0].SourceRow == 3 && result.Cells[0].SourceColumn == 7, "Equal planar-distance collisions must choose lower source row then column.");
            RequireApproximately(result.Cells[0].Height, 20.0, 1e-12, "Collision winner height was not retained.");
        }

        private static void TestReferenceGridOutOfBounds()
        {
            ReferenceGridRegridResult result = new ReferenceGridRegridTool().Execute(
                new[] { new ReferenceGridInputPoint(0, 0, 1.0, 0.0, 2.0) },
                CreateReferenceGridProfile(1, 1, 0.0));

            Require(!result.Success && result.Message.IndexOf("half-open", StringComparison.OrdinalIgnoreCase) >= 0, "Reference-grid upper U boundary must be rejected rather than assigned outside the grid.");
        }

        private static void TestReferenceGridInvalidAxes()
        {
            ReferenceGridProfile invalid = new ReferenceGridProfile(
                "frame.fixture-reference", "fixture-unit", "fixture reference", "R1",
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 0.0, 1.0,
                1.0, 1.0, 1, 1, 0.0);
            ReferenceGridRegridResult result = new ReferenceGridRegridTool().Execute(
                new[] { new ReferenceGridInputPoint(0, 0, 0.0, 0.0, 0.0) }, invalid);

            Require(!result.Success && result.Message.IndexOf("orthonormal", StringComparison.OrdinalIgnoreCase) >= 0, "Reference-grid non-orthonormal axes must be rejected.");
        }

        private static void TestDeterministicMedianFilterSpike()
        {
            DeterministicMedianFilterResult result = new DeterministicMedianFilterTool().Execute(
                3,
                3,
                new[] { 1.0, 1.0, 1.0, 1.0, 100.0, 1.0, 1.0, 1.0, 1.0 },
                new DeterministicMedianFilterOptions { KernelSize = 3 });

            Require(result.Success && result.Values.All(value => value == 1.0) && result.ChangedCount == 1,
                "Median filter must remove one isolated center spike.");
        }

        private static void TestDeterministicMedianFilterMissingAndBorder()
        {
            DeterministicMedianFilterResult missing = new DeterministicMedianFilterTool().Execute(
                3,
                1,
                new[] { 1.0, double.NaN, 5.0 },
                new DeterministicMedianFilterOptions { KernelSize = 3 });
            DeterministicMedianFilterResult border = new DeterministicMedianFilterTool().Execute(
                2,
                2,
                new[] { 1.0, 2.0, 3.0, 4.0 },
                new DeterministicMedianFilterOptions { KernelSize = 3 });

            Require(missing.Success && missing.Values[0] == 1.0 && double.IsNaN(missing.Values[1]) && missing.Values[2] == 5.0,
                "Median filter must preserve the source missing mask.");
            Require(border.Success && border.Values.All(value => value == 2.5),
                "Median filter borders must use available neighbors only.");
        }

        private static void TestDeterministicHeightDifferenceEdge()
        {
            HeightDifferenceEdgeResult result = new DeterministicHeightDifferenceEdgeTool().Execute(
                3,
                4,
                new[]
                {
                    0.0, 5.0, 15.0, 25.0,
                    0.0, 7.0, 17.0, 27.0,
                    0.0, 9.0, 19.0, 29.0
                },
                new HeightDifferenceEdgeOptions
                {
                    Selection = new HeightDifferenceEdgeSelection(0, 0, 3, 4),
                    ComparisonAxis = HeightDifferenceEdgeComparisonAxis.AcrossColumns,
                    Polarity = HeightDifferenceEdgePolarity.Rising,
                    MinimumDelta = 10.0
                });

            Require(result.Success, "Height-difference edge must accept the analytic scanlines.");
            Require(result.Points.Count == 3 && result.Diagnostics.EligiblePairCount == 9 && result.Diagnostics.SkippedMissingPairCount == 0,
                "Height-difference edge must retain the expected scan diagnostics.");
            Require(result.Points.All(point => point.FirstColumn == 1 && point.SecondColumn == 2 && point.Magnitude == 10.0),
                "Exact strongest-pair ties must retain the first start index.");
        }

        private static void TestDeterministicHeightDifferenceEdgeMissingAndSupport()
        {
            HeightDifferenceEdgeResult missing = new DeterministicHeightDifferenceEdgeTool().Execute(
                3,
                3,
                new[]
                {
                    0.0, 10.0, 25.0,
                    0.0, double.NaN, 30.0,
                    0.0, 10.0, 25.0
                },
                new HeightDifferenceEdgeOptions
                {
                    Selection = new HeightDifferenceEdgeSelection(0, 0, 3, 3),
                    ComparisonAxis = HeightDifferenceEdgeComparisonAxis.AcrossColumns,
                    Polarity = HeightDifferenceEdgePolarity.Rising,
                    MinimumDelta = 10.0
                });
            HeightDifferenceEdgeResult insufficient = new DeterministicHeightDifferenceEdgeTool().Execute(
                2,
                2,
                new[] { 0.0, 10.0, 0.0, 1.0 },
                new HeightDifferenceEdgeOptions
                {
                    Selection = new HeightDifferenceEdgeSelection(0, 0, 2, 2),
                    ComparisonAxis = HeightDifferenceEdgeComparisonAxis.AcrossColumns,
                    Polarity = HeightDifferenceEdgePolarity.Rising,
                    MinimumDelta = 5.0
                });

            Require(missing.Success && missing.Points.Count == 2 && missing.Diagnostics.SkippedMissingPairCount == 2,
                "Missing edge cells must skip only their adjacent pairs without filling or bridging.");
            Require(!insufficient.Success && insufficient.Message.IndexOf("at least two accepted", StringComparison.OrdinalIgnoreCase) >= 0,
                "Height-difference edge must reject fewer than two accepted scanlines.");
        }

        private static void TestDeterministicLineFit()
        {
            List<DeterministicLineFitPoint> points = new List<DeterministicLineFitPoint>();
            for (int index = 0; index < 8; index++)
            {
                points.Add(new DeterministicLineFitPoint(index, new ThreeDPoint(2.0 + (0.5 * index), -3.0 + (0.25 * index), index)));
            }
            points.Add(new DeterministicLineFitPoint(8, new ThreeDPoint(20.0, -30.0, 8.0)));
            points.Add(new DeterministicLineFitPoint(9, new ThreeDPoint(-10.0, 25.0, 9.0)));

            DeterministicLineFitOptions options = new DeterministicLineFitOptions
            {
                InputHash = new string('A', 64),
                MaximumOrthogonalResidual = 0.05,
                MinimumInlierCount = 6,
                MinimumInlierRatio = 0.6,
                MinimumInlierScanlineSpan = 5,
                PositiveScanlineAxis = DeterministicLineFitPositiveAxis.Z
            };
            DeterministicLineFitResult first = new DeterministicLineFitTool().Execute(points, options);
            DeterministicLineFitResult second = new DeterministicLineFitTool().Execute(points, options);
            double norm = Math.Sqrt((0.5 * 0.5) + (0.25 * 0.25) + 1.0);

            Require(first.Success && second.Success, "Deterministic line fit must accept the analytic full-XYZ inlier set.");
            Require(first.Diagnostics.InlierCount == 8 && first.Diagnostics.OutlierCount == 2, "Deterministic line fit must retain the expected inlier membership.");
            RequireApproximately(first.Geometry.Direction.X, 0.5 / norm, 1e-9, "Unexpected deterministic line direction X.");
            RequireApproximately(first.Geometry.Direction.Y, 0.25 / norm, 1e-9, "Unexpected deterministic line direction Y.");
            RequireApproximately(first.Geometry.Direction.Z, 1.0 / norm, 1e-9, "Unexpected deterministic line direction Z.");
            Require(first.PointDiagnostics.Count == second.PointDiagnostics.Count
                && first.PointDiagnostics.Where(point => point.IsInlier).Count() == second.PointDiagnostics.Where(point => point.IsInlier).Count(),
                "Repeated deterministic line fits must retain identical membership counts.");
        }

        private static void TestDeterministicLineFitSupportFailure()
        {
            DeterministicLineFitResult result = new DeterministicLineFitTool().Execute(
                new[]
                {
                    new DeterministicLineFitPoint(0, new ThreeDPoint(0.0, 0.0, 0.0)),
                    new DeterministicLineFitPoint(1, new ThreeDPoint(0.0, 0.0, 1.0)),
                    new DeterministicLineFitPoint(2, new ThreeDPoint(0.0, 0.0, 2.0)),
                    new DeterministicLineFitPoint(3, new ThreeDPoint(50.0, 50.0, 3.0)),
                    new DeterministicLineFitPoint(4, new ThreeDPoint(-50.0, -50.0, 4.0))
                },
                new DeterministicLineFitOptions
                {
                    InputHash = new string('B', 64),
                    MaximumOrthogonalResidual = 0.01,
                    MinimumInlierCount = 3,
                    MinimumInlierRatio = 0.8,
                    MinimumInlierScanlineSpan = 2,
                    PositiveScanlineAxis = DeterministicLineFitPositiveAxis.Z
                });

            Require(!result.Success && result.Message.IndexOf("support", StringComparison.OrdinalIgnoreCase) >= 0, "Deterministic line fit must reject insufficient taught support.");
        }

        private static void TestLeastSquaresHeightFieldPlaneFit()
        {
            HeightFieldPlaneFitSample[] samples = CreateAnalyticPlaneSamples(0.5, -0.25, 2.0, new double[9]);
            LeastSquaresHeightFieldPlaneFitResult result = new LeastSquaresHeightFieldPlaneFitTool().Execute(samples);

            RequireApproximately(result.SlopeX, 0.5, 1e-12, "Unexpected height-field plane X slope.");
            RequireApproximately(result.SlopeZ, -0.25, 1e-12, "Unexpected height-field plane Z slope.");
            RequireApproximately(result.Intercept, 2.0, 1e-12, "Unexpected height-field plane intercept.");
            RequireApproximately(result.RootMeanSquareDistance, 0.0, 1e-7, "Analytic plane fit RMS must be zero within float-compatible distance precision.");
        }

        private static void TestPlaneFlatnessInspection()
        {
            HeightFieldPlaneFitSample[] reference = CreateAnalyticPlaneSamples(0.5, -0.25, 2.0, new double[9]);
            HeightFieldPlaneFitSample[] measurement = CreateAnalyticPlaneSamples(
                0.5,
                -0.25,
                2.0,
                new[] { -0.4, 0.0, 0.6, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 });
            PlaneFlatnessInspectionResult result = new PlaneFlatnessInspectionTool().Execute(reference, measurement, 1.1);

            Require(result.Passed && result.ReferenceSampleCount == 9 && result.MeasurementSampleCount == 9,
                "Independent plane-flatness sample counts or pass state are incorrect.");
            RequireApproximately(result.Flatness, 1.0, 1e-6, "Unexpected orthogonal flatness.");
            Require(result.MinimumSignedDistance < 0.0 && result.MaximumSignedDistance > 0.0,
                "Plane-flatness extrema must preserve signed sides of the reference plane.");
        }

        private static void TestPlaneFlatnessDegenerateReference()
        {
            HeightFieldPlaneFitSample[] reference =
            {
                new HeightFieldPlaneFitSample(new ThreeDPoint(0.0, 0.0, 0.0), 0.0),
                new HeightFieldPlaneFitSample(new ThreeDPoint(1.0, 1.0, 0.0), 1.0),
                new HeightFieldPlaneFitSample(new ThreeDPoint(2.0, 2.0, 0.0), 2.0)
            };
            HeightFieldPlaneFitSample[] measurement = CreateAnalyticPlaneSamples(0.0, 0.0, 0.0, new double[9]);

            try
            {
                new PlaneFlatnessInspectionTool().Execute(reference, measurement, 1.0);
                throw new InvalidOperationException("Degenerate reference geometry must be rejected.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.Message.IndexOf("span two horizontal axes", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Degenerate reference rejection must retain the plane-fit contract.");
            }
        }

        private static void TestPointPairDimensions()
        {
            PointPairDimensionsInspectionResult result = new PointPairDimensionsInspectionTool().Execute(
                new ThreeDPoint(1.0, 2.0, 3.0),
                new ThreeDPoint(4.0, 6.0, 7.0),
                new ThreeDPoint(0.0, 1.0, 0.0),
                12.0,
                16.0,
                PointPairOptions(Math.Sqrt(41.0), 5.0, Math.Atan2(4.0, 5.0) * 180.0 / Math.PI));

            Require(result.Passed, "Analytic point pair must pass exact tolerances.");
            RequireApproximately(result.Distance, Math.Sqrt(41.0), 1e-12, "Unexpected point-pair distance.");
            RequireApproximately(result.PlanarWidth, 5.0, 1e-12, "Unexpected point-pair planar width.");
            RequireApproximately(result.AxialHeightDelta, 4.0, 1e-12, "Unexpected point-pair axial height delta.");
            RequireApproximately(result.ScalarHeightDelta, 4.0, 1e-12, "Unexpected point-pair scalar height delta.");
        }

        private static void TestPointPairDimensionsRotatedAxis()
        {
            PointPairDimensionsInspectionResult result = new PointPairDimensionsInspectionTool().Execute(
                new ThreeDPoint(0.0, 0.0, 0.0),
                new ThreeDPoint(3.0, 4.0, 12.0),
                new ThreeDPoint(0.0, 0.0, 2.0),
                2.0,
                14.0,
                PointPairOptions(13.0, 5.0, Math.Atan2(12.0, 5.0) * 180.0 / Math.PI));

            Require(result.Passed, "Rotated-axis point pair must pass exact tolerances.");
            RequireApproximately(result.NormalizedHeightAxis.Z, 1.0, 1e-12, "Height axis was not normalized.");
            RequireApproximately(result.PlanarWidth, 5.0, 1e-12, "Planar width must be orthogonal to the declared height axis.");
            RequireApproximately(result.AxialHeightDelta, 12.0, 1e-12, "Axial height must follow the declared height axis.");
        }

        private static void TestPointPairDimensionsCoincident()
        {
            try
            {
                new PointPairDimensionsInspectionTool().Execute(
                    new ThreeDPoint(1.0, 2.0, 3.0),
                    new ThreeDPoint(1.0, 2.0, 3.0),
                    new ThreeDPoint(0.0, 1.0, 0.0),
                    0.0,
                    0.0,
                    PointPairOptions(0.0, 0.0, 0.0));
                throw new InvalidOperationException("Coincident point-pair positions must be rejected.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.Message.IndexOf("distinct", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Coincident point-pair rejection must explain the distinct-point contract.");
            }
        }

        private static PointPairDimensionsInspectionOptions PointPairOptions(
            double distance,
            double planarWidth,
            double elevationAngleDegrees) =>
            new PointPairDimensionsInspectionOptions
            {
                ExpectedDistance = distance,
                DistanceTolerance = 1e-10,
                ExpectedPlanarWidth = planarWidth,
                PlanarWidthTolerance = 1e-10,
                ExpectedElevationAngleDegrees = elevationAngleDegrees,
                ElevationAngleToleranceDegrees = 1e-10
            };

        private static HeightFieldPlaneFitSample[] CreateAnalyticPlaneSamples(
            double slopeX,
            double slopeZ,
            double intercept,
            IReadOnlyList<double> normalOffsets)
        {
            HeightFieldPlaneFitSample[] samples = new HeightFieldPlaneFitSample[9];
            double normalLength = Math.Sqrt((slopeX * slopeX) + 1.0 + (slopeZ * slopeZ));
            for (int z = 0; z < 3; z++)
            {
                for (int x = 0; x < 3; x++)
                {
                    int index = (z * 3) + x;
                    double y = (slopeX * x) + (slopeZ * z) + intercept + (normalOffsets[index] * normalLength);
                    samples[index] = new HeightFieldPlaneFitSample(new ThreeDPoint(x, y, z), y);
                }
            }

            return samples;
        }

        private static ReferenceGridProfile CreateReferenceGridProfile(int rows, int columns, double minimumCoverage)
        {
            return new ReferenceGridProfile(
                "frame.fixture-reference", "fixture-unit", "fixture reference", "R1",
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0,
                1.0, 1.0, rows, columns, minimumCoverage);
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
