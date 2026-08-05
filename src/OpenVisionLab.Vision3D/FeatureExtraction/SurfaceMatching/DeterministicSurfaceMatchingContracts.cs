using System;
using System.Collections.Generic;

namespace OpenVisionLab.Vision3D.FeatureExtraction
{
    /// <summary>
    /// One source-neutral ordered point used by deterministic surface
    /// matching. Unit, frame, source identity, and persistence remain owned
    /// by the caller.
    /// </summary>
    public sealed class SurfaceMatchSample
    {
        public SurfaceMatchSample(int order, ThreeDPoint position)
        {
            Order = order;
            Position = position;
        }

        public int Order { get; }

        public ThreeDPoint Position { get; }
    }

    /// <summary>
    /// Row-major rigid transform. Points are transformed as target =
    /// rotation * source + translation.
    /// </summary>
    public sealed class RigidSurfacePose
    {
        public RigidSurfacePose(
            double m11,
            double m12,
            double m13,
            double m21,
            double m22,
            double m23,
            double m31,
            double m32,
            double m33,
            double translationX,
            double translationY,
            double translationZ)
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
            TranslationX = translationX;
            TranslationY = translationY;
            TranslationZ = translationZ;
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
        public double TranslationX { get; }
        public double TranslationY { get; }
        public double TranslationZ { get; }

        public ThreeDPoint Transform(ThreeDPoint point)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            return new ThreeDPoint(
                M11 * point.X + M12 * point.Y + M13 * point.Z + TranslationX,
                M21 * point.X + M22 * point.Y + M23 * point.Z + TranslationY,
                M31 * point.X + M32 * point.Y + M33 * point.Z + TranslationZ);
        }

        public bool IsRigid(double tolerance)
        {
            if (!SurfaceMatchingContractValidation.IsFinite(tolerance)
                || tolerance <= 0.0
                || !AllValuesFinite())
            {
                return false;
            }

            double firstLength = M11 * M11 + M12 * M12 + M13 * M13;
            double secondLength = M21 * M21 + M22 * M22 + M23 * M23;
            double thirdLength = M31 * M31 + M32 * M32 + M33 * M33;
            double firstSecond = M11 * M21 + M12 * M22 + M13 * M23;
            double firstThird = M11 * M31 + M12 * M32 + M13 * M33;
            double secondThird = M21 * M31 + M22 * M32 + M23 * M33;
            double determinant =
                M11 * (M22 * M33 - M23 * M32)
                - M12 * (M21 * M33 - M23 * M31)
                + M13 * (M21 * M32 - M22 * M31);
            return Math.Abs(firstLength - 1.0) <= tolerance
                && Math.Abs(secondLength - 1.0) <= tolerance
                && Math.Abs(thirdLength - 1.0) <= tolerance
                && Math.Abs(firstSecond) <= tolerance
                && Math.Abs(firstThird) <= tolerance
                && Math.Abs(secondThird) <= tolerance
                && Math.Abs(determinant - 1.0) <= tolerance;
        }

        private bool AllValuesFinite()
        {
            double[] values =
            {
                M11, M12, M13,
                M21, M22, M23,
                M31, M32, M33,
                TranslationX, TranslationY, TranslationZ
            };
            for (int index = 0; index < values.Length; index++)
            {
                if (!SurfaceMatchingContractValidation.IsFinite(values[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class SurfaceCoverageMatch
    {
        public SurfaceCoverageMatch(
            int modelSampleOrder,
            int sceneSampleOrder,
            double distance)
        {
            ModelSampleOrder = modelSampleOrder;
            SceneSampleOrder = sceneSampleOrder;
            Distance = distance;
        }

        public int ModelSampleOrder { get; }

        public int SceneSampleOrder { get; }

        public double Distance { get; }
    }

    /// <summary>
    /// Raw, decision-free one-way coverage evidence. Each scene sample can be
    /// claimed once and the denominator is always the model sample count.
    /// </summary>
    public sealed class DeterministicSurfaceCoverageResult
    {
        public const string Semantics =
            "one-way-model-sample-greedy-unique-nearest-position-v1";

        private DeterministicSurfaceCoverageResult(
            bool success,
            string message,
            int modelSampleCount,
            int sceneSampleCount,
            int matchedModelSampleCount,
            double coverageRatio,
            bool hasInlierRmse,
            double inlierRmse,
            double maximumCorrespondenceDistance,
            IReadOnlyList<SurfaceCoverageMatch> matches)
        {
            Success = success;
            Message = message ?? string.Empty;
            ModelSampleCount = modelSampleCount;
            SceneSampleCount = sceneSampleCount;
            MatchedModelSampleCount = matchedModelSampleCount;
            CoverageRatio = coverageRatio;
            HasInlierRmse = hasInlierRmse;
            InlierRmse = inlierRmse;
            MaximumCorrespondenceDistance = maximumCorrespondenceDistance;
            Matches = matches ?? Array.Empty<SurfaceCoverageMatch>();
        }

        public bool Success { get; }
        public string Message { get; }
        public int ModelSampleCount { get; }
        public int SceneSampleCount { get; }
        public int MatchedModelSampleCount { get; }
        public int UnmatchedModelSampleCount =>
            ModelSampleCount - MatchedModelSampleCount;
        public double CoverageRatio { get; }
        public bool HasInlierRmse { get; }
        public double InlierRmse { get; }
        public double MaximumCorrespondenceDistance { get; }
        public IReadOnlyList<SurfaceCoverageMatch> Matches { get; }

        internal static DeterministicSurfaceCoverageResult Completed(
            int modelSampleCount,
            int sceneSampleCount,
            int matchedModelSampleCount,
            double coverageRatio,
            bool hasInlierRmse,
            double inlierRmse,
            double maximumCorrespondenceDistance,
            IReadOnlyList<SurfaceCoverageMatch> matches)
        {
            return new DeterministicSurfaceCoverageResult(
                true,
                string.Empty,
                modelSampleCount,
                sceneSampleCount,
                matchedModelSampleCount,
                coverageRatio,
                hasInlierRmse,
                inlierRmse,
                maximumCorrespondenceDistance,
                matches);
        }

        internal static DeterministicSurfaceCoverageResult Failed(
            string message)
        {
            return new DeterministicSurfaceCoverageResult(
                false,
                message,
                0,
                0,
                0,
                0.0,
                false,
                double.NaN,
                double.NaN,
                Array.Empty<SurfaceCoverageMatch>());
        }
    }

    /// <summary>
    /// Explicit Euler rotation grid, translation bounds, correspondence
    /// limit, match requirement, and candidate budget for one pose search.
    /// </summary>
    public sealed class DeterministicRigidSurfacePoseSearchOptions
    {
        public double MinimumRotationXDegrees { get; set; }
        public double MaximumRotationXDegrees { get; set; }
        public double RotationStepXDegrees { get; set; }
        public double MinimumRotationYDegrees { get; set; }
        public double MaximumRotationYDegrees { get; set; }
        public double RotationStepYDegrees { get; set; }
        public double MinimumRotationZDegrees { get; set; }
        public double MaximumRotationZDegrees { get; set; }
        public double RotationStepZDegrees { get; set; }
        public double MinimumTranslationX { get; set; }
        public double MaximumTranslationX { get; set; }
        public double MinimumTranslationY { get; set; }
        public double MaximumTranslationY { get; set; }
        public double MinimumTranslationZ { get; set; }
        public double MaximumTranslationZ { get; set; }
        public double MaximumCorrespondenceDistance { get; set; }
        public int MinimumMatchedSampleCount { get; set; }
        public int MaximumCandidateCount { get; set; }
    }

    /// <summary>
    /// Pose-search execution state plus the selected pose and raw coverage
    /// evidence. Matched is independent of application acceptance policy.
    /// </summary>
    public sealed class DeterministicRigidSurfacePoseSearchResult
    {
        private DeterministicRigidSurfacePoseSearchResult(
            bool success,
            string message,
            bool matched,
            int evaluatedCandidateCount,
            RigidSurfacePose pose,
            DeterministicSurfaceCoverageResult coverage,
            string rejectionReason)
        {
            Success = success;
            Message = message ?? string.Empty;
            Matched = matched;
            EvaluatedCandidateCount = evaluatedCandidateCount;
            Pose = pose;
            Coverage = coverage;
            RejectionReason = rejectionReason ?? string.Empty;
        }

        public bool Success { get; }
        public string Message { get; }
        public bool Matched { get; }
        public int EvaluatedCandidateCount { get; }
        public RigidSurfacePose Pose { get; }
        public DeterministicSurfaceCoverageResult Coverage { get; }
        public string RejectionReason { get; }

        internal static DeterministicRigidSurfacePoseSearchResult Completed(
            bool matched,
            int evaluatedCandidateCount,
            RigidSurfacePose pose,
            DeterministicSurfaceCoverageResult coverage,
            string rejectionReason)
        {
            return new DeterministicRigidSurfacePoseSearchResult(
                true,
                string.Empty,
                matched,
                evaluatedCandidateCount,
                pose,
                coverage,
                rejectionReason);
        }

        internal static DeterministicRigidSurfacePoseSearchResult Failed(
            string message)
        {
            return new DeterministicRigidSurfacePoseSearchResult(
                false,
                message,
                false,
                0,
                null,
                null,
                string.Empty);
        }
    }

    internal static class SurfaceMatchingContractValidation
    {
        public static void ValidateSamples(
            IReadOnlyList<SurfaceMatchSample> samples,
            string label)
        {
            if (samples == null || samples.Count == 0)
            {
                throw new ArgumentException(
                    label + " requires at least one ordered sample.");
            }

            for (int index = 0; index < samples.Count; index++)
            {
                SurfaceMatchSample sample = samples[index];
                if (sample == null
                    || sample.Order != index
                    || sample.Position == null
                    || !sample.Position.IsFinite)
                {
                    throw new ArgumentException(
                        label + " requires contiguous zero-based orders and finite XYZ positions.");
                }
            }
        }

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
