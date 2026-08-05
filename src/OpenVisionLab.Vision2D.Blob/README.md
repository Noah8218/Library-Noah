# OpenVisionLab.Vision2D.Blob

Blob labeling, area filtering, ROI handling, and ordered `BlobResult` output for OpenVisionLab Vision2D.

```powershell
dotnet add package OpenVisionLab.Vision2D.Blob --version 3.0.0
```

`BlobTool` follows the same execution contract as the other 2D tools:

```csharp
using OpenCvSharp;
using OpenVisionLab.Vision2D.Blob;
using OpenVisionLab.Vision2D.Tool;

using Mat source = Cv2.ImRead("part.png", ImreadModes.Grayscale);
using BlobTool tool = new BlobTool();
tool.SetProperty(yourBlobProperty); // implements IOpenCVPropertyBlob

using VisionToolResult result = tool.Execute(source);
if (!result.Success)
{
    throw new InvalidOperationException($"{result.ErrorName}: {result.Message}");
}

foreach (BlobResult blob in tool.results)
{
    Console.WriteLine($"#{blob.Index}: area={blob.Area}, center={blob.Center}");
}
```

The application supplies an `IOpenCVPropertyBlob` implementation so the same property model can be persisted or bound to a PropertyGrid. It must provide the inherited preprocessing/ROI fields plus `MIN_AREA` and `MAX_AREA`.

[Complete Blob property example](https://github.com/Noah8218/OpenVisionLab-Vision-SDK#blobtool)
