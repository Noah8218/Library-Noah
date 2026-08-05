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
using static OpenVisionLab.Inspection.Smoke.SmokeAssert;
using static OpenVisionLab.Inspection.Smoke.SmokeFixtures;

namespace OpenVisionLab.Inspection.Smoke
{
    internal static class CombinedInspectionSmokeSuite
    {
        internal static IEnumerable<SmokeCase> Cases()
        {
            yield return new SmokeCase("Combined result disposal releases owned 2D result images", TestCombinedResultResourceOwnership);
            yield return new SmokeCase("Combined runner executes 2D and 3D pass steps", TestCombinedRunnerPass);
            yield return new SmokeCase("Combined runner retains later 3D evidence after 2D failure", TestCombinedRunnerContinuesAfterFailure);
            yield return new SmokeCase("Combined runner converts a 3D exception to a result", TestCombinedRunnerCatchesThreeDException);
            yield return new SmokeCase("Combined runner tolerates a throwing tool name", TestCombinedRunnerToleratesThrowingToolName);
            yield return new SmokeCase("Combined runner rejects an empty configuration", TestCombinedRunnerEmptyConfiguration);
        }

        private static void TestCombinedResultResourceOwnership()
        {
            using (Mat source = new Mat(3, 3, MatType.CV_8UC1, new Scalar(4)))
            {
                CombinedInspectionRunResult result = new CombinedInspectionRunner().Run(
                    new CombinedInspectionInput { Image = source },
                    new IVisionTool[] { new ImageReturningVisionTool() },
                    null);

                Require(result.Success, "The combined ownership fixture must pass.");
                Mat resultSnapshot = result.Steps[0].VisionResult.ResultImage;
                Require(resultSnapshot != null && !resultSnapshot.IsDisposed, "The combined runner did not retain its 2D result image.");

                result.Dispose();
                Require(resultSnapshot.IsDisposed, "Disposing a combined result did not release its 2D result image.");
                Require(!source.IsDisposed, "Disposing a combined result released the caller-owned input image.");
                result.Dispose();
            }
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
    }
}
