using Lib.Inspection;
using Lib.OpenCV.Property;
using Lib.OpenCV.Tool;
using Lib.ThreeD.FeatureExtraction;
using Lib.ThreeD.Geometry;
using Lib.ThreeD.Inspection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
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
                Run("Local-median outlier filter excludes the center and preserves the strict threshold", TestDeterministicLocalMedianOutlierFilter, ref passed, ref total);
                Run("Level Surface detrends unique reference cells and preserves region evidence", TestLevelSurfaceDetrend, ref passed, ref total);
                Run("Level Surface fails closed on insufficient unique reference support", TestLevelSurfaceInsufficientSupport, ref passed, ref total);
                Run("Height-difference edge retains strongest pair and exact-tie order", TestDeterministicHeightDifferenceEdge, ref passed, ref total);
                Run("Height-difference edge skips missing pairs and requires support", TestDeterministicHeightDifferenceEdgeMissingAndSupport, ref passed, ref total);
                Run("Deterministic line fit preserves full-XYZ inliers and direction", TestDeterministicLineFit, ref passed, ref total);
                Run("Deterministic line fit rejects insufficient support", TestDeterministicLineFitSupportFailure, ref passed, ref total);
                Run("Rigid surface pose search recovers known yaw and translation", TestDeterministicRigidSurfacePoseSearch, ref passed, ref total);
                Run("Surface coverage preserves one-way unique occlusion evidence", TestDeterministicSurfaceCoverageOcclusion, ref passed, ref total);
                Run("Rigid surface pose search fails closed on bounded domains", TestDeterministicRigidSurfacePoseSearchBounds, ref passed, ref total);
                Run("Triangle-mesh distance preserves closest feature and robust sign evidence", TestTriangleMeshDistance, ref passed, ref total);
                Run("Nominal/actual mesh comparison preserves streaming statistics and sampling", TestNominalActualMeshComparison, ref passed, ref total);
                Run("Rigid-transform diagnostics preserve plausibility measures", TestRigidTransformDiagnostics, ref passed, ref total);
                Run("Surface-model preparation preserves even triangle samples", TestDeterministicSurfaceModelPreparation, ref passed, ref total);
                Run("Prepared-scene preparation preserves even point samples", TestDeterministicPreparedScenePreparation, ref passed, ref total);
                Run("Model surface-edge extraction preserves boundary topology", TestDeterministicModelSurfaceEdgeExtraction, ref passed, ref total);
                Run("Organized scene surface-edge extraction anchors height steps", TestDeterministicOrganizedSceneSurfaceEdgeExtraction, ref passed, ref total);
                Run("Surface-edge coverage reuses unique nearest matching", TestDeterministicSurfaceEdgeCoverage, ref passed, ref total);
                Run("Surface-edge coverage accepts an empty scene as zero coverage", TestDeterministicSurfaceEdgeCoverageEmptyScene, ref passed, ref total);
                Run("Least-squares height-field plane fit preserves analytic coefficients", TestLeastSquaresHeightFieldPlaneFit, ref passed, ref total);
                Run("Plane flatness measures independent reference and surface samples", TestPlaneFlatnessInspection, ref passed, ref total);
                Run("Plane flatness rejects degenerate reference geometry", TestPlaneFlatnessDegenerateReference, ref passed, ref total);
                Run("Point pair measures dimensions relative to the height axis", TestPointPairDimensions, ref passed, ref total);
                Run("Point pair honors a rotated height axis", TestPointPairDimensionsRotatedAxis, ref passed, ref total);
                Run("Point pair rejects coincident positions", TestPointPairDimensionsCoincident, ref passed, ref total);
                Run("Gap/flush measures signed separation and height difference", TestGapFlush, ref passed, ref total);
                Run("Gap/flush preserves signed overlap", TestGapFlushOverlap, ref passed, ref total);
                Run("Gap/flush rejects an empty region", TestGapFlushEmptyRegion, ref passed, ref total);
                Run("Volume integrates signed height relative to a reference plane", TestVolume, ref passed, ref total);
                Run("Volume preserves below-plane sign and tolerance failure", TestVolumeBelowPlane, ref passed, ref total);
                Run("Volume rejects an empty measurement ROI", TestVolumeEmptyMeasurement, ref passed, ref total);
                Run("Cross-section measures axis width and scalar-height range", TestCrossSectionDimensions, ref passed, ref total);
                Run("Cross-section reports independent width and height failures", TestCrossSectionDimensionsFailure, ref passed, ref total);
                Run("Cross-section rejects non-finite samples", TestCrossSectionDimensionsInvalidSample, ref passed, ref total);
                Run("2D affine transform recovers a known matrix and drawings", TestAffineTransformKnownMatrix, ref passed, ref total);
                Run("2D affine transform rejects collinear source teaching", TestAffineTransformDegenerateSource, ref passed, ref total);
                Run("2D affine transform retains evidence on coverage failure", TestAffineTransformCoverageFailure, ref passed, ref total);
                Run("Auto MPoint suggests a unique pattern deterministically", TestAutoMPointUniquePattern, ref passed, ref total);
                Run("Auto MPoint rejects a repeated ambiguous pattern", TestAutoMPointRepeatedPattern, ref passed, ref total);
                Run("Auto MPoint rejects invalid ROI and pattern size", TestAutoMPointInvalidDefinition, ref passed, ref total);
                Run("Auto MPoint selects the best representative-image pattern", TestAutoMPointRepresentativeBestPattern, ref passed, ref total);
                Run("Auto MPoint rejects an invalid representative set", TestAutoMPointInvalidRepresentativeSet, ref passed, ref total);
                Run("Edge matcher preserves legacy single-result behavior", TestEdgeMatcherLegacySingleResult, ref passed, ref total);
                Run("Edge matcher accepts one unique candidate", TestEdgeMatcherUniqueSuccess, ref passed, ref total);
                Run("Edge matcher rejects repeated candidates as ambiguous", TestEdgeMatcherUniqueAmbiguous, ref passed, ref total);
                Run("Edge matcher reports no match without a candidate", TestEdgeMatcherUniqueNoMatch, ref passed, ref total);
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

        private static void TestDeterministicLocalMedianOutlierFilter()
        {
            double[] values =
            {
                1.0, 1.0, 1.0,
                1.0, 100.0, 1.0,
                1.0, 1.0, 1.0
            };
            DeterministicLocalMedianOutlierFilterResult result =
                new DeterministicLocalMedianOutlierFilterTool().Execute(
                    3,
                    3,
                    values,
                    new DeterministicLocalMedianOutlierFilterOptions
                    {
                        WindowSize = 3,
                        MaximumAbsoluteDeviation = 20.0,
                        MinimumValidNeighbors = 3
                    });
            double[] thresholdValues =
            {
                1.0, 1.0, 1.0,
                1.0, 21.0, 1.0,
                1.0, 1.0, 1.0
            };
            DeterministicLocalMedianOutlierFilterResult threshold =
                new DeterministicLocalMedianOutlierFilterTool().Execute(
                    3,
                    3,
                    thresholdValues,
                    new DeterministicLocalMedianOutlierFilterOptions
                    {
                        WindowSize = 3,
                        MaximumAbsoluteDeviation = 20.0,
                        MinimumValidNeighbors = 3
                    });

            Require(result.Success
                && result.OutlierIndices.Count == 1
                && result.OutlierIndices[0] == 4
                && double.IsNaN(result.Values[4]),
                "The local-median filter must remove only the isolated center spike.");
            Require(threshold.Success
                && threshold.OutlierIndices.Count == 0
                && threshold.Values[4] == 21.0,
                "Deviation exactly equal to the threshold must be retained.");
        }

        private static void TestLevelSurfaceDetrend()
        {
            const int rows = 4;
            const int columns = 4;
            double[] values = new double[rows * columns];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    values[(row * columns) + column] =
                        10.0 + (2.0 * column) - (0.5 * row);
                }
            }

            LevelSurfaceResult result = new LevelSurfaceTool().Execute(
                rows,
                columns,
                values,
                new[]
                {
                    new LevelSurfaceRegion(0, 0, 4, 3),
                    new LevelSurfaceRegion(0, 2, 4, 2)
                },
                new LevelSurfaceOptions { MinimumValidSampleCount = 12 });

            Require(result.Success
                && result.ReferenceSampleCount == 16
                && result.RegionEvidence.Count == 2
                && result.RegionEvidence[0].ValidSampleCount == 12
                && result.RegionEvidence[1].ValidSampleCount == 8,
                "Level Surface must de-duplicate overlapping reference cells while retaining per-region counts.");
            RequireApproximately(result.FittedSlopeX, 2.0, 1e-12,
                "Unexpected Level Surface input X slope.");
            RequireApproximately(result.FittedSlopeZ, -0.5, 1e-12,
                "Unexpected Level Surface input Z slope.");
            RequireApproximately(result.OutputReferenceSlopeX, 0.0, 1e-12,
                "Level Surface must remove the reference X slope.");
            RequireApproximately(result.OutputReferenceSlopeZ, 0.0, 1e-12,
                "Level Surface must remove the reference Z slope.");
            Require(result.Values.All(value => Math.Abs(value - 12.25) < 1e-12),
                "Level Surface must detrend every finite cell to the reference mean.");
        }

        private static void TestLevelSurfaceInsufficientSupport()
        {
            LevelSurfaceResult result = new LevelSurfaceTool().Execute(
                2,
                2,
                new[] { 1.0, double.NaN, 2.0, 3.0 },
                new[] { new LevelSurfaceRegion(0, 0, 2, 2) },
                new LevelSurfaceOptions { MinimumValidSampleCount = 4 });

            Require(!result.Success
                && result.Values.Count == 0
                && result.Message.IndexOf(
                    "unique finite reference samples",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "Level Surface must fail closed when unique finite support is insufficient.");
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

        private static void TestDeterministicRigidSurfacePoseSearch()
        {
            IReadOnlyList<SurfaceMatchSample> model = CreateSurfaceMatchModel();
            RigidSurfacePose knownPose = CreateKnownSurfacePose();
            IReadOnlyList<SurfaceMatchSample> scene = model
                .Select(sample => new SurfaceMatchSample(
                    sample.Order,
                    knownPose.Transform(sample.Position)))
                .ToArray();
            DeterministicRigidSurfacePoseSearchTool tool =
                new DeterministicRigidSurfacePoseSearchTool();
            DeterministicRigidSurfacePoseSearchResult first =
                tool.Execute(model, scene, CreateSurfaceSearchOptions());
            DeterministicRigidSurfacePoseSearchResult second =
                tool.Execute(model, scene, CreateSurfaceSearchOptions());

            Require(first.Success && first.Matched && first.Pose != null,
                "Known surface pose must produce one matched rigid result.");
            Require(first.EvaluatedCandidateCount == 7
                && first.Coverage.MatchedModelSampleCount == 5
                && first.Coverage.Matches.Count == 5,
                "Known surface pose must preserve the bounded candidate count and full coverage.");
            RequireApproximately(first.Pose.M11, Math.Sqrt(3.0) / 2.0, 1e-12,
                "Unexpected known-pose rotation M11.");
            RequireApproximately(first.Pose.M12, -0.5, 1e-12,
                "Unexpected known-pose rotation M12.");
            RequireApproximately(first.Pose.M21, 0.5, 1e-12,
                "Unexpected known-pose rotation M21.");
            RequireApproximately(first.Pose.M22, Math.Sqrt(3.0) / 2.0, 1e-12,
                "Unexpected known-pose rotation M22.");
            RequireApproximately(first.Pose.TranslationX, 10.0, 1e-12,
                "Unexpected known-pose translation X.");
            RequireApproximately(first.Pose.TranslationY, -4.0, 1e-12,
                "Unexpected known-pose translation Y.");
            RequireApproximately(first.Pose.TranslationZ, 2.0, 1e-12,
                "Unexpected known-pose translation Z.");
            Require(second.Success
                && second.Matched
                && second.Pose != null
                && first.Pose.M11 == second.Pose.M11
                && first.Pose.TranslationX == second.Pose.TranslationX
                && first.Coverage.InlierRmse == second.Coverage.InlierRmse,
                "Repeated rigid surface pose search must be deterministic.");
        }

        private static void TestDeterministicSurfaceCoverageOcclusion()
        {
            IReadOnlyList<SurfaceMatchSample> model = CreateSurfaceMatchModel();
            RigidSurfacePose knownPose = CreateKnownSurfacePose();
            IReadOnlyList<SurfaceMatchSample> scene = model
                .Take(4)
                .Select(sample => new SurfaceMatchSample(
                    sample.Order,
                    knownPose.Transform(sample.Position)))
                .ToArray();
            DeterministicSurfaceCoverageResult result =
                new DeterministicSurfaceCoverageTool().Execute(
                    model,
                    scene,
                    knownPose,
                    1e-6);

            Require(result.Success
                && result.MatchedModelSampleCount == 4
                && result.UnmatchedModelSampleCount == 1
                && result.Matches.Count == 4,
                "One removed scene sample must retain four unique matches.");
            RequireApproximately(result.CoverageRatio, 0.8, 1e-15,
                "Occluded surface coverage must be four fifths.");
            Require(result.HasInlierRmse && result.InlierRmse <= 1e-12,
                "Exact retained scene samples must have near-zero RMSE.");
            Require(result.Matches.Select(match => match.SceneSampleOrder).Distinct().Count()
                == result.Matches.Count,
                "A scene sample must never be claimed more than once.");
        }

        private static void TestDeterministicRigidSurfacePoseSearchBounds()
        {
            IReadOnlyList<SurfaceMatchSample> model = CreateSurfaceMatchModel();
            RigidSurfacePose knownPose = CreateKnownSurfacePose();
            IReadOnlyList<SurfaceMatchSample> scene = model
                .Select(sample => new SurfaceMatchSample(
                    sample.Order,
                    knownPose.Transform(sample.Position)))
                .ToArray();
            DeterministicRigidSurfacePoseSearchOptions bounded =
                CreateSurfaceSearchOptions();
            bounded.MinimumTranslationX = -1.0;
            bounded.MaximumTranslationX = 1.0;
            bounded.MinimumTranslationY = -1.0;
            bounded.MaximumTranslationY = 1.0;
            bounded.MinimumTranslationZ = -1.0;
            bounded.MaximumTranslationZ = 1.0;
            DeterministicRigidSurfacePoseSearchResult noMatch =
                new DeterministicRigidSurfacePoseSearchTool().Execute(
                    model,
                    scene,
                    bounded);

            DeterministicRigidSurfacePoseSearchOptions insufficientBudget =
                CreateSurfaceSearchOptions();
            insufficientBudget.MaximumCandidateCount = 6;
            DeterministicRigidSurfacePoseSearchResult rejected =
                new DeterministicRigidSurfacePoseSearchTool().Execute(
                    model,
                    scene,
                    insufficientBudget);

            Require(noMatch.Success
                && !noMatch.Matched
                && noMatch.Pose == null
                && noMatch.EvaluatedCandidateCount == 7
                && noMatch.RejectionReason.IndexOf(
                    "bounds",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "Translation bounds must produce a controlled no-match result.");
            Require(!rejected.Success
                && rejected.Message.IndexOf(
                    "exceeds",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "A declared candidate budget must fail closed before search.");
        }

        private static void TestTriangleMeshDistance()
        {
            TriangleMeshDistanceTool tool = new TriangleMeshDistanceTool(
                new[]
                {
                    new MeshTriangle(
                        7,
                        new ThreeDPoint(0.0, 0.0, 0.0),
                        new ThreeDPoint(2.0, 0.0, 0.0),
                        new ThreeDPoint(0.0, 2.0, 0.0))
                });
            PointMeshDistance face = tool.Execute(
                new ThreeDPoint(0.5, 0.5, 1.0));
            PointMeshDistance boundary = tool.Execute(
                new ThreeDPoint(1.0, -1.0, 1.0));
            PointMeshDistance recovered = tool.ExecuteRobustSign(
                new ThreeDPoint(1.0, -1.0, 1.0),
                boundary.UnsignedDistance);

            Require(tool.TriangleCount == 1
                && face.SourceTriangleIndex == 7
                && face.ClosestFeature == MeshClosestFeature.FaceInterior
                && face.SignResolved
                && face.SignedDistance.HasValue,
                "Face-interior distance must retain direct signed evidence.");
            RequireApproximately(face.UnsignedDistance, 1.0, 1e-12,
                "Unexpected face-interior unsigned distance.");
            RequireApproximately(face.SignedDistance.Value, 1.0, 1e-12,
                "Unexpected face-interior signed distance.");
            Require(boundary.ClosestFeature == MeshClosestFeature.Edge
                && !boundary.SignResolved
                && !boundary.SignedDistance.HasValue,
                "Boundary distance must not guess a direct sign.");
            Require(recovered.SignResolved
                && recovered.SignedDistance.HasValue,
                "Robust boundary-sign execution must return explicit evidence.");
            RequireApproximately(
                recovered.SignedDistance.Value,
                Math.Sqrt(2.0),
                1e-12,
                "Unexpected robust boundary sign distance.");
        }

        private static void TestNominalActualMeshComparison()
        {
            NominalActualMeshComparisonResult result =
                new NominalActualMeshComparisonTool().Execute(
                    new[]
                    {
                        new MeshTriangle(
                            3,
                            new ThreeDPoint(0.0, 0.0, 0.0),
                            new ThreeDPoint(2.0, 0.0, 0.0),
                            new ThreeDPoint(0.0, 2.0, 0.0))
                    },
                    new[]
                    {
                        new ThreeDPoint(0.5, 0.5, 1.0),
                        new ThreeDPoint(0.5, 0.5, -2.0)
                    },
                    new NominalActualMeshComparisonOptions(
                        2,
                        -1.5,
                        1.5,
                        2));

            Require(result.Success
                && result.ProcessedPointCount == 2
                && result.BelowToleranceCount == 1
                && result.WithinToleranceCount == 1
                && result.AboveToleranceCount == 0
                && result.DirectSignResolvedCount == 2
                && result.RobustSignRecoveredCount == 0
                && result.DisplayStride == 1
                && result.DisplaySamples.Count == 2,
                "Nominal/actual comparison must retain deterministic counts and display sampling.");
            RequireApproximately(result.UnsignedStatistics.Mean, 1.5, 1e-12,
                "Unexpected unsigned-deviation mean.");
            RequireApproximately(result.SignedStatistics.Mean, -0.5, 1e-12,
                "Unexpected signed-deviation mean.");
            Require(result.DisplaySamples[0].SourceTriangleIndex == 3
                && result.DisplaySamples[0].PointIndex == 0,
                "Display evidence must retain source triangle and query order.");
        }

        private static void TestRigidTransformDiagnostics()
        {
            RigidTransformDiagnosticsTool tool =
                new RigidTransformDiagnosticsTool();
            RigidTransformDiagnosticsResult result = tool.Execute(
                new[]
                {
                    0.0, -1.0, 0.0, 3.0,
                    1.0, 0.0, 0.0, 4.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0
                });
            RigidTransformDiagnosticsResult rejected = tool.Execute(
                new[]
                {
                    double.NaN, 0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0
                });

            Require(result.Success,
                "Finite rigid input must produce transform diagnostics.");
            RequireApproximately(result.HomogeneousRowMaximumError, 0.0, 0.0,
                "Unexpected homogeneous-row error.");
            RequireApproximately(result.RotationOrthogonalityMaximumError, 0.0, 0.0,
                "Unexpected rotation orthogonality error.");
            RequireApproximately(result.RotationDeterminant, 1.0, 0.0,
                "Unexpected rotation determinant.");
            RequireApproximately(result.RotationDeterminantUnitError, 0.0, 0.0,
                "Unexpected determinant-unit error.");
            RequireApproximately(result.TranslationMagnitude, 5.0, 1e-12,
                "Unexpected translation magnitude.");
            RequireApproximately(result.RotationAngleDegrees, 90.0, 1e-12,
                "Unexpected rotation angle.");
            Require(!rejected.Success
                && rejected.Message.IndexOf("16 finite", StringComparison.Ordinal) >= 0,
                "Non-finite transform input must fail closed.");
        }

        private static void TestDeterministicSurfaceModelPreparation()
        {
            ThreeDPoint[] points =
            {
                new ThreeDPoint(0.0, 0.0, 0.0),
                new ThreeDPoint(2.0, 0.0, 0.0),
                new ThreeDPoint(2.0, 2.0, 0.0),
                new ThreeDPoint(0.0, 2.0, 0.0)
            };
            SurfaceModelTriangleInput[] triangles =
            {
                new SurfaceModelTriangleInput(0, 1, 2),
                new SurfaceModelTriangleInput(0, 2, 3)
            };
            ThreeDPoint[] normals = points
                .Select(_ => new ThreeDPoint(0.0, 0.0, 1.0))
                .ToArray();
            DeterministicSurfaceModelPreparationResult result =
                new DeterministicSurfaceModelPreparationTool().Execute(
                    points,
                    triangles,
                    normals,
                    new DeterministicSurfaceModelPreparationOptions
                    {
                        MaximumSampleCount = 1
                    });

            Require(result.Success && result.Samples.Count == 1,
                "Surface-model preparation must return one controlled sample.");
            PreparedSurfaceModelSample sample = result.Samples[0];
            Require(sample.Order == 0 && sample.SourceTriangleIndex == 1,
                "Even triangle selection must preserve the established index schedule.");
            RequireApproximately(sample.Position.X, 2.0 / 3.0, 0.0,
                "Unexpected selected triangle centroid X.");
            RequireApproximately(sample.Position.Y, 4.0 / 3.0, 0.0,
                "Unexpected selected triangle centroid Y.");
            RequireApproximately(sample.Normal.Z, 1.0, 0.0,
                "Declared normal averaging must retain the source orientation.");
        }

        private static void TestDeterministicPreparedScenePreparation()
        {
            ThreeDPoint[] points = Enumerable.Range(0, 5)
                .Select(index => new ThreeDPoint(index, 0.0, index * 0.5))
                .ToArray();
            DeterministicPreparedScenePreparationResult result =
                new DeterministicPreparedScenePreparationTool().Execute(
                    points,
                    new DeterministicPreparedScenePreparationOptions
                    {
                        MaximumSampleCount = 2
                    });

            Require(result.Success && result.Samples.Count == 2,
                "Prepared-scene preparation must return the requested sample count.");
            Require(result.Samples[0].SourcePointIndex == 1
                && result.Samples[1].SourcePointIndex == 3,
                "Even point selection must preserve stable source locators.");
            Require(result.Samples[0].Position == points[1]
                && result.Samples[1].Position == points[3],
                "Prepared-scene samples must preserve the selected source objects.");
        }

        private static void TestDeterministicModelSurfaceEdgeExtraction()
        {
            ThreeDPoint[] points =
            {
                new ThreeDPoint(0.0, 0.0, 0.0),
                new ThreeDPoint(2.0, 0.0, 0.0),
                new ThreeDPoint(2.0, 2.0, 0.0),
                new ThreeDPoint(0.0, 2.0, 0.0)
            };
            SurfaceModelTriangleInput[] triangles =
            {
                new SurfaceModelTriangleInput(0, 1, 2),
                new SurfaceModelTriangleInput(0, 2, 3)
            };
            DeterministicModelSurfaceEdgeExtractionResult result =
                new DeterministicModelSurfaceEdgeExtractionTool().Execute(
                    points,
                    triangles,
                    new DeterministicModelSurfaceEdgeExtractionOptions
                    {
                        MinimumEdgeLength = 0.1,
                        MinimumCreaseAngleDegrees = 1.0,
                        IncludeBoundaryEdges = true
                    });

            Require(result.Success && result.Edges.Count == 4,
                "A flat triangulated square must expose four boundary edges only.");
            Require(result.Edges.All(edge =>
                    edge.Kind == ExtractedModelSurfaceEdgeKind.Boundary),
                "The flat internal diagonal must not be classified as a crease.");
            Require(result.Edges[0].FirstPointIndex == 0
                && result.Edges[0].SecondPointIndex == 1
                && result.Edges[1].FirstPointIndex == 0
                && result.Edges[1].SecondPointIndex == 3,
                "Model edge ordering must use sorted undirected point locators.");
        }

        private static void TestDeterministicOrganizedSceneSurfaceEdgeExtraction()
        {
            ThreeDPoint[] points =
            {
                new ThreeDPoint(0.0, 0.0, 0.0),
                new ThreeDPoint(1.0, 0.0, 2.0),
                new ThreeDPoint(2.0, 0.0, 0.0),
                new ThreeDPoint(0.0, 1.0, 0.0),
                new ThreeDPoint(1.0, 1.0, 2.0),
                new ThreeDPoint(2.0, 1.0, 0.0)
            };
            DeterministicOrganizedSceneSurfaceEdgeExtractionResult result =
                new DeterministicOrganizedSceneSurfaceEdgeExtractionTool()
                    .Execute(
                        points,
                        new DeterministicOrganizedSceneSurfaceEdgeExtractionOptions
                        {
                            Width = 3,
                            Height = 2,
                            MinimumAbsoluteHeightStep = 2.0,
                            IncludeColumnNeighbors = true,
                            IncludeRowNeighbors = false
                        });

            Require(result.Success && result.Edges.Count == 4,
                "Inclusive height-step extraction must retain four column edges.");
            Require(result.Edges[0].AnchorPointIndex == 1
                && result.Edges[1].AnchorPointIndex == 1
                && result.Edges[2].AnchorPointIndex == 4
                && result.Edges[3].AnchorPointIndex == 4,
                "Every organized height step must anchor at its higher endpoint.");
            Require(result.Edges.All(edge =>
                    edge.Axis == ExtractedSceneSurfaceEdgeAxis.AcrossColumns
                    && edge.AbsoluteHeightStep == 2.0),
                "Scene edge axis and threshold evidence were not preserved.");
        }

        private static void TestDeterministicSurfaceEdgeCoverage()
        {
            SurfaceEdgeAnchorSample[] model =
            {
                new SurfaceEdgeAnchorSample(
                    0, new ThreeDPoint(0.0, 0.0, 0.0)),
                new SurfaceEdgeAnchorSample(
                    1, new ThreeDPoint(2.0, 0.0, 0.0))
            };
            SurfaceEdgeAnchorSample[] scene =
            {
                new SurfaceEdgeAnchorSample(
                    0, new ThreeDPoint(0.1, 0.0, 0.0)),
                new SurfaceEdgeAnchorSample(
                    1, new ThreeDPoint(2.1, 0.0, 0.0))
            };
            RigidSurfacePose identity = new RigidSurfacePose(
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0,
                0.0, 0.0, 0.0);
            DeterministicSurfaceEdgeCoverageResult result =
                new DeterministicSurfaceEdgeCoverageTool().Execute(
                    model,
                    scene,
                    identity,
                    0.2);

            Require(result.Success
                && result.MatchedModelEdgeCount == 2
                && result.UnmatchedModelEdgeCount == 0
                && result.Matches.Count == 2,
                "Surface-edge coverage must retain two unique nearest matches.");
            RequireApproximately(result.CoverageRatio, 1.0, 0.0,
                "Surface-edge coverage ratio must remain decision-free and exact.");
            RequireApproximately(result.InlierRmse, 0.1, 1e-12,
                "Unexpected surface-edge coverage RMSE.");
        }

        private static void TestDeterministicSurfaceEdgeCoverageEmptyScene()
        {
            SurfaceEdgeAnchorSample[] model =
            {
                new SurfaceEdgeAnchorSample(
                    0, new ThreeDPoint(0.0, 0.0, 0.0))
            };
            RigidSurfacePose identity = new RigidSurfacePose(
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0,
                0.0, 0.0, 0.0);
            DeterministicSurfaceEdgeCoverageResult result =
                new DeterministicSurfaceEdgeCoverageTool().Execute(
                    model,
                    new SurfaceEdgeAnchorSample[0],
                    identity,
                    0.2);

            Require(result.Success
                && result.ModelEdgeCount == 1
                && result.SceneEdgeCount == 0
                && result.MatchedModelEdgeCount == 0
                && result.UnmatchedModelEdgeCount == 1
                && result.CoverageRatio == 0.0
                && !result.HasInlierRmse
                && result.Matches.Count == 0,
                "An empty scene-edge set must remain valid zero-coverage evidence.");
        }

        private static IReadOnlyList<SurfaceMatchSample> CreateSurfaceMatchModel()
        {
            return new[]
            {
                new SurfaceMatchSample(0, new ThreeDPoint(0.0, 0.0, 0.0)),
                new SurfaceMatchSample(1, new ThreeDPoint(2.0, 0.0, 0.0)),
                new SurfaceMatchSample(2, new ThreeDPoint(0.0, 3.0, 0.0)),
                new SurfaceMatchSample(3, new ThreeDPoint(4.0, 1.0, 0.0)),
                new SurfaceMatchSample(4, new ThreeDPoint(1.0, 5.0, 0.0))
            };
        }

        private static RigidSurfacePose CreateKnownSurfacePose()
        {
            double cosine = Math.Sqrt(3.0) / 2.0;
            return new RigidSurfacePose(
                cosine,
                -0.5,
                0.0,
                0.5,
                cosine,
                0.0,
                0.0,
                0.0,
                1.0,
                10.0,
                -4.0,
                2.0);
        }

        private static DeterministicRigidSurfacePoseSearchOptions
            CreateSurfaceSearchOptions()
        {
            return new DeterministicRigidSurfacePoseSearchOptions
            {
                MinimumRotationXDegrees = 0.0,
                MaximumRotationXDegrees = 0.0,
                RotationStepXDegrees = 1.0,
                MinimumRotationYDegrees = 0.0,
                MaximumRotationYDegrees = 0.0,
                RotationStepYDegrees = 1.0,
                MinimumRotationZDegrees = -45.0,
                MaximumRotationZDegrees = 45.0,
                RotationStepZDegrees = 15.0,
                MinimumTranslationX = 8.0,
                MaximumTranslationX = 12.0,
                MinimumTranslationY = -6.0,
                MaximumTranslationY = -2.0,
                MinimumTranslationZ = 1.0,
                MaximumTranslationZ = 3.0,
                MaximumCorrespondenceDistance = 1e-6,
                MinimumMatchedSampleCount = 3,
                MaximumCandidateCount = 100
            };
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

        private static void TestGapFlush()
        {
            GapFlushInspectionResult result = new GapFlushInspectionTool().Execute(
                0.0, 2.0, 3.0, 5.0,
                new GapFlushRegionStatistics(20, 100.0, 1.0),
                new GapFlushRegionStatistics(30, 104.0, 1.4),
                GapFlushOptions(1.0, 4.0));

            Require(result.Passed, "Analytic gap/flush must pass exact tolerances.");
            RequireApproximately(result.SignedGap, 1.0, 1e-12, "Unexpected signed gap.");
            RequireApproximately(result.SignedFlush, 4.0, 1e-12, "Unexpected signed flush.");
            RequireApproximately(result.SignedReferenceFlush, 0.4, 1e-12, "Unexpected reference-height flush.");
        }

        private static void TestGapFlushOverlap()
        {
            GapFlushInspectionResult result = new GapFlushInspectionTool().Execute(
                0.0, 2.0, 1.5, 3.5,
                new GapFlushRegionStatistics(2, 8.0, 8.0),
                new GapFlushRegionStatistics(2, 9.0, 9.0),
                GapFlushOptions(-0.5, 1.0));

            Require(result.Passed, "Authored overlap must retain its negative signed gap.");
            RequireApproximately(result.SignedGap, -0.5, 1e-12, "Overlap sign was lost.");
        }

        private static void TestGapFlushEmptyRegion()
        {
            try
            {
                new GapFlushInspectionTool().Execute(
                    0.0, 1.0, 2.0, 3.0,
                    new GapFlushRegionStatistics(0, 1.0, 1.0),
                    new GapFlushRegionStatistics(1, 2.0, 2.0),
                    GapFlushOptions(1.0, 1.0));
                throw new InvalidOperationException("Empty gap/flush input must be rejected.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.Message.IndexOf("at least one sample", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Empty gap/flush rejection must name the sample requirement.");
            }
        }

        private static GapFlushInspectionOptions GapFlushOptions(double expectedGap, double expectedFlush) =>
            new GapFlushInspectionOptions
            {
                ExpectedGap = expectedGap,
                GapTolerance = 1e-9,
                ExpectedFlush = expectedFlush,
                FlushTolerance = 1e-9
            };

        private static void TestVolume()
        {
            HeightFieldPlaneFitSample[] reference = CreateAnalyticPlaneSamples(0.5, -0.25, 2.0, new double[9]);
            HeightFieldPlaneFitSample[] measurement = CreateAnalyticPlaneSamples(
                0.5, -0.25, 2.0, new[] { 1.0, 1.0, 1.0, 1.0, 0.0, -1.0, -1.0, -1.0, -1.0 });
            double normalLength = Math.Sqrt(1.3125);
            VolumeInspectionResult result = new VolumeInspectionTool().Execute(
                reference,
                measurement,
                VolumeOptions(0.5, 0.0, 1e-9));

            Require(result.Passed, "Balanced analytic volume must pass.");
            RequireApproximately(result.AboveVolume, 2.0 * normalLength, 1e-10, "Unexpected above-plane volume.");
            RequireApproximately(result.BelowVolume, 2.0 * normalLength, 1e-10, "Unexpected below-plane volume.");
            RequireApproximately(result.NetVolume, 0.0, 1e-10, "Balanced volume must have zero net value.");
        }

        private static void TestVolumeBelowPlane()
        {
            HeightFieldPlaneFitSample[] reference = CreateAnalyticPlaneSamples(0.0, 0.0, 3.0, new double[9]);
            HeightFieldPlaneFitSample[] measurement = CreateAnalyticPlaneSamples(0.0, 0.0, 3.0, new[] { -2.0, -2.0, -2.0, -2.0, -2.0, -2.0, -2.0, -2.0, -2.0 });
            VolumeInspectionResult result = new VolumeInspectionTool().Execute(
                reference,
                measurement,
                VolumeOptions(0.25, 0.0, 1.0));

            Require(!result.Passed, "Out-of-tolerance below-plane volume must fail.");
            RequireApproximately(result.AboveVolume, 0.0, 1e-12, "Below-plane data must not add above volume.");
            RequireApproximately(result.BelowVolume, 4.5, 1e-12, "Unexpected below-plane volume.");
            RequireApproximately(result.NetVolume, -4.5, 1e-12, "Below-plane net volume must remain negative.");
        }

        private static void TestVolumeEmptyMeasurement()
        {
            try
            {
                new VolumeInspectionTool().Execute(
                    CreateAnalyticPlaneSamples(0.0, 0.0, 0.0, new double[9]),
                    new HeightFieldPlaneFitSample[0],
                    VolumeOptions(1.0, 0.0, 0.0));
                throw new InvalidOperationException("Empty volume measurement input must be rejected.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.Message.IndexOf("at least one sample", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Empty volume rejection must name the sample requirement.");
            }
        }

        private static VolumeInspectionOptions VolumeOptions(double sampleArea, double expectedNetVolume, double tolerance) =>
            new VolumeInspectionOptions
            {
                SampleArea = sampleArea,
                ExpectedNetVolume = expectedNetVolume,
                Tolerance = tolerance
            };

        private static void TestCrossSectionDimensions()
        {
            CrossSectionDimensionsInspectionResult result = new CrossSectionDimensionsInspectionTool().Execute(
                new[]
                {
                    new CrossSectionDimensionsSample(2, -1.5, 10.0),
                    new CrossSectionDimensionsSample(3, 0.5, 15.0),
                    new CrossSectionDimensionsSample(4, 3.5, 5.0)
                },
                CrossSectionOptions(5.0, 10.0));

            Require(result.Passed, "Analytic cross-section must pass exact acceptance.");
            RequireApproximately(result.Width, 5.0, 1e-12, "Unexpected cross-section width.");
            RequireApproximately(result.HeightRange, 10.0, 1e-12, "Unexpected cross-section height range.");
            RequireApproximately(result.HeightMinimum, 5.0, 1e-12, "Unexpected cross-section minimum height.");
            RequireApproximately(result.HeightMaximum, 15.0, 1e-12, "Unexpected cross-section maximum height.");
        }

        private static void TestCrossSectionDimensionsFailure()
        {
            CrossSectionDimensionsInspectionResult result = new CrossSectionDimensionsInspectionTool().Execute(
                new[]
                {
                    new CrossSectionDimensionsSample(0, 0.0, 2.0),
                    new CrossSectionDimensionsSample(1, 4.0, 8.0)
                },
                new CrossSectionDimensionsInspectionOptions
                {
                    ExpectedWidth = 3.0,
                    WidthTolerance = 0.1,
                    ExpectedHeightRange = 6.0,
                    HeightTolerance = 0.1
                });

            Require(!result.Passed && !result.WidthPassed && result.HeightPassed,
                "Cross-section acceptance must retain independent metric status.");
        }

        private static void TestCrossSectionDimensionsInvalidSample()
        {
            try
            {
                new CrossSectionDimensionsInspectionTool().Execute(
                    new[]
                    {
                        new CrossSectionDimensionsSample(0, 0.0, 1.0),
                        new CrossSectionDimensionsSample(1, double.NaN, 2.0)
                    },
                    CrossSectionOptions(1.0, 1.0));
                throw new InvalidOperationException("Non-finite cross-section samples must be rejected.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.Message.IndexOf("finite", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Cross-section rejection must explain the finite sample contract.");
            }
        }

        private static CrossSectionDimensionsInspectionOptions CrossSectionOptions(double expectedWidth, double expectedHeightRange) =>
            new CrossSectionDimensionsInspectionOptions
            {
                ExpectedWidth = expectedWidth,
                WidthTolerance = 1e-9,
                ExpectedHeightRange = expectedHeightRange,
                HeightTolerance = 1e-9
            };

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

        private static void TestAffineTransformKnownMatrix()
        {
            using (Mat source = new Mat(new Size(160, 120), MatType.CV_8UC1, Scalar.All(0)))
            {
                Cv2.Rectangle(source, new Rect(20, 20, 50, 40), Scalar.All(255), -1);
                AffineTransformTool tool = new AffineTransformTool();
                tool.SetProperty(new AffineTransformToolProperty
                {
                    SourcePoint1X = 0,
                    SourcePoint1Y = 0,
                    SourcePoint2X = 100,
                    SourcePoint2Y = 0,
                    SourcePoint3X = 0,
                    SourcePoint3Y = 100,
                    DestinationPoint1X = 12,
                    DestinationPoint1Y = 18,
                    DestinationPoint2X = 132,
                    DestinationPoint2Y = 8,
                    DestinationPoint3X = 37,
                    DestinationPoint3Y = 108,
                    OutputWidth = 240,
                    OutputHeight = 180,
                    MinimumSourceTriangleArea = 100,
                    MinimumDestinationTriangleArea = 100,
                    MinimumValidPixelRatio = 0.4
                });

                VisionToolResult result = tool.Execute(source);
                try
                {
                    Require(result.Success, "Known 2D affine transform must pass. " + result.ErrorName + ": " + result.Message);
                    Require(result.ResultImage != null && result.ResultImage.Width == 240 && result.ResultImage.Height == 180,
                        "2D affine transform did not honor the taught output size.");
                    RequireApproximately(result.Metrics["AffineM11"], 1.2, 1e-6, "Unexpected affine M11.");
                    RequireApproximately(result.Metrics["AffineM12"], 0.25, 1e-6, "Unexpected affine M12.");
                    RequireApproximately(result.Metrics["AffineM13"], 12.0, 1e-6, "Unexpected affine M13.");
                    RequireApproximately(result.Metrics["AffineM21"], -0.1, 1e-6, "Unexpected affine M21.");
                    RequireApproximately(result.Metrics["AffineM22"], 0.9, 1e-6, "Unexpected affine M22.");
                    RequireApproximately(result.Metrics["AffineM23"], 18.0, 1e-6, "Unexpected affine M23.");
                    Require(result.Metrics["AffineValidPixelRatio"] >= 0.4,
                        "Known 2D affine transform did not retain the declared source coverage.");
                    Require(result.Overlays.Count == 10,
                        "2D affine transform must retain three destination points, three destination edges, and four frame edges.");
                }
                finally
                {
                    result.ResultImage?.Dispose();
                }
            }
        }

        private static void TestAffineTransformDegenerateSource()
        {
            using (Mat source = new Mat(new Size(64, 64), MatType.CV_8UC1, Scalar.All(255)))
            {
                AffineTransformTool tool = new AffineTransformTool();
                tool.SetProperty(new AffineTransformToolProperty
                {
                    SourcePoint1X = 0,
                    SourcePoint1Y = 0,
                    SourcePoint2X = 10,
                    SourcePoint2Y = 10,
                    SourcePoint3X = 20,
                    SourcePoint3Y = 20,
                    MinimumSourceTriangleArea = 0
                });

                VisionToolResult result = tool.Execute(source);
                Require(!result.Success && result.ErrorCode == VisionToolErrorCode.AffineDegenerateSource,
                    "Collinear source teaching must fail with AffineDegenerateSource even when the operator area gate is zero.");
                Require(result.ResultStatus == VisionToolResultStatus.InvalidParameter,
                    "Collinear source teaching must be classified as an invalid parameter.");
            }
        }

        private static void TestAffineTransformCoverageFailure()
        {
            using (Mat source = new Mat(new Size(64, 64), MatType.CV_8UC1, Scalar.All(255)))
            {
                AffineTransformTool tool = new AffineTransformTool();
                tool.SetProperty(new AffineTransformToolProperty
                {
                    DestinationPoint1X = 500,
                    DestinationPoint1Y = 500,
                    DestinationPoint2X = 600,
                    DestinationPoint2Y = 500,
                    DestinationPoint3X = 500,
                    DestinationPoint3Y = 600,
                    OutputWidth = 64,
                    OutputHeight = 64,
                    MinimumValidPixelRatio = 0.1
                });

                VisionToolResult result = tool.Execute(source);
                try
                {
                    Require(!result.Success && result.ErrorCode == VisionToolErrorCode.AffineInsufficientCoverage,
                        "Off-frame affine teaching must fail with AffineInsufficientCoverage.");
                    Require(result.ResultImage != null && !result.ResultImage.Empty(),
                        "Coverage failure must retain the transformed image for correction evidence.");
                    Require(result.Metrics.ContainsKey("AffineValidPixelRatio")
                        && result.Metrics["AffineValidPixelRatio"] == 0,
                        "Coverage failure must retain the measured valid-pixel ratio.");
                    Require(result.Overlays.Count == 10,
                        "Coverage failure must retain the taught geometry overlays.");
                }
                finally
                {
                    result.ResultImage?.Dispose();
                }
            }
        }

        private static void TestAutoMPointUniquePattern()
        {
            using (Mat source = CreateAutoMPointUniqueSource())
            {
                AutoMPointToolProperty property = CreateAutoMPointProperty(
                    new Rect(0, 0, source.Width, source.Height),
                    64,
                    64,
                    32);
                AutoMPointTool firstTool = new AutoMPointTool();
                firstTool.SetProperty(property);
                AutoMPointTool secondTool = new AutoMPointTool();
                secondTool.SetProperty(property);

                VisionToolResult first = firstTool.Execute(source);
                VisionToolResult second = secondTool.Execute(source);
                try
                {
                    Require(first.Success, "Unique Auto MPoint source must produce a suggestion. " + first.ErrorName + ": " + first.Message);
                    Require(second.Success, "Repeated Auto MPoint execution must produce a suggestion. " + second.ErrorName + ": " + second.Message);
                    Require(firstTool.results.Count > 0 && secondTool.results.Count == firstTool.results.Count,
                        "Auto MPoint must retain the same non-empty result count.");
                    Require(firstTool.results[0].Accepted && firstTool.results[0].Rank == 1,
                        "Auto MPoint best result must be accepted and ranked first.");
                    Require(firstTool.results[0].UniquenessMargin >= property.MinimumUniquenessMargin,
                        "Auto MPoint best result must satisfy the uniqueness gate.");
                    Require(firstTool.results[0].SyntheticSuccessRate >= property.MinimumSyntheticSuccessRate,
                        "Auto MPoint best result must satisfy the synthetic stability gate.");
                    Require(firstTool.results[0].PositionErrorMaxPixels <= property.MaximumPositionErrorPixels,
                        "Auto MPoint best result must satisfy the position precision gate.");
                    Require(double.IsFinite(firstTool.results[0].RuntimeMedianMilliseconds)
                        && double.IsFinite(firstTool.results[0].RuntimeP95Milliseconds),
                        "Auto MPoint must publish finite runtime measurements.");
                    Require(first.Overlays.Count == firstTool.results.Count * 2,
                        "Auto MPoint must publish one pattern rectangle and one MPoint overlay per result.");
                    Require(firstTool.results.Select(candidate => candidate.PatternRoi)
                        .SequenceEqual(secondTool.results.Select(candidate => candidate.PatternRoi)),
                        "Auto MPoint result ranking must be deterministic for the same source.");
                    Require(Cv2.Norm(first.ResultImage, second.ResultImage, NormTypes.L1) == 0d,
                        "Auto MPoint result drawing must be deterministic for the same source.");

                    SaveAutoMPointEvidence(
                        "unique",
                        source,
                        first,
                        new[]
                        {
                            "Status=Accepted",
                            "ResultCount=" + firstTool.results.Count,
                            "BestPatternRoi=" + firstTool.results[0].PatternRoi,
                            "BestScore=" + firstTool.results[0].Score.ToString("0.000"),
                            "BestUniquenessMargin=" + firstTool.results[0].UniquenessMargin.ToString("0.000000"),
                            "BestPositionErrorMaxPx=" + firstTool.results[0].PositionErrorMaxPixels.ToString("0.000"),
                            "BestRuntimeMedianMs=" + firstTool.results[0].RuntimeMedianMilliseconds.ToString("0.000"),
                            "BestRuntimeP95Ms=" + firstTool.results[0].RuntimeP95Milliseconds.ToString("0.000")
                        });
                }
                finally
                {
                    first.ResultImage?.Dispose();
                    second.ResultImage?.Dispose();
                }
            }
        }

        private static void TestAutoMPointRepeatedPattern()
        {
            using (Mat source = CreateAutoMPointRepeatedSource())
            {
                AutoMPointToolProperty property = CreateAutoMPointProperty(
                    new Rect(0, 0, 128, 64),
                    64,
                    64,
                    64);
                property.MaximumFinalists = 2;
                property.MaximumResults = 2;
                property.MinimumUniquenessMargin = 0.1;

                AutoMPointTool tool = new AutoMPointTool();
                tool.SetProperty(property);
                VisionToolResult result = tool.Execute(source);
                try
                {
                    Require(!result.Success && result.ErrorCode == VisionToolErrorCode.AutoMPointNoCandidate,
                        "Two identical patterns must fail with AutoMPointNoCandidate.");
                    Require(tool.candidates.Count == 2 && tool.results.Count == 0,
                        "Both repeated candidates must be evaluated and neither may be suggested.");
                    Require(tool.candidates.All(candidate =>
                            !candidate.Accepted
                            && candidate.RejectReason.IndexOf("UniquenessMargin", StringComparison.Ordinal) >= 0),
                        "Repeated patterns must fail specifically at the uniqueness gate.");

                    SaveAutoMPointEvidence(
                        "repeated",
                        source,
                        result,
                        new[]
                        {
                            "Status=Rejected",
                            "ErrorCode=" + result.ErrorCode,
                            "CandidateCount=" + tool.candidates.Count,
                            "AcceptedCount=" + tool.results.Count,
                            "Candidate1Reason=" + tool.candidates[0].RejectReason,
                            "Candidate2Reason=" + tool.candidates[1].RejectReason
                        });
                }
                finally
                {
                    result.ResultImage?.Dispose();
                }
            }
        }

        private static void TestAutoMPointInvalidDefinition()
        {
            using (Mat source = CreateAutoMPointUniqueSource())
            {
                AutoMPointTool invalidRoiTool = new AutoMPointTool();
                invalidRoiTool.SetProperty(CreateAutoMPointProperty(
                    new Rect(source.Width - 10, source.Height - 10, 64, 64),
                    64,
                    64,
                    32));
                VisionToolResult invalidRoi = invalidRoiTool.Execute(source);
                Require(!invalidRoi.Success && invalidRoi.ErrorCode == VisionToolErrorCode.AutoMPointInvalidRoi,
                    "Out-of-image Auto MPoint ROI must fail with AutoMPointInvalidRoi.");

                AutoMPointTool invalidPatternTool = new AutoMPointTool();
                invalidPatternTool.SetProperty(CreateAutoMPointProperty(
                    new Rect(0, 0, 80, 80),
                    96,
                    96,
                    16));
                VisionToolResult invalidPattern = invalidPatternTool.Execute(source);
                Require(!invalidPattern.Success && invalidPattern.ErrorCode == VisionToolErrorCode.AutoMPointInvalidPatternSize,
                    "Oversized Auto MPoint pattern must fail with AutoMPointInvalidPatternSize.");
            }
        }

        private static void TestAutoMPointRepresentativeBestPattern()
        {
            using (Mat reference = CreateAutoMPointRepresentativeReference())
            {
                AutoMPointToolProperty property = CreateAutoMPointProperty(
                    new Rect(0, 0, reference.Width, reference.Height),
                    64,
                    64,
                    32);
                property.MaximumFinalists = 8;
                property.MaximumResults = 5;
                property.MinimumFeatureQuality = 0.01;
                property.MatchingMinimumScore = 0.45;
                property.MinimumUniquenessMargin = 0.01;
                property.MinimumRepresentativeImageCount = 3;
                property.MinimumRepresentativeSuccessRate = 0.75;

                List<Mat> samples = Enumerable.Range(0, 4)
                    .Select(index => CreateAutoMPointRepresentativeSample(reference, index))
                    .ToList();
                try
                {
                    AutoMPointTool tool = new AutoMPointTool();
                    tool.SetProperty(property);
                    VisionToolResult result = tool.Execute(reference, samples);
                    try
                    {
                        Require(result.Success,
                            "Representative Auto MPoint analysis must produce one stable suggestion. "
                            + result.ErrorName + ": " + result.Message + " Candidates="
                            + string.Join(
                                " | ",
                                tool.candidates.Select(candidate =>
                                    candidate.PatternRoi + " "
                                    + candidate.RepresentativeSuccessCount + "/"
                                    + candidate.RepresentativeImageCount + " ["
                                    + candidate.RejectReason + "] "
                                    + string.Join(
                                        ",",
                                        candidate.RepresentativeMatches.Select(match =>
                                            match.Outcome + ":" + match.Score.ToString("0.0")
                                            + "/" + match.UniquenessMargin.ToString("0.000"))))));
                        Require(tool.results.Count >= 1
                            && tool.results[0].PatternRoi == new Rect(64, 64, 64, 64),
                            "The pattern preserved across representative images must rank first.");
                        Require(tool.results[0].RepresentativeImageCount == 4
                            && tool.results[0].RepresentativeSuccessCount == 4
                            && Math.Abs(tool.results[0].RepresentativeSuccessRate - 1d) < 0.000001d,
                            "The best pattern must publish 4/4 representative-image success.");
                        Require(tool.results[0].RepresentativeMatches.Count == 4
                            && tool.results[0].RepresentativeMatches.All(match => match.Success),
                            "Per-image representative outcomes must be retained.");
                        Require(result.Metrics["AutoMPoint.RepresentativeImageCount"] == 4d
                            && result.Metrics["AutoMPoint.BestRepresentativeSuccessRate"] == 1d,
                            "Representative-image count and best success rate must be public metrics.");
                        SaveAutoMPointEvidence(
                            "representative_best",
                            reference,
                            result,
                            new[]
                            {
                                "Status=Accepted",
                                "BestPatternRoi=" + tool.results[0].PatternRoi,
                                "RepresentativeImages=" + tool.results[0].RepresentativeImageCount,
                                "RepresentativeSuccess=" + tool.results[0].RepresentativeSuccessCount,
                                "RepresentativeSuccessRate=" + tool.results[0].RepresentativeSuccessRate.ToString("0.000"),
                                "RepresentativeMeanScore=" + tool.results[0].RepresentativeMeanScore.ToString("0.000"),
                                "RepresentativeMinimumUniquenessMargin="
                                    + tool.results[0].RepresentativeMinimumUniquenessMargin.ToString("0.000000")
                            });
                    }
                    finally
                    {
                        result.ResultImage?.Dispose();
                    }
                }
                finally
                {
                    foreach (Mat sample in samples)
                    {
                        sample.Dispose();
                    }
                }
            }
        }

        private static void TestAutoMPointInvalidRepresentativeSet()
        {
            using (Mat reference = CreateAutoMPointRepresentativeReference())
            using (Mat sample = reference.Clone())
            {
                AutoMPointToolProperty property = CreateAutoMPointProperty(
                    new Rect(0, 0, reference.Width, reference.Height),
                    64,
                    64,
                    32);
                property.MinimumRepresentativeImageCount = 3;
                AutoMPointTool tool = new AutoMPointTool();
                tool.SetProperty(property);
                VisionToolResult result = tool.Execute(reference, new[] { sample });
                Require(!result.Success
                    && result.ErrorCode == VisionToolErrorCode.AutoMPointRepresentativeImageInvalid,
                    "Too few representative images must fail closed with AutoMPointRepresentativeImageInvalid.");
            }
        }

        private static AutoMPointToolProperty CreateAutoMPointProperty(
            Rect analysisRoi,
            int patternWidth,
            int patternHeight,
            int stride)
        {
            return new AutoMPointToolProperty
            {
                UseAnalysisRoi = true,
                AnalysisRoi = analysisRoi,
                CandidateMode = AutoMPointCandidateMode.Grid,
                PatternWidth = patternWidth,
                PatternHeight = patternHeight,
                CandidateStride = stride,
                MaximumFinalists = 6,
                MaximumResults = 3,
                MaximumCandidateOverlap = 0.05,
                MinimumContrastStdDev = 2,
                MinimumEdgeDensity = 0.002,
                MinimumQuadrantBalance = 0.02,
                MinimumOrientationBalance = 0.05,
                MinimumFeatureQuality = 0.05,
                MatchingMinimumScore = 0.5,
                MinimumUniquenessMargin = 0.03,
                MaximumTemplatePoints = 250,
                SearchStep = 2,
                UsePositionRefine = true,
                UseSubpixelRefine = true,
                UsePyramidPositionProposal = true,
                UseHybridVerify = true,
                UseAngleSearch = false,
                UseScaleSearch = false,
                SyntheticTranslationPixels = 3,
                MinimumSyntheticSuccessRate = 1,
                MaximumPositionErrorPixels = 5,
                MaximumAngleErrorDegrees = 0.1,
                MaximumScaleErrorRatio = 0.001
            };
        }

        private static Mat CreateAutoMPointUniqueSource()
        {
            Mat source = new Mat(new Size(256, 192), MatType.CV_8UC1, Scalar.All(24));
            Cv2.Rectangle(source, new Rect(66, 66, 50, 50), Scalar.All(205), 3);
            Cv2.Line(source, new Point(72, 108), new Point(109, 73), Scalar.All(245), 3, LineTypes.AntiAlias);
            Cv2.Circle(source, new Point(101, 99), 8, Scalar.All(90), -1, LineTypes.AntiAlias);
            Cv2.Rectangle(source, new Rect(142, 38, 54, 14), Scalar.All(130), -1);
            Cv2.Line(source, new Point(154, 148), new Point(220, 148), Scalar.All(105), 4);
            return source;
        }

        private static Mat CreateAutoMPointRepeatedSource()
        {
            Mat source = new Mat(new Size(128, 64), MatType.CV_8UC1, Scalar.All(24));
            DrawRepeatedAutoMPointMark(source, 0);
            DrawRepeatedAutoMPointMark(source, 64);
            return source;
        }

        private static Mat CreateAutoMPointRepresentativeReference()
        {
            Mat source = CreateAutoMPointUniqueSource();
            Cv2.Rectangle(source, new Rect(166, 70, 50, 50), Scalar.All(215), 3);
            Cv2.Line(source, new Point(171, 114), new Point(211, 74), Scalar.All(250), 4, LineTypes.AntiAlias);
            Cv2.Circle(source, new Point(204, 106), 9, Scalar.All(70), -1, LineTypes.AntiAlias);
            Cv2.Line(source, new Point(166, 96), new Point(216, 96), Scalar.All(180), 2, LineTypes.AntiAlias);
            return source;
        }

        private static Mat CreateAutoMPointRepresentativeSample(Mat reference, int index)
        {
            Mat sample = reference.Clone();
            Cv2.Rectangle(sample, new Rect(160, 64, 64, 64), Scalar.All(24), -1);
            Cv2.Line(
                sample,
                new Point(166 + (index * 3), 72),
                new Point(214, 119 - (index * 4)),
                Scalar.All(48 + (index * 7)),
                2,
                LineTypes.AntiAlias);
            return sample;
        }

        private static void DrawRepeatedAutoMPointMark(Mat source, int offsetX)
        {
            Cv2.Rectangle(source, new Rect(offsetX + 8, 8, 46, 46), Scalar.All(205), 3);
            Cv2.Line(
                source,
                new Point(offsetX + 13, 49),
                new Point(offsetX + 48, 14),
                Scalar.All(245),
                3,
                LineTypes.AntiAlias);
            Cv2.Circle(source, new Point(offsetX + 42, 42), 6, Scalar.All(90), -1, LineTypes.AntiAlias);
        }

        private static void SaveAutoMPointEvidence(
            string name,
            Mat source,
            VisionToolResult result,
            IEnumerable<string> summary)
        {
            string directory = Environment.GetEnvironmentVariable("LIB_NOAH_AUTOMPOINT_EVIDENCE_DIR");
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            Cv2.ImWrite(Path.Combine(directory, name + "_source.png"), source);
            if (result?.ResultImage != null && !result.ResultImage.Empty())
            {
                Cv2.ImWrite(Path.Combine(directory, name + "_result.png"), result.ResultImage);
            }

            File.WriteAllLines(Path.Combine(directory, name + "_summary.txt"), summary ?? Array.Empty<string>());
        }

        private static void TestEdgeMatcherLegacySingleResult()
        {
            using (Mat source = CreateAutoMPointRepeatedSource())
            using (Mat template = new Mat(source, new Rect(0, 0, 64, 64)).Clone())
            {
                EdgeBasedTemplateMatchingTool tool = CreateEdgeMatcher(template, false);
                VisionToolResult result = tool.Execute(source);
                try
                {
                    Require(result.Success && tool.results.Count == 1,
                        "The opt-in contract must not change a legacy NUM_MATCH=1 repeated-pattern result. "
                        + result.ErrorName + ": " + result.Message
                        + " Count=" + tool.results.Count);
                    Require(result.Metrics["UniqueMatch.Enabled"] == 0D,
                        "Legacy execution must report the unique-match option as disabled.");
                    Require(double.IsNaN(tool.results[0].ScoreMargin),
                        "Legacy MatchingResult must not publish a synthetic uniqueness margin.");
                    Require(result.EdgeBasedMatchingDiagnostics != null
                        && result.EdgeBasedMatchingDiagnostics.State == "Success"
                        && result.EdgeBasedMatchingDiagnostics.ModelPoints.Count > 0
                        && result.EdgeBasedMatchingDiagnostics.SelectedCandidate != null,
                        "Legacy success must retain read-only model and selected-candidate diagnostics.");
                    SaveUniqueMatchEvidence("legacy_repeated_success", source, result, tool);
                }
                finally
                {
                    result.ResultImage?.Dispose();
                }
            }
        }

        private static void TestEdgeMatcherUniqueSuccess()
        {
            using (Mat source = CreateAutoMPointUniqueSource())
            using (Mat template = new Mat(source, new Rect(60, 60, 64, 64)).Clone())
            {
                EdgeBasedTemplateMatchingTool tool = CreateEdgeMatcher(template, true);
                VisionToolResult result = tool.Execute(source);
                try
                {
                    Require(result.Success && tool.results.Count == 1,
                        "One distinct pattern must produce exactly one unique MatchingResult. "
                        + result.ErrorName + ": " + result.Message);
                    Require(result.Metrics["UniqueMatch.State"] == 2D,
                        "A unique result must publish UniqueMatch.State=Success.");
                    Require(tool.results[0].ScoreMargin >= 3D,
                        "A unique result must expose the score margin in percentage points.");
                    Require(tool.results[0].FinalScore >= tool.results[0].EdgeScore - 0.001D,
                        "Non-hybrid final score must preserve the edge score.");
                    Require(result.EdgeBasedMatchingDiagnostics != null
                        && result.EdgeBasedMatchingDiagnostics.State == "Success"
                        && result.EdgeBasedMatchingDiagnostics.ModelPoints.Count > 0
                        && result.EdgeBasedMatchingDiagnostics.SelectedCandidate != null
                        && result.EdgeBasedMatchingDiagnostics.Reason.StartsWith("Success:", StringComparison.Ordinal),
                        "Unique success must retain its exact read-only model, candidate, state, and reason.");
                    SaveUniqueMatchEvidence("unique_success", source, result, tool);
                }
                finally
                {
                    result.ResultImage?.Dispose();
                }
            }
        }

        private static void TestEdgeMatcherUniqueAmbiguous()
        {
            using (Mat source = CreateAutoMPointRepeatedSource())
            using (Mat template = new Mat(source, new Rect(0, 0, 64, 64)).Clone())
            {
                EdgeBasedTemplateMatchingTool tool = CreateEdgeMatcher(template, true);
                VisionToolResult result = tool.Execute(source);
                try
                {
                    Require(!result.Success
                        && result.ErrorCode == VisionToolErrorCode.MatchingAmbiguous
                        && tool.results.Count == 0,
                        "Two repeated patterns must fail closed with MatchingAmbiguous and no MatchingResult.");
                    Require(result.Metrics["UniqueMatch.State"] == 3D
                        && result.Metrics["UniqueMatch.PlausibleAlternativeCount"] >= 1D,
                        "Ambiguous execution must retain its state and alternative count.");
                    Require(result.Metrics["UniqueMatch.ScoreMargin"] < result.Metrics["UniqueMatch.MinimumScoreMargin"],
                        "Ambiguous execution must expose the failed normalized score-margin gate.");
                    Require(result.Message.IndexOf("PlausibleAlternatives=", StringComparison.Ordinal) >= 0,
                        "Ambiguous execution must expose the exact reject reason.");
                    Require(result.EdgeBasedMatchingDiagnostics != null
                        && result.EdgeBasedMatchingDiagnostics.State == "Ambiguous"
                        && result.EdgeBasedMatchingDiagnostics.ModelPoints.Count > 0
                        && result.EdgeBasedMatchingDiagnostics.SelectedCandidate != null
                        && result.EdgeBasedMatchingDiagnostics.StrongestSpatialAlternative != null
                        && result.EdgeBasedMatchingDiagnostics.Reason == result.Message,
                        "Ambiguous execution must retain the exact selected/alternative geometry and runtime reason.");
                    SaveUniqueMatchEvidence("repeated_ambiguous", source, result, tool);
                }
                finally
                {
                    result.ResultImage?.Dispose();
                }
            }
        }

        private static void TestEdgeMatcherUniqueNoMatch()
        {
            using (Mat templateSource = CreateAutoMPointRepeatedSource())
            using (Mat template = new Mat(templateSource, new Rect(0, 0, 64, 64)).Clone())
            using (Mat source = new Mat(new Size(128, 64), MatType.CV_8UC1, Scalar.All(24)))
            {
                EdgeBasedTemplateMatchingTool tool = CreateEdgeMatcher(template, true);
                VisionToolResult result = tool.Execute(source);
                try
                {
                    Require(!result.Success
                        && result.ErrorCode == VisionToolErrorCode.MatchingNoResult
                        && tool.results.Count == 0,
                        "A source without the pattern must fail closed with MatchingNoResult.");
                    Require(result.Metrics["UniqueMatch.State"] == 1D,
                        "No-match execution must publish UniqueMatch.State=NoMatch.");
                    Require(result.EdgeBasedMatchingDiagnostics != null
                        && result.EdgeBasedMatchingDiagnostics.State == "NoMatch"
                        && result.EdgeBasedMatchingDiagnostics.ModelPoints.Count > 0
                        && result.EdgeBasedMatchingDiagnostics.Reason == result.Message,
                        "No-match execution must retain the trained model and exact runtime reason.");
                    SaveUniqueMatchEvidence("no_match", source, result, tool);
                }
                finally
                {
                    result.ResultImage?.Dispose();
                }
            }
        }

        private static EdgeBasedTemplateMatchingTool CreateEdgeMatcher(Mat template, bool useUniqueMatchValidation)
        {
            EdgeBasedTemplateMatchingTool tool = new EdgeBasedTemplateMatchingTool();
            tool.SetProperty(new SmokeEdgeMatcherProperty
            {
                USE_UNIQUE_MATCH_VALIDATION = useUniqueMatchValidation
            });
            tool.SetTemplateImage(template);
            return tool;
        }

        private static void SaveUniqueMatchEvidence(
            string name,
            Mat source,
            VisionToolResult result,
            EdgeBasedTemplateMatchingTool tool)
        {
            string directory = Environment.GetEnvironmentVariable("LIB_NOAH_UNIQUE_MATCH_EVIDENCE_DIR");
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            Cv2.ImWrite(Path.Combine(directory, name + "_source.png"), source);
            if (result?.ResultImage != null && !result.ResultImage.Empty())
            {
                Cv2.ImWrite(Path.Combine(directory, name + "_result.png"), result.ResultImage);
            }

            List<string> summary = new List<string>
            {
                "Success=" + result.Success,
                "ErrorCode=" + result.ErrorCode,
                "Message=" + result.Message,
                "MatchingResultCount=" + tool.results.Count
            };
            foreach (KeyValuePair<string, double> metric in result.Metrics
                .Where(metric => metric.Key.StartsWith("UniqueMatch.", StringComparison.Ordinal))
                .OrderBy(metric => metric.Key, StringComparer.Ordinal))
            {
                summary.Add(metric.Key + "=" + metric.Value.ToString("0.######"));
            }

            if (tool.results.Count > 0)
            {
                summary.Add("EdgeScore=" + tool.results[0].EdgeScore.ToString("0.###"));
                summary.Add("ImageScore=" + tool.results[0].ImageScore.ToString("0.###"));
                summary.Add("FinalScore=" + tool.results[0].FinalScore.ToString("0.###"));
                summary.Add("ScoreMargin=" + tool.results[0].ScoreMargin.ToString("0.###"));
            }

            File.WriteAllLines(Path.Combine(directory, name + "_summary.txt"), summary);
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

        private sealed class SmokeEdgeMatcherProperty : IOpenCVPropertyEdgeBasedTemplateMatching
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
