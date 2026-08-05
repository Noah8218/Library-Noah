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

namespace OpenVisionLab.Inspection.Smoke
{
    internal static class SmokeFixtures
    {
        internal static ReferenceGridProfile CreateReferenceGridProfile(int rows, int columns, double minimumCoverage)
        {
            return new ReferenceGridProfile(
                "frame.fixture-reference", "fixture-unit", "fixture reference", "R1",
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0,
                1.0, 1.0, rows, columns, minimumCoverage);
        }

        internal static IReadOnlyList<FullXyzAffineCorrespondence> CreateAffinePairs()
        {
            return new[]
            {
                new FullXyzAffineCorrespondence(new ThreeDPoint(0.0, 0.0, 0.0), new ThreeDPoint(10.0, 20.0, 30.0)),
                new FullXyzAffineCorrespondence(new ThreeDPoint(1.0, 0.0, 0.0), new ThreeDPoint(12.0, 19.0, 30.2)),
                new FullXyzAffineCorrespondence(new ThreeDPoint(0.0, 1.0, 0.0), new ThreeDPoint(10.5, 23.0, 29.7)),
                new FullXyzAffineCorrespondence(new ThreeDPoint(0.0, 0.0, 1.0), new ThreeDPoint(9.75, 20.75, 34.0))
            };
        }

        internal static HeightMap3D CreateThicknessMap()
        {
            return new HeightMap3D(
                2,
                3,
                0.0,
                0.0,
                1.0,
                1.0,
                new[] { 1.0, 1.1, 1.2, 1.3, double.NaN, 1.4 },
                "mm",
                "sensor-top",
                "sample-thickness");
        }

        internal static HeightMap3D CreatePlaneMap(int rows, int columns, double slopeX, double slopeY, double intercept)
        {
            double[] values = new double[rows * columns];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    values[(row * columns) + column] = (slopeX * column) + (slopeY * row) + intercept;
                }
            }

            return new HeightMap3D(rows, columns, 0.0, 0.0, 1.0, 1.0, values, "mm", "fixture", "analytic-plane");
        }
    }
}
