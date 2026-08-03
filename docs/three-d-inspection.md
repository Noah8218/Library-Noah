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
analytic-plane fitting, tolerance failures that retain measurements, controlled input
errors, and that a 3D step still runs after a 2D step fails. It is not a substitute for
sensor data, calibrated artifacts, or production acceptance testing.
