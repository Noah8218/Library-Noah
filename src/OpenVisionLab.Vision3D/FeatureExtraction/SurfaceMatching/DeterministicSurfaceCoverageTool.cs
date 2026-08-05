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
                double maximumDistanceSquared =
                    maximumCorrespondenceDistance
                    * maximumCorrespondenceDistance;
                double squaredErrorSum = 0.0;
                for (int modelIndex = 0;
                     modelIndex < modelSamples.Count;
                     modelIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SurfaceMatchSample modelSample = modelSamples[modelIndex];
                    ThreeDPoint transformed = pose.Transform(modelSample.Position);
                    int bestSceneOrder = -1;
                    double bestDistanceSquared = double.PositiveInfinity;
                    for (int sceneIndex = 0;
                         sceneIndex < sceneSamples.Count;
                         sceneIndex++)
                    {
                        SurfaceMatchSample sceneSample = sceneSamples[sceneIndex];
                        if (claimedSceneSamples[sceneSample.Order])
                        {
                            continue;
                        }

                        double distanceSquared = DistanceSquared(
                            transformed,
                            sceneSample.Position);
                        if (distanceSquared < bestDistanceSquared
                            || distanceSquared == bestDistanceSquared
                            && sceneSample.Order < bestSceneOrder)
                        {
                            bestDistanceSquared = distanceSquared;
                            bestSceneOrder = sceneSample.Order;
                        }
                    }

                    if (bestSceneOrder < 0
                        || bestDistanceSquared > maximumDistanceSquared)
                    {
                        continue;
                    }

                    claimedSceneSamples[bestSceneOrder] = true;
                    squaredErrorSum += bestDistanceSquared;
                    matches.Add(
                        new SurfaceCoverageMatch(
                            modelSample.Order,
                            bestSceneOrder,
                            Math.Sqrt(bestDistanceSquared)));
                }

                int matchedCount = matches.Count;
                double coverageRatio =
                    matchedCount / (double)modelSamples.Count;
                bool hasInlierRmse = matchedCount > 0;
                double inlierRmse = hasInlierRmse
                    ? Math.Sqrt(squaredErrorSum / matchedCount)
                    : double.NaN;
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

        private static double DistanceSquared(
            ThreeDPoint first,
            ThreeDPoint second)
        {
            double x = first.X - second.X;
            double y = first.Y - second.Y;
            double z = first.Z - second.Z;
            return x * x + y * y + z * z;
        }
    }
}
