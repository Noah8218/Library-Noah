using System;
using System.Collections.Generic;
using System.Threading;

namespace OpenVisionLab.Vision3D.FeatureExtraction
{
    /// <summary>
    /// Deterministic bounded Euler-grid pose search. It derives translation
    /// from model and scene centroids and ranks candidates by matched model
    /// count, RMSE, then enumeration order. It has no product acceptance or
    /// source identity policy.
    /// </summary>
    public sealed class DeterministicRigidSurfacePoseSearchTool
    {
        public const int AbsoluteMaximumCandidateCount = 1000000;

        private readonly DeterministicSurfaceCoverageTool coverageTool =
            new DeterministicSurfaceCoverageTool();

        public DeterministicRigidSurfacePoseSearchResult Execute(
            IReadOnlyList<SurfaceMatchSample> modelSamples,
            IReadOnlyList<SurfaceMatchSample> sceneSamples,
            DeterministicRigidSurfacePoseSearchOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                SurfaceMatchingContractValidation.ValidateSamples(
                    modelSamples,
                    "Rigid surface pose model input");
                SurfaceMatchingContractValidation.ValidateSamples(
                    sceneSamples,
                    "Rigid surface pose scene input");
                ValidateOptions(options, modelSamples.Count, sceneSamples.Count);

                int rotationXCount = AxisCandidateCount(
                    options.MinimumRotationXDegrees,
                    options.MaximumRotationXDegrees,
                    options.RotationStepXDegrees,
                    "X");
                int rotationYCount = AxisCandidateCount(
                    options.MinimumRotationYDegrees,
                    options.MaximumRotationYDegrees,
                    options.RotationStepYDegrees,
                    "Y");
                int rotationZCount = AxisCandidateCount(
                    options.MinimumRotationZDegrees,
                    options.MaximumRotationZDegrees,
                    options.RotationStepZDegrees,
                    "Z");
                long candidateCount = checked(
                    (long)rotationXCount
                    * rotationYCount
                    * rotationZCount);
                if (candidateCount > options.MaximumCandidateCount)
                {
                    throw new ArgumentException(
                        "Rigid pose search candidate count "
                        + candidateCount
                        + " exceeds the declared maximum "
                        + options.MaximumCandidateCount
                        + ".");
                }

                double[] rotationX = AxisCandidates(
                    options.MinimumRotationXDegrees,
                    options.RotationStepXDegrees,
                    rotationXCount);
                double[] rotationY = AxisCandidates(
                    options.MinimumRotationYDegrees,
                    options.RotationStepYDegrees,
                    rotationYCount);
                double[] rotationZ = AxisCandidates(
                    options.MinimumRotationZDegrees,
                    options.RotationStepZDegrees,
                    rotationZCount);
                ThreeDPoint modelCentroid = Centroid(modelSamples);
                ThreeDPoint sceneCentroid = Centroid(sceneSamples);
                Candidate best = null;
                int evaluatedCandidateCount = 0;
                int enumerationOrder = 0;
                for (int xIndex = 0; xIndex < rotationX.Length; xIndex++)
                {
                    for (int yIndex = 0; yIndex < rotationY.Length; yIndex++)
                    {
                        for (int zIndex = 0; zIndex < rotationZ.Length; zIndex++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            evaluatedCandidateCount++;
                            Rotation3 rotation = Rotation(
                                rotationX[xIndex],
                                rotationY[yIndex],
                                rotationZ[zIndex]);
                            ThreeDPoint rotatedCentroid =
                                rotation.Transform(modelCentroid);
                            ThreeDPoint translation = new ThreeDPoint(
                                sceneCentroid.X - rotatedCentroid.X,
                                sceneCentroid.Y - rotatedCentroid.Y,
                                sceneCentroid.Z - rotatedCentroid.Z);
                            if (!InsideTranslationBounds(translation, options))
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
                                coverageTool.Execute(
                                    modelSamples,
                                    sceneSamples,
                                    pose,
                                    options.MaximumCorrespondenceDistance,
                                    cancellationToken);
                            if (!coverage.Success)
                            {
                                throw new InvalidOperationException(
                                    coverage.Message);
                            }

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

                if (best == null
                    || best.Coverage.MatchedModelSampleCount
                        < options.MinimumMatchedSampleCount)
                {
                    DeterministicSurfaceCoverageResult emptyCoverage =
                        best == null
                        ? DeterministicSurfaceCoverageResult.Completed(
                            modelSamples.Count,
                            sceneSamples.Count,
                            0,
                            0.0,
                            false,
                            double.NaN,
                            options.MaximumCorrespondenceDistance,
                            Array.Empty<SurfaceCoverageMatch>())
                        : best.Coverage;
                    string reason = best == null
                        ? "No rotation candidate produced a translation inside the declared bounds."
                        : "Best candidate matched "
                            + best.Coverage.MatchedModelSampleCount
                            + " model samples, below the required "
                            + options.MinimumMatchedSampleCount
                            + ".";
                    return DeterministicRigidSurfacePoseSearchResult.Completed(
                        false,
                        evaluatedCandidateCount,
                        null,
                        emptyCoverage,
                        reason);
                }

                return DeterministicRigidSurfacePoseSearchResult.Completed(
                    true,
                    evaluatedCandidateCount,
                    best.Pose,
                    best.Coverage,
                    string.Empty);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicRigidSurfacePoseSearchResult.Failed(
                    "Deterministic rigid surface pose search failed: "
                    + exception.Message);
            }
        }

        internal static void ValidateOptions(
            DeterministicRigidSurfacePoseSearchOptions options,
            int modelSampleCount,
            int sceneSampleCount)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            double[] translation =
            {
                options.MinimumTranslationX,
                options.MaximumTranslationX,
                options.MinimumTranslationY,
                options.MaximumTranslationY,
                options.MinimumTranslationZ,
                options.MaximumTranslationZ
            };
            for (int index = 0; index < translation.Length; index++)
            {
                if (!SurfaceMatchingContractValidation.IsFinite(
                        translation[index]))
                {
                    throw new ArgumentException(
                        "Rigid pose translation bounds must be finite and ordered.");
                }
            }

            if (options.MinimumTranslationX > options.MaximumTranslationX
                || options.MinimumTranslationY > options.MaximumTranslationY
                || options.MinimumTranslationZ > options.MaximumTranslationZ)
            {
                throw new ArgumentException(
                    "Rigid pose translation bounds must be finite and ordered.");
            }

            if (!SurfaceMatchingContractValidation.IsFinite(
                    options.MaximumCorrespondenceDistance)
                || options.MaximumCorrespondenceDistance <= 0.0)
            {
                throw new ArgumentException(
                    "Maximum correspondence distance must be finite and positive.");
            }

            if (options.MinimumMatchedSampleCount < 3
                || options.MinimumMatchedSampleCount > modelSampleCount
                || options.MinimumMatchedSampleCount > sceneSampleCount)
            {
                throw new ArgumentException(
                    "Rigid pose search minimum matched sample count must be at least three and cannot exceed available samples.");
            }

            if (options.MaximumCandidateCount <= 0
                || options.MaximumCandidateCount
                    > AbsoluteMaximumCandidateCount)
            {
                throw new ArgumentException(
                    "Maximum candidate count must be from 1 through "
                    + AbsoluteMaximumCandidateCount
                    + ".");
            }
        }

        internal static int AxisCandidateCount(
            double minimum,
            double maximum,
            double step,
            string axis)
        {
            if (!SurfaceMatchingContractValidation.IsFinite(minimum)
                || !SurfaceMatchingContractValidation.IsFinite(maximum)
                || !SurfaceMatchingContractValidation.IsFinite(step)
                || minimum > maximum
                || step <= 0.0
                || minimum < -180.0
                || maximum > 180.0)
            {
                throw new ArgumentException(
                    "Rigid pose "
                    + axis
                    + "-rotation range requires finite ordered bounds in [-180,180] and a positive step.");
            }

            double candidateCount = Math.Floor(
                (maximum - minimum) / step + 1e-12) + 1.0;
            if (!SurfaceMatchingContractValidation.IsFinite(candidateCount)
                || candidateCount < 1.0)
            {
                throw new ArgumentException(
                    "Rigid pose " + axis + "-rotation range has no candidate.");
            }

            if (candidateCount > AbsoluteMaximumCandidateCount)
            {
                throw new ArgumentException(
                    "Rigid pose "
                    + axis
                    + "-rotation candidate count exceeds the supported limit "
                    + AbsoluteMaximumCandidateCount
                    + ".");
            }

            return checked((int)candidateCount);
        }

        internal static double[] AxisCandidates(
            double minimum,
            double step,
            int count)
        {
            double[] values = new double[count];
            for (int index = 0; index < count; index++)
            {
                values[index] = minimum + index * step;
            }

            return values;
        }

        private static ThreeDPoint Centroid(
            IReadOnlyList<SurfaceMatchSample> samples)
        {
            double x = 0.0;
            double y = 0.0;
            double z = 0.0;
            for (int index = 0; index < samples.Count; index++)
            {
                x += samples[index].Position.X;
                y += samples[index].Position.Y;
                z += samples[index].Position.Z;
            }

            return new ThreeDPoint(
                x / samples.Count,
                y / samples.Count,
                z / samples.Count);
        }

        internal static Rotation3 Rotation(
            double xDegrees,
            double yDegrees,
            double zDegrees)
        {
            double x = xDegrees * Math.PI / 180.0;
            double y = yDegrees * Math.PI / 180.0;
            double z = zDegrees * Math.PI / 180.0;
            double cx = Math.Cos(x);
            double sx = Math.Sin(x);
            double cy = Math.Cos(y);
            double sy = Math.Sin(y);
            double cz = Math.Cos(z);
            double sz = Math.Sin(z);
            return new Rotation3(
                cz * cy,
                cz * sy * sx - sz * cx,
                cz * sy * cx + sz * sx,
                sz * cy,
                sz * sy * sx + cz * cx,
                sz * sy * cx - cz * sx,
                -sy,
                cy * sx,
                cy * cx);
        }

        internal static bool InsideTranslationBounds(
            ThreeDPoint translation,
            DeterministicRigidSurfacePoseSearchOptions options)
        {
            return translation.X >= options.MinimumTranslationX
                && translation.X <= options.MaximumTranslationX
                && translation.Y >= options.MinimumTranslationY
                && translation.Y <= options.MaximumTranslationY
                && translation.Z >= options.MinimumTranslationZ
                && translation.Z <= options.MaximumTranslationZ;
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

        internal sealed class Rotation3
        {
            public Rotation3(
                double m11,
                double m12,
                double m13,
                double m21,
                double m22,
                double m23,
                double m31,
                double m32,
                double m33)
            {
                M11 = m11;
                M12 = m12;
                M13 = m13;
                M21 = m21;
                M22 = m22;
                M23 = m23;
                M31 = m31;
                M32 = m32;
                M33 = m33;
            }

            public double M11 { get; }
            public double M12 { get; }
            public double M13 { get; }
            public double M21 { get; }
            public double M22 { get; }
            public double M23 { get; }
            public double M31 { get; }
            public double M32 { get; }
            public double M33 { get; }

            public ThreeDPoint Transform(ThreeDPoint point)
            {
                return new ThreeDPoint(
                    M11 * point.X + M12 * point.Y + M13 * point.Z,
                    M21 * point.X + M22 * point.Y + M23 * point.Z,
                    M31 * point.X + M32 * point.Y + M33 * point.Z);
            }
        }
    }
}
