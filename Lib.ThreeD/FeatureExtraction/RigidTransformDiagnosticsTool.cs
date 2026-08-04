using System;
using System.Collections.Generic;

namespace Lib.ThreeD.FeatureExtraction
{
    public sealed class RigidTransformDiagnosticsResult
    {
        private RigidTransformDiagnosticsResult(
            bool success,
            string message,
            double homogeneousRowMaximumError,
            double rotationOrthogonalityMaximumError,
            double rotationDeterminant,
            double rotationDeterminantUnitError,
            double translationMagnitude,
            double rotationAngleDegrees)
        {
            Success = success;
            Message = message;
            HomogeneousRowMaximumError = homogeneousRowMaximumError;
            RotationOrthogonalityMaximumError =
                rotationOrthogonalityMaximumError;
            RotationDeterminant = rotationDeterminant;
            RotationDeterminantUnitError = rotationDeterminantUnitError;
            TranslationMagnitude = translationMagnitude;
            RotationAngleDegrees = rotationAngleDegrees;
        }

        public bool Success { get; }

        public string Message { get; }

        public double HomogeneousRowMaximumError { get; }

        public double RotationOrthogonalityMaximumError { get; }

        public double RotationDeterminant { get; }

        public double RotationDeterminantUnitError { get; }

        public double TranslationMagnitude { get; }

        public double RotationAngleDegrees { get; }

        internal static RigidTransformDiagnosticsResult Completed(
            double homogeneousRowMaximumError,
            double rotationOrthogonalityMaximumError,
            double rotationDeterminant,
            double rotationDeterminantUnitError,
            double translationMagnitude,
            double rotationAngleDegrees)
        {
            return new RigidTransformDiagnosticsResult(
                true,
                "Rigid-transform diagnostics completed.",
                homogeneousRowMaximumError,
                rotationOrthogonalityMaximumError,
                rotationDeterminant,
                rotationDeterminantUnitError,
                translationMagnitude,
                rotationAngleDegrees);
        }

        internal static RigidTransformDiagnosticsResult Failed(string message)
        {
            return new RigidTransformDiagnosticsResult(
                false,
                message,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN);
        }
    }

    /// <summary>
    /// Measures deterministic plausibility diagnostics for a row-major 4x4
    /// transform. It owns no units, scenario limits, product acceptance, or
    /// evaluation ordering.
    /// </summary>
    public sealed class RigidTransformDiagnosticsTool
    {
        public RigidTransformDiagnosticsResult Execute(
            IReadOnlyList<double> values)
        {
            if (values == null || values.Count != 16)
            {
                return RigidTransformDiagnosticsResult.Failed(
                    "Registration transform must contain 16 finite row-major float64 values.");
            }

            for (int index = 0; index < values.Count; index++)
            {
                if (!IsFinite(values[index]))
                {
                    return RigidTransformDiagnosticsResult.Failed(
                        "Registration transform must contain 16 finite row-major float64 values.");
                }
            }

            double homogeneousRowMaximumError = Maximum(
                Math.Abs(values[12]),
                Math.Abs(values[13]),
                Math.Abs(values[14]),
                Math.Abs(values[15] - 1.0));
            Vector3d[] rows =
            {
                new Vector3d(values[0], values[1], values[2]),
                new Vector3d(values[4], values[5], values[6]),
                new Vector3d(values[8], values[9], values[10])
            };
            double orthogonalityMaximumError = 0.0;
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    double expected = row == column ? 1.0 : 0.0;
                    orthogonalityMaximumError = Math.Max(
                        orthogonalityMaximumError,
                        Math.Abs(rows[row].Dot(rows[column]) - expected));
                }
            }

            double determinant =
                values[0]
                    * (values[5] * values[10]
                        - values[6] * values[9])
                - values[1]
                    * (values[4] * values[10]
                        - values[6] * values[8])
                + values[2]
                    * (values[4] * values[9]
                        - values[5] * values[8]);
            double determinantUnitError = Math.Abs(determinant - 1.0);
            double translationMagnitude = Math.Sqrt(
                values[3] * values[3]
                + values[7] * values[7]
                + values[11] * values[11]);
            double cosine =
                (values[0] + values[5] + values[10] - 1.0) / 2.0;
            cosine = Math.Max(-1.0, Math.Min(1.0, cosine));
            double rotationAngleDegrees =
                Math.Acos(cosine) * 180.0 / Math.PI;
            if (!IsFinite(homogeneousRowMaximumError)
                || !IsFinite(orthogonalityMaximumError)
                || !IsFinite(determinant)
                || !IsFinite(determinantUnitError)
                || !IsFinite(translationMagnitude)
                || !IsFinite(rotationAngleDegrees))
            {
                return RigidTransformDiagnosticsResult.Failed(
                    "Registration transform produced non-finite plausibility metrics.");
            }

            return RigidTransformDiagnosticsResult.Completed(
                homogeneousRowMaximumError,
                orthogonalityMaximumError,
                determinant,
                determinantUnitError,
                translationMagnitude,
                rotationAngleDegrees);
        }

        private static double Maximum(
            double first,
            double second,
            double third,
            double fourth)
        {
            return Math.Max(
                Math.Max(first, second),
                Math.Max(third, fourth));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private struct Vector3d
        {
            public Vector3d(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }

            public double Dot(Vector3d other)
            {
                return X * other.X + Y * other.Y + Z * other.Z;
            }
        }
    }
}
