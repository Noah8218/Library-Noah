# Library-Noah 3D inspection

## Purpose

`Lib.ThreeD` adds pure, UI-free height-map inspection algorithms to Library-Noah.
`Lib.Inspection` runs existing 2D `IVisionTool` instances and new
`IThreeDInspectionTool` instances in one ordered run while preserving every result.

The 3D libraries target `netstandard2.0`. They do not reference WPF, SharpGL, a
viewer control, or the OpenVisionLab 3D Studio application. A host can render the
same result separately without making rendering part of the measurement algorithm.

## Source-neutral feature extraction

`Lib.ThreeD.FeatureExtraction` contains pure full-XYZ geometry tools that do
not know a camera, C3D file, recipe, UI, or calibration claim.

`DualSurfaceThicknessInspectionTool` and `HeightDeviationInspectionTool`
own deterministic height-residual/statistical evaluation and typed decisions.
Source identity, units, frames, recipe lifecycle, and UI evidence remain with
the consuming application.

- `TwoPointLineTool` constructs an ordered finite full-XYZ segment from two
  explicit points. It does not pick, snap, fit, or measure.
- `FullXyzAffineSolveTool` solves one source-to-reference affine matrix from
  exactly four independent correspondence pairs using scaled partial pivoting.
  It returns matrix, determinant, condition, and residual evidence only; it
  does not move a point cloud or create a height map.
- `LineIntersectionTool` evaluates the closest approach, acute angle, and
  finite-segment support of two normalized full-XYZ line geometries. It does
  not choose lines, attach source/frame identity, or claim a physical corner.
- `DeterministicSurfaceCoverageTool` visits ordered model samples and assigns
  each one to the nearest still-unclaimed scene sample inside an inclusive
  distance limit. It reports one-way matched count, ratio, RMSE, and exact
  correspondences without applying a product acceptance threshold.
- `DeterministicRigidSurfacePoseSearchTool` enumerates caller-bounded X/Y/Z
  Euler candidates, derives translation from the two sample centroids, and
  ranks candidates by coverage count, RMSE, then stable enumeration order.
  Candidate and translation bounds are explicit and fail closed.
- `TriangleMeshDistanceTool` builds a deterministic triangle BVH and returns
  closest-point, closest-feature, unsigned-distance, and explicit direct or
  robust signed-distance evidence for source-neutral XYZ queries.
- `NominalActualMeshComparisonTool` streams ordered query points through that
  mesh-distance kernel and returns deterministic tolerance counts, signed and
  unsigned population statistics, sign-recovery counts, and bounded display
  samples. It does not own file identity, units, frames, or product lifecycle.
- `RigidTransformDiagnosticsTool` measures the homogeneous-row error,
  rotation orthogonality, determinant, translation magnitude, and rotation
  angle of a row-major 4x4 transform. The caller retains scenario limits and
  acceptance order.
- `HeightGridSummaryTool` computes finite/missing/zero counts, minimum,
  maximum, mean, and a deterministic fixed-bin distribution from
  single-precision height samples under an explicit zero-is-missing policy.
- `HeightDistributionStatisticsTool` computes the corresponding finite-value
  statistics and bins for double-precision scalar sequences, including an
  optional expected-valid-count guard.
- `HeightMapRegionStatisticsTool` owns deterministic finite count, coverage,
  sum, mean, and extrema for an explicit row-major rectangular region.
- `CompletenessGridInspectionTool` owns reference-region mean, rectangular
  cell placement, finite coverage, reference-relative mean, and typed
  per-cell/aggregate decisions under an optional inclusive policy.
- `ReferenceGridPointReconstructionTool` maps finite grid cells to both
  declared-frame XYZ and reference-axis U/H/V coordinates under an explicit
  supported-coordinate range.
- `DeclaredMeshNormalQualityTool` evaluates declared per-position normals for
  finite/non-zero/unit length, topology validity, degenerate triangles, and
  corner alignment. It does not generate, repair, or promote normals and does
  not own source identity or admission policy.
- `LandmarkCorrespondenceValidationTool` evaluates the augmented rank and
  span-normalized tetrahedral volume of exactly four source/reference points.
  Pair identities, lineage, units, frames, recipe lifecycle, affine solving,
  and acceptance remain with the consuming application.
- `RepeatabilityStatisticsTool` calculates finite scalar mean, extrema, sample
  standard deviation, six-sigma spread, and range. Study identity, units,
  acceptance limits, Gauge R&R claims, and product decisions remain with the
  consuming application.

A host owns source binding, metadata, persistence, identity hashing, and any
display or inspection lifecycle around these pure results.

The surface-matching tools receive only contiguous ordered finite XYZ samples,
the source-neutral search domain, and a correspondence distance. They do not
receive a mesh, C3D file, prepared-scene artifact, unit, coordinate frame,
recipe, acceptance policy, Viewer state, or published-result lifecycle. Their
controlled synthetic smoke covers a known `30 degree` yaw with translation,
one-sample occlusion, translation no-match, and candidate-budget rejection.

## Height-map contract

`HeightMap3D` is an immutable regular grid with:

- rows and columns;
- origin and positive row/column pitch;
- one scalar value per grid cell;
- declared `Unit`, `FrameId`, and `SourceId` metadata.

`double.NaN` means an unavailable sample and is ignored by the 3D tools.
Infinity, invalid grid geometry, invalid ROI values, and insufficient usable samples
produce controlled result statuses rather than a successful measurement.

The unit and frame are declarations from the caller. They are not calibration,
traceability, Gauge R and R, repeatability, or physical-accuracy evidence.

## Thickness

`ThicknessInspectionTool` evaluates finite scalar values in its configured ROI.

- `MinimumThickness` and `MaximumThickness` are inclusive limits.
- Metrics include valid sample count, minimum, maximum, mean, range, and the count
  below or above each limit.
- An out-of-limit result has `HasMeasurement=true` and
  `ResultStatus=Failed`; invalid input is reported separately.

The caller must provide a map whose scalar values actually represent thickness. The
tool does not infer reference planes, material surfaces, or sensor calibration.

## Warpage

`WarpageInspectionTool` fits the least-squares plane `z = ax + by + c` to finite
ROI samples in the declared map frame. It then evaluates:

- residual peak-to-valley: `max(residual) - min(residual)`;
- residual RMS;
- fitted plane slope and intercept.

The configured peak-to-valley limit is required. RMS is optional. A collinear ROI,
fewer than three valid points, or a numerically unstable plane fit returns a
controlled non-measurement status.

This is a numerical planarity/warpage calculation. It is not a substitute for a
specified fixture, alignment scheme, mechanical warpage standard, or calibrated
metrology workflow.

## Combined 2D and 3D execution

`CombinedInspectionRunner` deliberately does not modify the existing Mat/layer
`VisionPipeline`. It runs the two domains independently, preserves the native result
type for each step, and continues after individual failures so an operator can see all
available 2D and 3D evidence from one acquisition.

```csharp
var run = new CombinedInspectionRunner().Run(
    new CombinedInspectionInput
    {
        Image = image,
        HeightMap = heightMap
    },
    new IVisionTool[] { twoDTool },
    new IThreeDInspectionTool[]
    {
        new ThicknessInspectionTool(new ThicknessInspectionOptions
        {
            MinimumThickness = 1.00,
            MaximumThickness = 1.20
        }),
        new WarpageInspectionTool(new WarpageInspectionOptions
        {
            MaximumPeakToValley = 0.05,
            MaximumRms = 0.02
        })
    });
```

The caller owns `Image` and `HeightMap`; the combined runner never disposes them.

## Verification commands

```powershell
dotnet build Lib.Common.sln -c Debug
dotnet run --project Lib.Inspection.Smoke/Lib.Inspection.Smoke.csproj -c Debug --no-build
dotnet pack Lib.Common.sln -c Debug --no-build
```

The smoke executable uses only deterministic synthetic height maps. It verifies
analytic-plane fitting, tolerance failures that retain measurements, controlled input
errors, and that a 3D step still runs after a 2D step fails. It is not a substitute for
sensor data, calibrated artifacts, or production acceptance testing.
