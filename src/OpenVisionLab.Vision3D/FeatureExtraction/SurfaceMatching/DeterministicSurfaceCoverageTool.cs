using System;
using System.Collections.Generic;
using System.Threading;

namespace OpenVisionLab.Vision3D.FeatureExtraction
{
    /// <summary>
    /// Computes deterministic one-way model-to-scene coverage. Model samples
    /// are visited in stable order and claim the nearest unclaimed scene
    /// sample within the inclusive distance limit.
    /// </summary>
    public sealed class DeterministicSurfaceCoverageTool
    {
        public DeterministicSurfaceCoverageResult Execute(
            IReadOnlyList<SurfaceMatchSample> modelSamples,
            IReadOnlyList<SurfaceMatchSample> sceneSamples,
            RigidSurfacePose pose,
            double maximumCorrespondenceDistance,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                SurfaceMatchingContractValidation.ValidateSamples(
                    modelSamples,
                    "Surface coverage model input");
                SurfaceMatchingContractValidation.ValidateSamples(
                    sceneSamples,
                    "Surface coverage scene input");
                if (pose == null || !pose.IsRigid(1e-9))
                {
                    throw new ArgumentException(
                        "Surface coverage requires a finite rigid pose.");
                }

                if (!SurfaceMatchingContractValidation.IsFinite(
                        maximumCorrespondenceDistance)
                    || maximumCorrespondenceDistance <= 0.0)
                {
                    throw new ArgumentException(
                        "Surface coverage distance must be finite and positive.");
                }

                bool[] claimedSceneSamples = new bool[sceneSamples.Count];
                List<SurfaceCoverageMatch> matches =
                    new List<SurfaceCoverageMatch>(
                        Math.Min(modelSamples.Count, sceneSamples.Count));
                double rmseScale = 0.0;
                double rmseScaledSquareSum = 0.0;
                for (int modelIndex = 0;
                     modelIndex < modelSamples.Count;
                     modelIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SurfaceMatchSample modelSample = modelSamples[modelIndex];
                    ThreeDPoint transformed = pose.Transform(modelSample.Position);
                    int bestSceneOrder = -1;
                    double bestDistance = double.PositiveInfinity;
                    for (int sceneIndex = 0;
                         sceneIndex < sceneSamples.Count;
                         sceneIndex++)
                    {
                        SurfaceMatchSample sceneSample = sceneSamples[sceneIndex];
                        if (claimedSceneSamples[sceneSample.Order])
                        {
                            continue;
                        }

                        double distance = Distance(
                            transformed,
                            sceneSample.Position);
                        if (distance < bestDistance
                            || distance == bestDistance
                            && sceneSample.Order < bestSceneOrder)
                        {
                            bestDistance = distance;
                            bestSceneOrder = sceneSample.Order;
                        }
                    }

                    if (bestSceneOrder < 0
                        || bestDistance > maximumCorrespondenceDistance)
                    {
                        continue;
                    }

                    claimedSceneSamples[bestSceneOrder] = true;
                    AccumulateScaledSquare(
                        bestDistance,
                        ref rmseScale,
                        ref rmseScaledSquareSum);
                    matches.Add(
                        new SurfaceCoverageMatch(
                            modelSample.Order,
                            bestSceneOrder,
                            bestDistance));
                }

                int matchedCount = matches.Count;
                double coverageRatio =
                    matchedCount / (double)modelSamples.Count;
                bool hasInlierRmse = matchedCount > 0;
                double inlierRmse = !hasInlierRmse
                    ? double.NaN
                    : rmseScale == 0.0
                        ? 0.0
                        : rmseScale * Math.Min(
                            1.0,
                            Math.Sqrt(
                                rmseScaledSquareSum / matchedCount));
                return DeterministicSurfaceCoverageResult.Completed(
                    modelSamples.Count,
                    sceneSamples.Count,
                    matchedCount,
                    coverageRatio,
                    hasInlierRmse,
                    inlierRmse,
                    maximumCorrespondenceDistance,
                    matches);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicSurfaceCoverageResult.Failed(
                    "Deterministic surface coverage failed: "
                    + exception.Message);
            }
        }

        private static double Distance(
            ThreeDPoint first,
            ThreeDPoint second)
        {
            double x = Math.Abs(first.X - second.X);
            double y = Math.Abs(first.Y - second.Y);
            double z = Math.Abs(first.Z - second.Z);
            double scale = Math.Max(x, Math.Max(y, z));
            if (!SurfaceMatchingContractValidation.IsFinite(scale))
            {
                return double.PositiveInfinity;
            }

            if (scale == 0.0)
            {
                return 0.0;
            }

            x /= scale;
            y /= scale;
            z /= scale;
            return scale * Math.Sqrt(x * x + y * y + z * z);
        }

        private static void AccumulateScaledSquare(
            double value,
            ref double scale,
            ref double scaledSquareSum)
        {
            if (value == 0.0)
            {
                return;
            }

            if (scale < value)
            {
                double ratio = scale / value;
                scaledSquareSum = 1.0
                    + scaledSquareSum * ratio * ratio;
                scale = value;
            }
            else
            {
                double ratio = value / scale;
                scaledSquareSum += ratio * ratio;
            }
        }
    }
}
