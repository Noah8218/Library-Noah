# Library-Noah 3D inspection

## Purpose

`Lib.ThreeD` adds pure, UI-free height-map inspection and full-XYZ algorithms to Library-Noah.
`Lib.Inspection` runs existing 2D `IVisionTool` instances and new
`IThreeDInspectionTool` instances in one ordered run while preserving every result.

The 3D libraries target `netstandard2.0`. They do not reference WPF, SharpGL, a
viewer control, or the OpenVisionLab 3D Studio application. A host can render the
same result separately without making rendering part of the measurement algorithm.

## Companion validation applications

- [OpenVisionLab](https://github.com/Noah8218/OpenVisionLab) is the 2D rule-based
  workbench that exercises the Library-Noah image Tool, Layer, Pipeline, and result
  display contracts.
- [OpenVisionLab 3D Studio](https://github.com/Noah8218/OpenVisionLab-3D-Studio)
  exercises 3D source review, ROI teaching, explicit Preview/Run, metrics, overlays,
  and recipe replay. It consumes a fixed `Lib.ThreeD` NuGet package through an
  explicit adapter rather than an adjacent source checkout.

A Library-Noah source change is not automatically present in either application.
The consuming application must intentionally update its binary or pinned package,
and 3D Studio must update the package hash and adapter contract together.

## Source-neutral feature extraction

`Lib.ThreeD.FeatureExtraction` contains pure full-XYZ geometry tools that do
not know a camera, C3D file, recipe, UI, or calibration claim.

- `TwoPointLineTool` constructs an ordered finite full-XYZ segment from two
  explicit points. It does not pick, snap, fit, or measure.
- `FullXyzAffineSolveTool` solves one source-to-reference affine matrix from
  exactly four independent correspondence pairs using scaled partial pivoting.
  It returns matrix, determinant, condition, and residual evidence only; it
  does not move a point cloud or create a height map.
- `LineIntersectionTool` evaluates the closest approach, acute angle, and
  finite-segment support of two normalized full-XYZ line geometries. It does
  not choose lines, attach source/frame identity, or claim a physical corner.

A host owns source binding, metadata, persistence, identity hashing, and any
display or inspection lifecycle around these pure results.

## Height-map contract

`HeightMap3D` is an immutable regular grid with:

- rows and columns;
- origin and positive row/column pitch;
- one scalar value per grid cell;
- declared `PlanarUnit`, `HeightUnit`, `FrameId`, and `SourceId` metadata.

Its coordinate convention is fixed:

```text
X = OriginX + Column * ColumnPitch
Y = OriginY + Row * RowPitch
H = Values[Row * Columns + Column]
```

Columns increase X, rows increase Y, and the stored scalar is height H. H is not
implicitly Cartesian Y or Z. The final X/Y coordinate extent must remain finite.

The legacy constructor's single `Unit` declares both `PlanarUnit` and `HeightUnit`.
The legacy `Unit` property remains a scalar-height alias. New integrations should use
the separate units so a planar pitch in millimetres cannot be confused with height
samples in micrometres.

`double.NaN` means an unavailable sample and is ignored by height-map inspection tools.
Infinity and non-finite coordinate extents are rejected when `HeightMap3D` is
constructed. Invalid ROI values, insufficient usable samples, and insufficient valid
coverage produce controlled non-measurement result statuses.

Units and frame are declarations from the caller. They are not calibration,
traceability, Gauge R and R, repeatability, or physical-accuracy evidence.

## Strict input requirements

`ThicknessInspectionOptions`, `WarpageInspectionOptions`, and
`DatumPlaneRawHeightDeviationInspectionOptions` accept:

- `InputRequirements`: exact planar unit, height unit, and frame ID;
- `MinimumValidSamples`: absolute finite-sample gate;
- `MinimumValidCoverageRatio`: finite samples divided by all ROI cells.

When `InputRequirements` is present, comparison uses exact ordinal strings. The
library does not convert units, accept aliases, infer a frame, or apply a coordinate
transform. A mismatch returns `InputContractMismatch`, `InvalidInput`, and
`HasMeasurement=false` before the numerical algorithm runs. A null requirement keeps
the 2.x compatibility path; production recipes should declare one explicitly.

```csharp
HeightMap3D map = new HeightMap3D(
    rows: 2,
    columns: 3,
    originX: 0.0,
    originY: 0.0,
    columnPitch: 0.1,
    rowPitch: 0.1,
    values: new[] { 1.00, 1.05, 1.10, 1.15, double.NaN, 1.20 },
    planarUnit: "mm",
    heightUnit: "mm",
    frameId: "fixture-top",
    sourceId: "scan-001");

ThreeDInspectionResult result = new ThicknessInspectionTool(
    new ThicknessInspectionOptions
    {
        MinimumThickness = 0.95,
        MaximumThickness = 1.25,
        MinimumValidSamples = 5,
        MinimumValidCoverageRatio = 0.8,
        InputRequirements = new HeightMapInputRequirements("mm", "mm", "fixture-top")
    }).Execute(map);
```

No height-map inspection interpolates or fills NaN cells. Results after ROI sampling
expose typed properties and matching metrics for `TotalSampleCount`,
`ValidSampleCount`, `MissingSampleCount`, `ValidCoverageRatio`,
`MinimumValidSamples`, and `MinimumValidCoverageRatio`.

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

Planar and height units may differ for this height-field fit. Residuals and intercept
use `HeightUnit`; slopes use `HeightUnit/PlanarUnit`.

## Datum-plane raw-height deviation

`DatumPlaneRawHeightDeviationInspectionTool` evaluates the explicit equation
`n.x * X + n.y * H + n.z * Y + d = 0`. It does not fit the plane. Because this
equation normalizes X, Y, and H as one Euclidean coordinate without unit conversion,
the tool rejects a map whose planar and height unit strings differ.

## Result units and status

`ThreeDInspectionResult` preserves `PlanarUnit`, `HeightUnit`, legacy `Unit`,
`FrameId`, `SourceId`, and the fixed coordinate convention. `MetricUnits` identifies
each metric independently; counts use `count`, ratios use `ratio`, residuals use the
height unit, and plane slopes use height unit divided by planar unit.

- `Success=true`, `HasMeasurement=true`: measurement completed within tolerance.
- `Success=false`, `HasMeasurement=true`: measurement completed outside tolerance.
- `HasMeasurement=false`: input, parameter, ROI, coverage, geometry, configuration,
  or execution failure prevented a valid measurement.

## Combined 2D and 3D execution

`CombinedInspectionRunner` deliberately does not modify the existing Mat/layer
`VisionPipeline`. It runs the two domains independently, preserves the native result
type for each step, and continues after individual failures so an operator can see all
available 2D and 3D evidence from one acquisition.

```csharp
using CombinedInspectionRunResult run = new CombinedInspectionRunner().Run(
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

The caller owns `Image`, `HeightMap`, and the supplied tools; the combined runner
never disposes them. `CombinedInspectionRunResult` owns any 2D
`VisionToolResult.ResultImage` snapshots it contains, so dispose the run result after
all result inspection and rendering is complete.

## Verification commands

```powershell
dotnet build Lib.Common.sln -c Debug
dotnet run --project Lib.Inspection.Smoke/Lib.Inspection.Smoke.csproj -c Debug --no-build
dotnet pack Lib.Common.sln -c Debug --no-build
```

The smoke executable uses only deterministic synthetic height maps. It verifies
legacy constructor compatibility, strict unit/frame rejection, missing-sample coverage,
analytic-plane fitting, tolerance failures that retain measurements, controlled input
errors, and that a 3D step still runs after a 2D step fails. It is not a substitute for
sensor data, calibrated artifacts, or production acceptance testing.
