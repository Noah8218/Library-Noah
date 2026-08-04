using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lib.ThreeD.FeatureExtraction
{
    /// <summary>
    /// Finds a bounded collection of rigid surface instances. Each rotation,
    /// model anchor, and available scene anchor defines one translation
    /// candidate. The best candidate claims its scene samples before the next
    /// instance is searched, so returned instances never share evidence.
    /// </summary>
    public sealed class DeterministicMultipleSurfaceMatchTool
    {
        public const int AbsoluteMaximumMatchCount = 1000;
        public const int AbsoluteMaximumExpandedCandidateCount = 10000000;

        private readonly DeterministicSurfaceCoverageTool coverageTool =
            new DeterministicSurfaceCoverageTool();

        public DeterministicMultipleSurfaceMatchResult Execute(
            IReadOnlyList<SurfaceMatchSample> modelSamples,
            IReadOnlyList<SurfaceMatchSample> sceneSamples,
            DeterministicMultipleSurfaceMatchOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                SurfaceMatchingContractValidation.ValidateSamples(
                    modelSamples,
                    "Multiple surface match model input");
                SurfaceMatchingContractValidation.ValidateSamples(
                    sceneSamples,
                    "Multiple surface match scene input");
                ValidateOptions(options, modelSamples.Count, sceneSamples.Count);

                DeterministicRigidSurfacePoseSearchOptions poseOptions =
                    options.PoseSearchOptions;
                int rotationXCount =
                    DeterministicRigidSurfacePoseSearchTool.AxisCandidateCount(
                        poseOptions.MinimumRotationXDegrees,
                        poseOptions.MaximumRotationXDegrees,
                        poseOptions.RotationStepXDegrees,
                        "X");
                int rotationYCount =
                    DeterministicRigidSurfacePoseSearchTool.AxisCandidateCount(
                        poseOptions.MinimumRotationYDegrees,
                        poseOptions.MaximumRotationYDegrees,
                        poseOptions.RotationStepYDegrees,
                        "Y");
                int rotationZCount =
                    DeterministicRigidSurfacePoseSearchTool.AxisCandidateCount(
                        poseOptions.MinimumRotationZDegrees,
                        poseOptions.MaximumRotationZDegrees,
                        poseOptions.RotationStepZDegrees,
                        "Z");
                long rotationCandidateCount = checked(
                    (long)rotationXCount
                    * rotationYCount
                    * rotationZCount);
                if (rotationCandidateCount > poseOptions.MaximumCandidateCount)
                {
                    throw new ArgumentException(
                        "Multiple surface match rotation candidate count "
                        + rotationCandidateCount
                        + " exceeds the declared pose maximum "
                        + poseOptions.MaximumCandidateCount
                        + ".");
                }

                long expandedCandidateCount = checked(
                    rotationCandidateCount
                    * modelSamples.Count
                    * sceneSamples.Count
                    * options.MaximumMatchCount);
                if (expandedCandidateCount
                    > options.MaximumExpandedCandidateCount)
                {
                    throw new ArgumentException(
                        "Multiple surface match expanded candidate count "
                        + expandedCandidateCount
                        + " exceeds the declared maximum "
                        + options.MaximumExpandedCandidateCount
                        + ".");
                }

                double[] rotationX =
                    DeterministicRigidSurfacePoseSearchTool.AxisCandidates(
                        poseOptions.MinimumRotationXDegrees,
                        poseOptions.RotationStepXDegrees,
                        rotationXCount);
                double[] rotationY =
                    DeterministicRigidSurfacePoseSearchTool.AxisCandidates(
                        poseOptions.MinimumRotationYDegrees,
                        poseOptions.RotationStepYDegrees,
                        rotationYCount);
                double[] rotationZ =
                    DeterministicRigidSurfacePoseSearchTool.AxisCandidates(
                        poseOptions.MinimumRotationZDegrees,
                        poseOptions.RotationStepZDegrees,
                        rotationZCount);
                bool[] claimedSceneSamples = new bool[sceneSamples.Count];
                List<DeterministicSurfaceMatchInstance> matches =
                    new List<DeterministicSurfaceMatchInstance>();
                int totalEvaluatedCandidateCount = 0;
                string stopReason = string.Empty;
                while (matches.Count < options.MaximumMatchCount)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Candidate best = null;
                    int iterationEvaluatedCandidateCount = 0;
                    int enumerationOrder = 0;
                    for (int xIndex = 0; xIndex < rotationX.Length; xIndex++)
                    {
                        for (int yIndex = 0; yIndex < rotationY.Length; yIndex++)
                        {
                            for (int zIndex = 0; zIndex < rotationZ.Length; zIndex++)
                            {
                                DeterministicRigidSurfacePoseSearchTool.Rotation3 rotation =
                                    DeterministicRigidSurfacePoseSearchTool.Rotation(
                                        rotationX[xIndex],
                                        rotationY[yIndex],
                                        rotationZ[zIndex]);
                                for (int modelIndex = 0;
                                     modelIndex < modelSamples.Count;
                                     modelIndex++)
                                {
                                    ThreeDPoint rotatedModelAnchor =
                                        rotation.Transform(
                                            modelSamples[modelIndex].Position);
                                    for (int sceneIndex = 0;
                                         sceneIndex < sceneSamples.Count;
                                         sceneIndex++)
                                    {
                                        if (claimedSceneSamples[sceneIndex])
                                        {
                                            enumerationOrder++;
                                            continue;
                                        }

                                        cancellationToken.ThrowIfCancellationRequested();
                                        iterationEvaluatedCandidateCount++;
                                        ThreeDPoint sceneAnchor =
                                            sceneSamples[sceneIndex].Position;
                                        ThreeDPoint translation = new ThreeDPoint(
                                            sceneAnchor.X - rotatedModelAnchor.X,
                                            sceneAnchor.Y - rotatedModelAnchor.Y,
                                            sceneAnchor.Z - rotatedModelAnchor.Z);
                                        if (!DeterministicRigidSurfacePoseSearchTool
                                                .InsideTranslationBounds(
                                                    translation,
                                                    poseOptions))
                                        {
                                            enumerationOrder++;
                                            continue;
                                        }

                                        RigidSurfacePose pose = new RigidSurfacePose(
                                            rotation.M11,
                                            rotation.M12,
                                            rotation.M13,
                                            rotation.M21,
                                            rotation.M22,
                                            rotation.M23,
                                            rotation.M31,
                                            rotation.M32,
                                            rotation.M33,
                                            translation.X,
                                            translation.Y,
                                            translation.Z);
                                        DeterministicSurfaceCoverageResult coverage =
                                            Coverage(
                                                modelSamples,
                                                sceneSamples,
                                                claimedSceneSamples,
                                                pose,
                                                poseOptions.MaximumCorrespondenceDistance,
                                                cancellationToken);
                                        Candidate candidate = new Candidate(
                                            enumerationOrder,
                                            pose,
                                            coverage);
                                        if (candidate.IsBetterThan(best))
                                        {
                                            best = candidate;
                                        }

                                        enumerationOrder++;
                                    }
                                }
                            }
                        }
                    }

                    totalEvaluatedCandidateCount = checked(
                        totalEvaluatedCandidateCount
                        + iterationEvaluatedCandidateCount);
                    if (best == null
                        || best.Coverage.MatchedModelSampleCount
                            < poseOptions.MinimumMatchedSampleCount)
                    {
                        stopReason = best == null
                            ? "No available scene sample produced a translation inside the declared bounds."
                            : "Best remaining candidate matched "
                                + best.Coverage.MatchedModelSampleCount
                                + " model samples, below the required "
                                + poseOptions.MinimumMatchedSampleCount
                                + ".";
                        break;
                    }

                    foreach (SurfaceCoverageMatch match in best.Coverage.Matches)
                    {
                        claimedSceneSamples[match.SceneSampleOrder] = true;
                    }

                    matches.Add(new DeterministicSurfaceMatchInstance(
                        matches.Count,
                        iterationEvaluatedCandidateCount,
                        best.Pose,
                        best.Coverage));
                }

                return DeterministicMultipleSurfaceMatchResult.Completed(
                    totalEvaluatedCandidateCount,
                    matches,
                    stopReason);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicMultipleSurfaceMatchResult.Failed(
                    "Deterministic multiple surface match failed: "
                    + exception.Message);
            }
        }

        private DeterministicSurfaceCoverageResult Coverage(
            IReadOnlyList<SurfaceMatchSample> modelSamples,
            IReadOnlyList<SurfaceMatchSample> sceneSamples,
            bool[] claimedSceneSamples,
            RigidSurfacePose pose,
            double maximumCorrespondenceDistance,
            CancellationToken cancellationToken)
        {
            List<int> sourceOrders = new List<int>();
            List<SurfaceMatchSample> available =
                new List<SurfaceMatchSample>();
            for (int sceneIndex = 0;
                 sceneIndex < sceneSamples.Count;
                 sceneIndex++)
            {
                if (claimedSceneSamples[sceneIndex])
                {
                    continue;
                }

                sourceOrders.Add(sceneSamples[sceneIndex].Order);
                available.Add(new SurfaceMatchSample(
                    available.Count,
                    sceneSamples[sceneIndex].Position));
            }

            if (available.Count == 0)
            {
                return DeterministicSurfaceCoverageResult.Completed(
                    modelSamples.Count,
                    sceneSamples.Count,
                    0,
                    0.0,
                    false,
                    double.NaN,
                    maximumCorrespondenceDistance,
                    Array.Empty<SurfaceCoverageMatch>());
            }

            DeterministicSurfaceCoverageResult result = coverageTool.Execute(
                modelSamples,
                available,
                pose,
                maximumCorrespondenceDistance,
                cancellationToken);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message);
            }

            SurfaceCoverageMatch[] mapped = result.Matches
                .Select(match => new SurfaceCoverageMatch(
                    match.ModelSampleOrder,
                    sourceOrders[match.SceneSampleOrder],
                    match.Distance))
                .ToArray();
            return DeterministicSurfaceCoverageResult.Completed(
                modelSamples.Count,
                sceneSamples.Count,
                result.MatchedModelSampleCount,
                result.CoverageRatio,
                result.HasInlierRmse,
                result.InlierRmse,
                result.MaximumCorrespondenceDistance,
                mapped);
        }

        private static void ValidateOptions(
            DeterministicMultipleSurfaceMatchOptions options,
            int modelSampleCount,
            int sceneSampleCount)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.PoseSearchOptions == null)
            {
                throw new ArgumentException(
                    "Multiple surface match requires pose search options.");
            }

            if (options.MaximumMatchCount <= 0
                || options.MaximumMatchCount > AbsoluteMaximumMatchCount)
            {
                throw new ArgumentException(
                    "Maximum match count must be from 1 through "
                    + AbsoluteMaximumMatchCount
                    + ".");
            }

            if (options.MaximumExpandedCandidateCount <= 0
                || options.MaximumExpandedCandidateCount
                    > AbsoluteMaximumExpandedCandidateCount)
            {
                throw new ArgumentException(
                    "Maximum expanded candidate count must be from 1 through "
                    + AbsoluteMaximumExpandedCandidateCount
                    + ".");
            }

            if (options.PoseSearchOptions.MinimumMatchedSampleCount < 3
                || options.PoseSearchOptions.MinimumMatchedSampleCount
                    > modelSampleCount
                || options.PoseSearchOptions.MinimumMatchedSampleCount
                    > sceneSampleCount)
            {
                throw new ArgumentException(
                    "Multiple surface match minimum matched sample count must be at least three and cannot exceed available samples.");
            }
        }

        private sealed class Candidate
        {
            public Candidate(
                int enumerationOrder,
                RigidSurfacePose pose,
                DeterministicSurfaceCoverageResult coverage)
            {
                EnumerationOrder = enumerationOrder;
                Pose = pose;
                Coverage = coverage;
            }

            public int EnumerationOrder { get; }

            public RigidSurfacePose Pose { get; }

            public DeterministicSurfaceCoverageResult Coverage { get; }

            public bool IsBetterThan(Candidate current)
            {
                if (current == null)
                {
                    return true;
                }

                if (Coverage.MatchedModelSampleCount
                    != current.Coverage.MatchedModelSampleCount)
                {
                    return Coverage.MatchedModelSampleCount
                        > current.Coverage.MatchedModelSampleCount;
                }

                double candidateRmse = Coverage.HasInlierRmse
                    ? Coverage.InlierRmse
                    : double.PositiveInfinity;
                double currentRmse = current.Coverage.HasInlierRmse
                    ? current.Coverage.InlierRmse
                    : double.PositiveInfinity;
                if (candidateRmse != currentRmse)
                {
                    return candidateRmse < currentRmse;
                }

                return EnumerationOrder < current.EnumerationOrder;
            }
        }
    }
}
