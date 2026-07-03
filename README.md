# Library-Noah

OpenCvSharp 기반의 C# 비전 검사 라이브러리입니다.

Threshold, Filter, Morphology, Edge, Contour, Matching, Line Gauge, Mean, Blob 등 검사 도구를 공통 실행 구조로 묶고, 결과 이미지/검출 결과/에러 코드/메트릭을 애플리케이션에서 사용하기 쉽게 제공합니다.

## 개발 환경

- Visual Studio 2022 또는 .NET SDK
- C# / .NET Standard 2.0
- Windows 런타임 권장
- OpenCvSharp 관련 DLL은 `Lib.Common/DLL`에 포함되어 있습니다.

빌드:

```powershell
dotnet restore Lib.Common.sln
dotnet build Lib.Common.sln -c Release
```

## 프로젝트 구조

```text
Library-Noah
|- Lib.Common
|  |- Bitmap
|  |- Converter
|  |- Line
|  |- DLL
|  `- build
|- Lib.OpenCV
|  `- OpenCV
|     |- Pipeline
|     |- Property
|     |- Result
|     `- Tool
`- Lib.OpenCV.Blob
```

| 프로젝트 | 역할 |
| --- | --- |
| `Lib.Common` | 공통 유틸리티, Bitmap/Mat 변환, 좌표/ROI 변환, 라인 계산, 디렉터리/COM 포트 보조 기능 |
| `Lib.OpenCV` | 주요 OpenCV 검사 도구, 속성 인터페이스, 결과 모델, 파이프라인 실행 구조 |
| `Lib.OpenCV.Blob` | Blob 라벨링/면적 필터링 도구 |

참조 관계:

```text
Lib.Common
`- Lib.OpenCV
   `- Lib.OpenCV.Blob
```

## 코드 구조

### Lib.Common

- `Converter`: `Bitmap`, `Mat`, `Point`, `Rect`, `Rectangle` 변환 유틸리티
- `Bitmap`: `BitmapHelper`, `BitmapProcessing` 등 Bitmap 직접 처리 기능
- `Line`: 직선 피팅, 수직선 계산, 교차점 계산용 모델과 계산기
- `CFormula`, `FormulaUtil`: 각도, 교차점, 원근 변환, 폴리곤 판정 등 수식 유틸리티
- `AppUtil`, `CUtil`: 디렉터리 초기화, 폴더 동기화, 드라이브/COM 포트 조회 등 애플리케이션 보조 기능

### Lib.OpenCV

- `OpenCV/Tool`: 실제 검사 도구 구현
- `OpenCV/Property`: 각 도구가 사용하는 설정 인터페이스와 일부 기본 속성 클래스
- `OpenCV/Result`: Matching, Contour, Mean, LineGauge 등 도구별 결과 모델
- `OpenCV/Pipeline`: 여러 도구를 순차 실행하는 파이프라인 모델과 런타임
- `OpenCvHelper`: Mat 유효성 검사와 채널 변환 유틸리티

### Lib.OpenCV.Blob

- `BlobTool`: 새 실행 구조를 사용하는 Blob 도구
- `BlobResult`: Blob 결과 모델
- `CVBlob`, `CResultBlob`: 기존 코드 호환을 위한 레거시 API

## Tool 실행 구조

새 도구들은 대부분 `OpenCvAlgorithmBase`를 상속합니다.

```text
IVisionTool
`- OpenCvAlgorithmBase
   |- ThresholdTool
   |- MorphologyTool
   |- FilterTool
   |- EdgeDetectionTool
   |- RotateScaleTool
   |- ContourTool
   |- MatchingTool
   |- LineGaugeTool
   |- MeanTool
   `- BlobTool
```

기본 실행 흐름:

1. Tool 객체를 생성합니다.
2. Tool에 Property를 설정합니다.
3. `Execute(Mat source)`를 호출합니다.
4. `VisionToolResult`에서 성공 여부, 결과 이미지, 에러 코드, 메트릭, 오버레이를 확인합니다.

```csharp
VisionToolResult result = tool.Execute(source);

if (result.Success)
{
    Mat output = result.ResultImage;
}
else
{
    string error = $"{result.ErrorName}: {result.Message}";
}
```

`Execute`는 입력 이미지 검증, 파라미터 검증, 예외 처리, 결과 이미지 복사, 메트릭 수집을 공통으로 처리합니다. 기존 코드와 호환되는 `CV*` 계열 클래스는 `Run()` 호출 후 `results` 또는 `resultList`를 직접 읽는 구조입니다.

## 지원 Tool 요약

| Tool | 주요 용도 | Property |
| --- | --- | --- |
| `ThresholdTool` | 이진화, 범위 이진화, Adaptive Threshold | `ThresholdToolProperty` |
| `MorphologyTool` | Erode, Dilate, Open, Close 등 형태학 연산 | `MorphologyToolProperty` |
| `FilterTool` | Blur, Gaussian, Median, Bilateral 등 필터 | `FilterToolProperty` |
| `EdgeDetectionTool` | Canny, Sobel, Scharr, Laplacian 엣지 검출 | `EdgeDetectionToolProperty` |
| `RotateScaleTool` | 이미지 회전/스케일 변환 | `RotateScaleToolProperty` |
| `ContourTool` | Contour 검출과 면적 필터링 | `IOpenCVPropertyContour` 구현체 |
| `BlobTool` | Blob 라벨링과 면적 필터링 | `IOpenCVPropertyBlob` 구현체 |
| `MatchingTool` | Template Matching, Scale/Angle 탐색 | `IOpenCVPropertyMatching` 구현체 |
| `EdgeBasedTemplateMatchingTool` | 엣지 기반 템플릿 매칭 | `IOpenCVPropertyEdgeBasedTemplateMatching` 구현체 |
| `SiftTool` | SIFT 특징점 기반 매칭 | `IOpenCVPropertyFeatureSIFT` 구현체 |
| `LineGaugeTool` | ROI 내 엣지 검출 후 직선 피팅 | `IOpenCvPropertyLineGauge` 구현체 |
| `MeanTool` | ROI 평균/표준편차 계산 | `IOpenCVPropertyMean` 구현체 |

## 기본 사용 예제

### ThresholdTool

```csharp
using System;
using Lib.OpenCV;
using Lib.OpenCV.Property;
using Lib.OpenCV.Tool;
using OpenCvSharp;

public static class ThresholdExample
{
    public static void Run()
    {
        using (Mat source = Cv2.ImRead("sample.png", ImreadModes.Color))
        {
            ThresholdTool tool = new ThresholdTool();
            tool.SetProperty(new ThresholdToolProperty
            {
                Mode = ThresholdToolMode.Threshold,
                Threshold = 120,
                MaxValue = 255,
                ThresholdType = ThresholdTypes.Binary
            });

            VisionToolResult result = tool.Execute(source);
            if (!result.Success)
            {
                throw new InvalidOperationException($"{result.ErrorName}: {result.Message}");
            }

            Cv2.ImWrite("result_threshold.png", result.ResultImage);
            result.ResultImage?.Dispose();
        }
    }
}
```

### Filter 후 Edge 검출

Canny 기반 Edge 검출은 단일 채널 입력을 사용하는 것이 안전합니다.

```csharp
using Lib.OpenCV;
using Lib.OpenCV.Property;
using Lib.OpenCV.Tool;
using OpenCvSharp;

using (Mat source = Cv2.ImRead("sample.png", ImreadModes.Grayscale))
{
    FilterTool filter = new FilterTool();
    filter.SetProperty(new FilterToolProperty
    {
        FilterType = FilterToolType.GaussianBlur,
        KernelWidth = 5,
        KernelHeight = 5
    });

    VisionToolResult filtered = filter.Execute(source);
    if (!filtered.Success)
    {
        throw new Exception(filtered.Message);
    }

    EdgeDetectionTool edge = new EdgeDetectionTool();
    edge.SetProperty(new EdgeDetectionToolProperty
    {
        EdgeType = EdgeDetectionToolType.Canny,
        CannyThresholdLow = 80,
        CannyThresholdHigh = 160,
        CannyApertureSize = 3
    });

    VisionToolResult edgeResult = edge.Execute(filtered.ResultImage);
    if (!edgeResult.Success)
    {
        throw new Exception(edgeResult.Message);
    }

    Cv2.ImWrite("result_edge.png", edgeResult.ResultImage);

    filtered.ResultImage?.Dispose();
    edgeResult.ResultImage?.Dispose();
}
```

### BlobTool

`BlobTool`은 `IOpenCVPropertyBlob` 구현체가 필요합니다. 애플리케이션의 설정 모델이 이 인터페이스를 구현해도 되고, 아래처럼 전용 클래스를 만들어도 됩니다.

```csharp
using System.Collections.Generic;
using Lib.OpenCV.Blob;
using OpenCvSharp;

public sealed class BlobProperty : IOpenCVPropertyBlob
{
    public string NAME { get; set; } = "Blob";
    public double PIXELPERMM { get; set; } = 1;
    public bool USE_THRESHOLD { get; set; } = true;
    public bool USE_BITWISENOT { get; set; }
    public ThresholdTypes THRESHOLD_TYPES { get; set; } = ThresholdTypes.Binary;
    public double THRESHOLD { get; set; } = 120;
    public bool USE_ADAPTIVE_THRESHOLD { get; set; }
    public double ADAPTIVE_THRESHOLD { get; set; } = 255;
    public ThresholdTypes ADAPTIVE_THRESHOLD_TYPES { get; set; } = ThresholdTypes.Binary;
    public AdaptiveThresholdTypes ADAPTIVE_THRESHOLD_ALGORITHM { get; set; } = AdaptiveThresholdTypes.MeanC;
    public int BlockSize { get; set; } = 25;
    public int Weight { get; set; } = 5;
    public bool USE_ROI { get; set; }
    public bool USE_MULTI_ROI { get; set; }
    public Rect CvROI { get; set; } = new Rect();
    public List<Rect> CvROIS { get; set; } = new List<Rect>();
    public List<Rect> CvMASKS { get; set; } = new List<Rect>();
    public int MIN_AREA { get; set; } = 20;
    public int MAX_AREA { get; set; } = 100000;
}
```

사용:

```csharp
using System;
using Lib.OpenCV.Blob;
using Lib.OpenCV.Tool;
using OpenCvSharp;

using (Mat source = Cv2.ImRead("sample.png", ImreadModes.Grayscale))
{
    BlobTool tool = new BlobTool();
    tool.SetProperty(new BlobProperty
    {
        USE_THRESHOLD = true,
        THRESHOLD = 120,
        MIN_AREA = 50,
        MAX_AREA = 5000,
        USE_ROI = true,
        CvROI = new Rect(100, 100, 300, 200)
    });

    VisionToolResult result = tool.Execute(source);
    if (!result.Success)
    {
        throw new Exception(result.Message);
    }

    foreach (BlobResult blob in tool.results)
    {
        Console.WriteLine($"#{blob.Index}, Area={blob.Area}, Center={blob.Center}");
    }

    result.ResultImage?.Dispose();
}
```

## Pipeline 사용

파이프라인은 여러 Tool을 layer 기반으로 순차 실행합니다.

현재 기본 `VisionPipelineToolFactory`가 생성할 수 있는 Tool은 다음입니다.

- `threshold`
- `morphology`
- `filter`
- `edge` 또는 `edgedetection`
- `rotatescale`

예제:

```csharp
using Lib.OpenCV.Pipeline;
using Lib.OpenCV.Property;
using OpenCvSharp;

VisionPipeline pipeline = new VisionPipeline
{
    Name = "Preprocess"
};

VisionPipelineStep threshold = new VisionPipelineStep
{
    Name = "Binary",
    ToolType = "threshold",
    InputLayer = "input",
    OutputLayer = "binary"
};

threshold.Parameters[nameof(ThresholdToolProperty.Mode)] = "Threshold";
threshold.Parameters[nameof(ThresholdToolProperty.Threshold)] = "120";
threshold.Parameters[nameof(ThresholdToolProperty.MaxValue)] = "255";

pipeline.Steps.Add(threshold);

using (Mat source = Cv2.ImRead("sample.png", ImreadModes.Color))
using (VisionPipelineContext context = new VisionPipelineContext())
{
    context.SetLayer("input", source);

    VisionPipelineRuntime runtime = new VisionPipelineRuntime();
    VisionPipelineRunResult runResult = runtime.Run(pipeline, context);

    if (!runResult.Success)
    {
        VisionPipelineStepResult failed = runResult.StepResults[runResult.StepResults.Count - 1];
        throw new Exception(failed.ToolResult?.Message ?? failed.AcceptanceMessage);
    }

    using (Mat binary = context.GetLayer("binary"))
    {
        Cv2.ImWrite("result_pipeline.png", binary);
    }
}
```

## 결과 확인

`VisionToolResult`의 주요 필드:

| 필드 | 의미 |
| --- | --- |
| `Success` | Tool 실행 성공 여부 |
| `Message` | 실패 또는 검증 메시지 |
| `ErrorCode`, `ErrorName` | 실패 원인을 구분하기 위한 에러 코드 |
| `ResultStatus` | `Passed`, `InvalidInput`, `InvalidParameter`, `InvalidRoi`, `Exception` 등 상태 |
| `ResultImage` | Tool 실행 후 결과 이미지 |
| `Elapsed` | 실행 시간 |
| `Metrics` | 결과 개수, 이미지 크기, 면적/스코어/각도 등 수치 정보 |
| `Overlays` | UI 표시용 사각형, 점, 라인 등 오버레이 정보 |

## ROI와 전처리 규칙

`IOpenCVPropertyBase`를 구현하는 Tool은 공통 전처리 옵션을 사용할 수 있습니다.

- `USE_ROI`: 단일 ROI 사용
- `USE_MULTI_ROI`: 여러 ROI 사용
- `CvROI`: 단일 ROI
- `CvROIS`: 여러 ROI 목록
- `CvMASKS`: 결과 제외 영역
- `USE_THRESHOLD`: 실행 전 Threshold 적용
- `USE_ADAPTIVE_THRESHOLD`: 실행 전 Adaptive Threshold 적용
- `USE_BITWISENOT`: 흑백 반전

ROI의 폭 또는 높이가 0인 경우 Tool에 따라 전체 이미지로 대체되거나 실패합니다. `LineGaugeTool`처럼 ROI가 필수인 Tool은 유효한 `CvROI` 또는 `CvROIS`를 지정해야 합니다.

## 레거시 API

기존 호환을 위해 `CV*`, `C*` 계열 클래스가 남아 있습니다.

예:

- `CVBlob`, `CResultBlob`
- `CVMatching`, `CResultMatching`
- `CVLineGuage`, `CVLineGuage_Result`
- `COpenCVAlgorithmBase`
- `COpenCVHelper`

새 코드에서는 가능하면 `BlobTool`, `MatchingTool`, `LineGaugeTool`, `OpenCvAlgorithmBase`, `VisionToolResult` 기반 API를 사용하는 것을 권장합니다. 레거시 API는 기존 애플리케이션 호환을 위해 유지됩니다.

## 패키징 참고

공통 패키지 메타데이터는 `Directory.Build.props`에 정의되어 있습니다.

- `Version`: `2.1.0`
- `PackageOutputPath`: `artifacts/packages`
- `GeneratePackageOnBuild`: `false`

패키지를 생성하려면 필요한 프로젝트를 명시해서 pack을 실행합니다.

```powershell
dotnet pack Lib.Common\Lib.Common.csproj -c Release
dotnet pack Lib.OpenCV\Lib.OpenCV.csproj -c Release
dotnet pack Lib.OpenCV.Blob\Lib.OpenCV.Blob.csproj -c Release
```

`Lib.Common`은 `OpenCvSharpExtern.dll`을 `runtimes/win-x64/native` 경로로 패키징하고, `buildTransitive/Lib.Common.targets`를 통해 출력 폴더로 복사합니다.
