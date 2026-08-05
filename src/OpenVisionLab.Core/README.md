# OpenVisionLab.Core

Shared runtime and geometry support for OpenVisionLab Vision SDK 3.0.

Most applications should install `OpenVisionLab.Vision2D`, `OpenVisionLab.Vision2D.Blob`, or `OpenVisionLab.Inspection` and receive this package transitively.

```powershell
dotnet add package OpenVisionLab.Core --version 3.0.0
```

The package contains:

- common bitmap/Mat conversion and 2D geometry utilities;
- managed OpenCvSharp assemblies used by the SDK;
- `runtimes/win-x64/native/OpenCvSharpExtern.dll`;
- a `buildTransitive` target that copies the native DLL to the consumer output.

`OpenCvSharpExtern.dll` currently makes Windows x64 the supported native runtime. Keep all OpenVisionLab packages on the same version.

[Repository and full documentation](https://github.com/Noah8218/OpenVisionLab-Vision-SDK)
