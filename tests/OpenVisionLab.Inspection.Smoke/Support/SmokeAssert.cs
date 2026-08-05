using System;

namespace OpenVisionLab.Inspection.Smoke
{
    internal static class SmokeAssert
    {
        internal static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        internal static void RequireApproximately(double actual, double expected, double tolerance, string message)
        {
            if (Math.Abs(actual - expected) > tolerance)
            {
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
            }
        }
    }
}
