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

The caller owns the input `Mat`. Dispose the Tool and `VisionToolResult`; the result owns its output image snapshot. Windows x64 is the supported native runtime.

[2D and 3D SDK documentation](https://github.com/Noah8218/OpenVisionLab-Vision-SDK#2d-quick-start)
