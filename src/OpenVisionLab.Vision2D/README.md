# OpenVisionLab.Vision2D

OpenCvSharp-based 2D inspection tools with explicit properties and disposable `VisionToolResult` output.

```powershell
dotnet add package OpenVisionLab.Vision2D --version 3.0.0
```

## Quick start

```csharp
using OpenCvSharp;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;

using Mat source = new Mat(16, 16, MatType.CV_8UC1, new Scalar(100));
using ThresholdTool tool = new ThresholdTool();
tool.SetProperty(new ThresholdToolProperty
{
    Threshold = 50,
    MaxValue = 255,
    ThresholdType = ThresholdTypes.Binary
});

using VisionToolResult result = tool.Execute(source);
if (!result.Success)
{
    throw new InvalidOperationException($"{result.ErrorName}: {result.Message}");
}

Console.WriteLine($"Output: {result.ResultImage.Width}x{result.ResultImage.Height}");
```

## Ready-to-use property models

The package provides concrete configuration models for every current non-legacy 2D Tool. Implement the interfaces directly only when an application needs its own persistence model. `BlobToolProperty` is supplied by the separate `OpenVisionLab.Vision2D.Blob` package.

| Tool | Property model |
| --- | --- |
| `ThresholdTool` | `ThresholdToolProperty` |
| `MorphologyTool` | `MorphologyToolProperty` |
| `FilterTool` | `FilterToolProperty` |
| `EdgeDetectionTool` | `EdgeDetectionToolProperty` |
| `RotateScaleTool` | `RotateScaleToolProperty` |
| `AffineTransformTool` | `AffineTransformToolProperty` |
| `AutoMPointTool` | `AutoMPointToolProperty` |
| `ContourTool`, `CornerTool` | `ContourToolProperty` |
| `MatchingTool` | `MatchingToolProperty` |
| `EdgeBasedTemplateMatchingTool` | `EdgeBasedTemplateMatchingToolProperty` |
| `SiftTool` | `SiftToolProperty` |
| `MeanTool` | `MeanToolProperty` |
| `LineGaugeTool` | `LineGaugeToolProperty` |

These models inherit the common whole-image, no-preprocessing defaults from `OpenCvToolPropertyBase`. `LineGaugeTool` always requires a taught `CvROI` because its scan direction and extent are part of the measurement definition.

The caller owns the input `Mat`. Dispose the Tool and `VisionToolResult`; the result owns its output image snapshot. Windows x64 is the supported native runtime.

[2D and 3D SDK documentation](https://github.com/Noah8218/OpenVisionLab-Vision-SDK#2d-quick-start)
