using System;

namespace Lib.ThreeD.FeatureExtraction
{
    /// <summary>
    /// Immutable source-neutral full-XYZ coordinate for pure 3D feature tools.
    /// Unit and frame ownership remain with the caller.
    /// </summary>
    public sealed class ThreeDPoint
    {
        public ThreeDPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }

        internal bool IsFinite => !double.IsNaN(X) && !double.IsInfinity(X)
            && !double.IsNaN(Y) && !double.IsInfinity(Y)
            && !double.IsNaN(Z) && !double.IsInfinity(Z);
    }
}
