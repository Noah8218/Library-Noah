# AffineTransform 2D

`Lib.OpenCV.Tool.AffineTransformTool` provides an additive, deterministic
three-point 2D affine transform.

## Contract

- Teach three non-collinear source points and the three corresponding destination points.
- Calculate the matrix with OpenCV `GetAffineTransform`.
- Execute the image mapping with OpenCV `WarpAffine`.
- Configure output size, interpolation, border policy, and minimum valid-pixel ratio.
- Review the six matrix coefficients, determinant, scale, rotation, shear, translation,
  triangle-area, and valid-pixel metrics.
- Review the destination triangle and transformed source-frame overlays.
- Fail closed on invalid points, degenerate triangles, invalid output/sampling/gates,
  or insufficient source coverage.

Canonical factory name: `AffineTransform`.

Compatibility aliases: `Affine`, `AffineMatrix`.

## Version and compatibility

The package and file version is `2.8.0`. `Lib.Common` and `Lib.OpenCV` retain
assembly identity `2.1.0.0` because the new API is additive and existing
OpenVisionLab deployments load compatible `2.1` Noah dependencies.

Consumers should record the vendored DLL SHA-256 and file version. Do not replace
the full dependency set unless the consumer has verified that its legacy APIs are
still present.

## Verification

```powershell
dotnet build Lib.Common.sln -c Debug -p:Platform="Any CPU"
dotnet run --project Lib.Inspection.Smoke\Lib.Inspection.Smoke.csproj -c Debug --no-build
```

The smoke suite includes a known six-coefficient matrix, zero-gate collinear-source
rejection, and insufficient-coverage evidence retention.
