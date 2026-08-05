using OpenCvSharp;
using OpenVisionLab.Inspection;
using OpenVisionLab.Vision2D.Blob;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;
using OpenVisionLab.Vision3D.FeatureExtraction;
using OpenVisionLab.Vision3D.Geometry;
using OpenVisionLab.Vision3D.Inspection;

using Mat image = new Mat(2, 2, MatType.CV_8UC1, new Scalar(100));
using ThresholdTool threshold = new ThresholdTool();
threshold.SetProperty(new ThresholdToolProperty
{
    Threshold = 50,
    MaxValue = 255,
    ThresholdType = ThresholdTypes.Binary
});

HeightMap3D heightMap = HeightMap3D.FromArray(
    new[,] { { 1.0, 1.1 }, { 1.2, 1.3 } },
    0.0,
    0.0,
    1.0,
    1.0,
    "mm",
    "mm",
    "fixture",
    "package-smoke");
ThicknessInspectionTool thickness = new ThicknessInspectionTool(
    new ThicknessInspectionOptions
    {
        MinimumThickness = 0.9,
        MaximumThickness = 1.4,
        MinimumValidSamples = 4,
        MinimumValidCoverageRatio = 1.0,
        InputRequirements = new HeightMapInputRequirements(
            "mm",
            "mm",
            "fixture")
    });

using CombinedInspectionRunResult result = new CombinedInspectionRunner().Run(
    new CombinedInspectionInput
    {
        Image = image,
        HeightMap = heightMap
    },
    new IVisionTool[] { threshold },
    new IThreeDInspectionTool[] { thickness });
using BlobTool blobAssemblyProbe = new BlobTool();

if (!result.Success || result.Steps.Count != 2)
{
    throw new InvalidOperationException(
        $"Package-only consumer failed: {result.Message}");
}

SurfaceMatchSample[] model =
{
    new SurfaceMatchSample(0, new ThreeDPoint(0, 0, 0)),
    new SurfaceMatchSample(1, new ThreeDPoint(2, 0, 0)),
    new SurfaceMatchSample(2, new ThreeDPoint(0, 1, 0)),
    new SurfaceMatchSample(3, new ThreeDPoint(0, 0, 1))
};
SurfaceMatchSample[] scene =
{
    new SurfaceMatchSample(0, new ThreeDPoint(10, -4, 2)),
    new SurfaceMatchSample(1, new ThreeDPoint(12, -4, 2)),
    new SurfaceMatchSample(2, new ThreeDPoint(10, -3, 2)),
    new SurfaceMatchSample(3, new ThreeDPoint(10, -4, 3))
};
DeterministicRigidSurfacePoseSearchResult match =
    new DeterministicRigidSurfacePoseSearchTool().Execute(
        model,
        scene,
        new DeterministicRigidSurfacePoseSearchOptions
        {
            MinimumRotationXDegrees = 0,
            MaximumRotationXDegrees = 0,
            RotationStepXDegrees = 1,
            MinimumRotationYDegrees = 0,
            MaximumRotationYDegrees = 0,
            RotationStepYDegrees = 1,
            MinimumRotationZDegrees = 0,
            MaximumRotationZDegrees = 0,
            RotationStepZDegrees = 1,
            MinimumTranslationX = 9,
            MaximumTranslationX = 11,
            MinimumTranslationY = -5,
            MaximumTranslationY = -3,
            MinimumTranslationZ = 1,
            MaximumTranslationZ = 3,
            MaximumCorrespondenceDistance = 1e-9,
            MinimumMatchedSampleCount = 4,
            MaximumCandidateCount = 1
        });

if (!match.Success || !match.Matched || match.Coverage.MatchedModelSampleCount != 4)
{
    throw new InvalidOperationException(
        $"Surface-match package example failed: {match.Message}{match.RejectionReason}");
}

NominalActualMeshComparisonResult comparison =
    new NominalActualMeshComparisonTool().Execute(
        new[]
        {
            new MeshTriangle(
                3,
                new ThreeDPoint(0, 0, 0),
                new ThreeDPoint(2, 0, 0),
                new ThreeDPoint(0, 2, 0))
        },
        new[]
        {
            new ThreeDPoint(0.5, 0.5, 1.0),
            new ThreeDPoint(0.5, 0.5, -2.0)
        },
        new NominalActualMeshComparisonOptions(2, -1.5, 1.5, 100));

if (!comparison.Success
    || comparison.WithinToleranceCount != 1
    || comparison.BelowToleranceCount != 1)
{
    throw new InvalidOperationException(
        $"Mesh-comparison package example failed: {comparison.Message}");
}

Console.WriteLine(
    "OpenVisionLab package-only 2D, 3D, surface-match, and mesh consumer passed.");
