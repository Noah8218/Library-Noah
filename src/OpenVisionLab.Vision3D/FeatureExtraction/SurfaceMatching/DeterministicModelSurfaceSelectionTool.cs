using System;
using System.Collections.Generic;
using System.Threading;

namespace OpenVisionLab.Vision3D.FeatureExtraction
{
    public enum ModelSurfaceRemovalReason
    {
        ExplicitInternal,
        ExplicitUnobservable,
        ExactDuplicate
    }

    public sealed class RemovedModelSurface
    {
        public RemovedModelSurface(
            int sourceTriangleIndex,
            ModelSurfaceRemovalReason reason,
            int? duplicateOfSourceTriangleIndex)
        {
            SourceTriangleIndex = sourceTriangleIndex;
            Reason = reason;
            DuplicateOfSourceTriangleIndex = duplicateOfSourceTriangleIndex;
        }

        public int SourceTriangleIndex { get; }

        public ModelSurfaceRemovalReason Reason { get; }

        public int? DuplicateOfSourceTriangleIndex { get; }
    }

    public sealed class DeterministicModelSurfaceSelectionOptions
    {
        public IReadOnlyList<int> ExplicitInternalSourceTriangleIndices
        {
            get;
            set;
        } = Array.Empty<int>();

        public IReadOnlyList<int> ExplicitUnobservableSourceTriangleIndices
        {
            get;
            set;
        } = Array.Empty<int>();

        public bool RemoveExactDuplicateTriangles { get; set; }
    }

    public sealed class DeterministicModelSurfaceSelectionResult
    {
        private DeterministicModelSurfaceSelectionResult(
            bool success,
            string message,
            IReadOnlyList<int> explicitInternalSourceTriangleIndices,
            IReadOnlyList<int> explicitUnobservableSourceTriangleIndices,
            IReadOnlyList<int> retainedSourceTriangleIndices,
            IReadOnlyList<RemovedModelSurface> removedSurfaces)
        {
            Success = success;
            Message = message ?? string.Empty;
            ExplicitInternalSourceTriangleIndices =
                explicitInternalSourceTriangleIndices ?? Array.Empty<int>();
            ExplicitUnobservableSourceTriangleIndices =
                explicitUnobservableSourceTriangleIndices
                ?? Array.Empty<int>();
            RetainedSourceTriangleIndices =
                retainedSourceTriangleIndices ?? Array.Empty<int>();
            RemovedSurfaces =
                removedSurfaces ?? Array.Empty<RemovedModelSurface>();
        }

        public bool Success { get; }

        public string Message { get; }

        public IReadOnlyList<int> ExplicitInternalSourceTriangleIndices
        {
            get;
        }

        public IReadOnlyList<int> ExplicitUnobservableSourceTriangleIndices
        {
            get;
        }

        public IReadOnlyList<int> RetainedSourceTriangleIndices { get; }

        public IReadOnlyList<RemovedModelSurface> RemovedSurfaces { get; }

        internal static DeterministicModelSurfaceSelectionResult Completed(
            IReadOnlyList<int> explicitInternalSourceTriangleIndices,
            IReadOnlyList<int> explicitUnobservableSourceTriangleIndices,
            IReadOnlyList<int> retainedSourceTriangleIndices,
            IReadOnlyList<RemovedModelSurface> removedSurfaces)
        {
            return new DeterministicModelSurfaceSelectionResult(
                true,
                string.Empty,
                explicitInternalSourceTriangleIndices,
                explicitUnobservableSourceTriangleIndices,
                retainedSourceTriangleIndices,
                removedSurfaces);
        }

        internal static DeterministicModelSurfaceSelectionResult Failed(
            string message)
        {
            return new DeterministicModelSurfaceSelectionResult(
                false,
                message,
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<RemovedModelSurface>());
        }
    }

    /// <summary>
    /// Selects the active model-surface domain. Explicit source-triangle
    /// exclusions are authoritative; optional exact-coordinate duplicate
    /// removal retains the lowest non-excluded source-triangle index.
    /// Viewpoint-dependent visibility remains outside this Tool.
    /// </summary>
    public sealed class DeterministicModelSurfaceSelectionTool
    {
        public const string Semantics =
            "exact-duplicate-and-explicit-source-triangle-exclusion-v1";

        public DeterministicModelSurfaceSelectionResult Execute(
            IReadOnlyList<ThreeDPoint> points,
            IReadOnlyList<SurfaceModelTriangleInput> triangles,
            DeterministicModelSurfaceSelectionOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                Validate(points, triangles, options);
                HashSet<int> explicitInternal = IndexSet(
                    options.ExplicitInternalSourceTriangleIndices,
                    triangles.Count,
                    "internal");
                HashSet<int> explicitUnobservable = IndexSet(
                    options.ExplicitUnobservableSourceTriangleIndices,
                    triangles.Count,
                    "unobservable");
                foreach (int index in explicitInternal)
                {
                    if (explicitUnobservable.Contains(index))
                    {
                        throw new ArgumentException(
                            "A source triangle cannot be both explicitly internal and explicitly unobservable.");
                    }
                }

                var retained = new List<int>(triangles.Count);
                var removed = new List<RemovedModelSurface>();
                var firstRetainedByGeometry =
                    new Dictionary<TriangleKey, int>();
                for (int triangleIndex = 0;
                     triangleIndex < triangles.Count;
                     triangleIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (explicitInternal.Contains(triangleIndex))
                    {
                        removed.Add(new RemovedModelSurface(
                            triangleIndex,
                            ModelSurfaceRemovalReason.ExplicitInternal,
                            null));
                        continue;
                    }

                    if (explicitUnobservable.Contains(triangleIndex))
                    {
                        removed.Add(new RemovedModelSurface(
                            triangleIndex,
                            ModelSurfaceRemovalReason.ExplicitUnobservable,
                            null));
                        continue;
                    }

                    if (options.RemoveExactDuplicateTriangles)
                    {
                        TriangleKey key = TriangleKey.Create(
                            points,
                            triangles[triangleIndex]);
                        int firstIndex;
                        if (firstRetainedByGeometry.TryGetValue(
                                key,
                                out firstIndex))
                        {
                            removed.Add(new RemovedModelSurface(
                                triangleIndex,
                                ModelSurfaceRemovalReason.ExactDuplicate,
                                firstIndex));
                            continue;
                        }

                        firstRetainedByGeometry.Add(key, triangleIndex);
                    }

                    retained.Add(triangleIndex);
                }

                if (retained.Count == 0)
                {
                    throw new ArgumentException(
                        "Model-surface selection must retain at least one source triangle.");
                }

                return DeterministicModelSurfaceSelectionResult.Completed(
                    Sorted(explicitInternal),
                    Sorted(explicitUnobservable),
                    retained.ToArray(),
                    removed.ToArray());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DeterministicModelSurfaceSelectionResult.Failed(
                    "Deterministic model-surface selection failed: "
                    + exception.Message);
            }
        }

        private static void Validate(
            IReadOnlyList<ThreeDPoint> points,
            IReadOnlyList<SurfaceModelTriangleInput> triangles,
            DeterministicModelSurfaceSelectionOptions options)
        {
            if (points == null || points.Count == 0)
            {
                throw new ArgumentException(
                    "Model-surface selection requires finite source points.");
            }

            if (triangles == null || triangles.Count == 0)
            {
                throw new ArgumentException(
                    "Model-surface selection requires source triangles.");
            }

            if (options == null
                || options.ExplicitInternalSourceTriangleIndices == null
                || options.ExplicitUnobservableSourceTriangleIndices == null)
            {
                throw new ArgumentException(
                    "Model-surface selection requires explicit options and exclusion lists.");
            }

            for (int pointIndex = 0;
                 pointIndex < points.Count;
                 pointIndex++)
            {
                if (points[pointIndex] == null
                    || !points[pointIndex].IsFinite)
                {
                    throw new ArgumentException(
                        "Model-surface selection requires finite source points.");
                }
            }

            for (int triangleIndex = 0;
                 triangleIndex < triangles.Count;
                 triangleIndex++)
            {
                SurfaceModelTriangleInput triangle = triangles[triangleIndex];
                if (triangle == null
                    || triangle.FirstPointIndex < 0
                    || triangle.SecondPointIndex < 0
                    || triangle.ThirdPointIndex < 0
                    || triangle.FirstPointIndex >= points.Count
                    || triangle.SecondPointIndex >= points.Count
                    || triangle.ThirdPointIndex >= points.Count
                    || triangle.FirstPointIndex == triangle.SecondPointIndex
                    || triangle.FirstPointIndex == triangle.ThirdPointIndex
                    || triangle.SecondPointIndex == triangle.ThirdPointIndex)
                {
                    throw new ArgumentException(
                        "Model-surface triangles must reference three distinct existing points.");
                }
            }
        }

        private static HashSet<int> IndexSet(
            IReadOnlyList<int> indices,
            int triangleCount,
            string label)
        {
            var result = new HashSet<int>();
            for (int itemIndex = 0;
                 itemIndex < indices.Count;
                 itemIndex++)
            {
                int sourceTriangleIndex = indices[itemIndex];
                if (sourceTriangleIndex < 0
                    || sourceTriangleIndex >= triangleCount)
                {
                    throw new ArgumentException(
                        "Explicit " + label
                        + " source-triangle indices must exist.");
                }

                if (!result.Add(sourceTriangleIndex))
                {
                    throw new ArgumentException(
                        "Explicit " + label
                        + " source-triangle indices must be unique.");
                }
            }

            return result;
        }

        private static int[] Sorted(HashSet<int> indices)
        {
            int[] result = new int[indices.Count];
            indices.CopyTo(result);
            Array.Sort(result);
            return result;
        }

        private struct PointKey : IComparable<PointKey>, IEquatable<PointKey>
        {
            public PointKey(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }

            public int CompareTo(PointKey other)
            {
                int comparison = X.CompareTo(other.X);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = Y.CompareTo(other.Y);
                return comparison != 0
                    ? comparison
                    : Z.CompareTo(other.Z);
            }

            public bool Equals(PointKey other)
            {
                return X.Equals(other.X)
                    && Y.Equals(other.Y)
                    && Z.Equals(other.Z);
            }

            public override bool Equals(object obj)
            {
                return obj is PointKey && Equals((PointKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = X.GetHashCode();
                    hash = hash * 397 ^ Y.GetHashCode();
                    return hash * 397 ^ Z.GetHashCode();
                }
            }
        }

        private struct TriangleKey : IEquatable<TriangleKey>
        {
            private TriangleKey(
                PointKey first,
                PointKey second,
                PointKey third)
            {
                First = first;
                Second = second;
                Third = third;
            }

            public PointKey First { get; }
            public PointKey Second { get; }
            public PointKey Third { get; }

            public static TriangleKey Create(
                IReadOnlyList<ThreeDPoint> points,
                SurfaceModelTriangleInput triangle)
            {
                var keys = new[]
                {
                    Key(points[triangle.FirstPointIndex]),
                    Key(points[triangle.SecondPointIndex]),
                    Key(points[triangle.ThirdPointIndex])
                };
                Array.Sort(keys);
                return new TriangleKey(keys[0], keys[1], keys[2]);
            }

            public bool Equals(TriangleKey other)
            {
                return First.Equals(other.First)
                    && Second.Equals(other.Second)
                    && Third.Equals(other.Third);
            }

            public override bool Equals(object obj)
            {
                return obj is TriangleKey && Equals((TriangleKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = First.GetHashCode();
                    hash = hash * 397 ^ Second.GetHashCode();
                    return hash * 397 ^ Third.GetHashCode();
                }
            }

            private static PointKey Key(ThreeDPoint point)
            {
                return new PointKey(point.X, point.Y, point.Z);
            }
        }
    }
}
