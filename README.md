# OpenVisionLab Vision SDK

> **3.0 이름 변경:** `Library-Noah`와 `Lib.* 2.9.1`은 기존 소비자용 호환
> 기준으로 남고, 이 소스는 `OpenVisionLab.* 3.0.0` 패키지·DLL·네임스페이스를
> 빌드합니다. 기존 프로젝트를 옮길 때는
> [2.9.1 → 3.0.0 마이그레이션 가이드](docs/MIGRATING_LIB_2_9_1_TO_OPENVISIONLAB_3_0.md)를
> 먼저 확인하세요.

OpenCvSharp 기반 2D 검사와 UI 독립적인 height-map/full-XYZ 3D 계산을 제공하는 C# 비전 검사 라이브러리입니다.

2D 영상 처리 도구, 3D 특징 추출과 측정 알고리즘, 공통 결과 상태와 메트릭을 애플리케이션에서 사용하기 쉽게 제공합니다.

## 1분 요약

- `OpenVisionLab.Core`는 UI 독립적인 좌표/라인 계산과 OpenCV native DLL 패키징을 담당합니다.
- `OpenVisionLab.Vision2D`는 Threshold, Filter, Edge, Contour, Matching, LineGauge 등 주요 검사 Tool을 제공합니다.
- `OpenVisionLab.Vision2D.Blob`은 Blob 라벨링과 면적 필터링 기능을 제공합니다.
- `OpenVisionLab.Vision3D`는 height map, full-XYZ geometry, affine/regrid, thickness, warpage, flatness, gap/flush, volume 등 순수 3D 계약과 알고리즘을 제공합니다.
- `OpenVisionLab.Inspection`은 기존 2D Tool과 `IThreeDInspectionTool`을 한 실행 결과로 보존합니다.
- 2D Tool은 `Execute(Mat source)`, height-map 검사 Tool은 `Execute(HeightMap3D source)`로 실행합니다.
- UI 프레임워크에 직접 의존하지 않으며, 측정과 렌더링·ROI 편집·레시피 관리는 호스트 애플리케이션이 담당합니다.

## 설치/참조 방법

소스 프로젝트를 직접 참조하는 경우 사용하는 애플리케이션에서 필요한 프로젝트를 참조합니다.

```xml
<ItemGroup>
  <ProjectReference Include="..\OpenVisionLab-Vision-SDK\src\OpenVisionLab.Vision2D\OpenVisionLab.Vision2D.csproj" />
  <ProjectReference Include="..\OpenVisionLab-Vision-SDK\src\OpenVisionLab.Vision2D.Blob\OpenVisionLab.Vision2D.Blob.csproj" />
  <ProjectReference Include="..\OpenVisionLab-Vision-SDK\src\OpenVisionLab.Vision3D\OpenVisionLab.Vision3D.csproj" />
  <ProjectReference Include="..\OpenVisionLab-Vision-SDK\src\OpenVisionLab.Inspection\OpenVisionLab.Inspection.csproj" />
</ItemGroup>
```

로컬 NuGet 패키지로 사용하는 경우 먼저 패키지를 생성한 뒤 `artifacts/packages`를 패키지 소스로 추가합니다.

```powershell
dotnet pack OpenVisionLab.VisionSdk.sln -c Release
dotnet add package OpenVisionLab.Vision2D --source .\artifacts\packages
dotnet add package OpenVisionLab.Vision2D.Blob --source .\artifacts\packages
dotnet add package OpenVisionLab.Vision3D --source .\artifacts\packages
dotnet add package OpenVisionLab.Inspection --source .\artifacts\packages
```

## 2D Quick Start

아래 예제는 샘플 이미지를 읽고 Canny Edge 결과를 `artifacts/smoke_edge.png`로 저장합니다.

```csharp
using System;
using System.IO;
using OpenVisionLab.Vision2D;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;
using OpenCvSharp;

Directory.CreateDirectory("artifacts");

using (Mat source = Cv2.ImRead("docs/samples/vision_sample.png", ImreadModes.Grayscale))
{
    using EdgeDetectionTool tool = new EdgeDetectionTool();
    tool.SetProperty(new EdgeDetectionToolProperty
    {
        EdgeType = EdgeDetectionToolType.Canny,
        CannyThresholdLow = 80,
        CannyThresholdHigh = 160,
        CannyApertureSize = 3
    });

    using VisionToolResult result = tool.Execute(source);
    if (!result.Success)
    {
        throw new InvalidOperationException($"{result.ErrorName}: {result.Message}");
    }

    Cv2.ImWrite("artifacts/smoke_edge.png", result.ResultImage);
}
```

## 3D Quick Start

아래 예제는 X/Y 격자 단위, 높이 단위, 좌표 프레임과 최소 유효 커버리지를 명시하고 thickness를 검사합니다.

```csharp
using System;
using OpenVisionLab.Vision3D.Geometry;
using OpenVisionLab.Vision3D.Inspection;

HeightMap3D heightMap = HeightMap3D.FromArray(
    values: new[,]
    {
        { 1.00, 1.05, 1.10 },
        { 1.15, double.NaN, 1.20 }
    },
    originX: 0.0,
    originY: 0.0,
    columnPitch: 0.1,
    rowPitch: 0.1,
    planarUnit: "mm",
    heightUnit: "mm",
    frameId: "fixture-top",
    sourceId: "scan-001");

ThicknessInspectionTool tool = new ThicknessInspectionTool(
    new ThicknessInspectionOptions
    {
        MinimumThickness = 0.95,
        MaximumThickness = 1.25,
        MinimumValidSamples = 5,
        MinimumValidCoverageRatio = 0.8,
        InputRequirements = new HeightMapInputRequirements("mm", "mm", "fixture-top")
    });

ThreeDInspectionResult result = tool.Execute(heightMap);
if (result.MeasurementOutcome == ThreeDMeasurementOutcome.NotMeasured)
{
    throw new InvalidOperationException($"{result.ErrorName}: {result.Message}");
}

if (!result.TryGetMetric(ThreeDInspectionMetricNames.Thickness.Mean, out double mean, out string meanUnit))
{
    throw new InvalidOperationException("Thickness mean was not produced.");
}

Console.WriteLine($"{result.MeasurementOutcome}, Mean={mean} {meanUnit}");
```

`MeasurementOutcome`은 `Passed`, `OutOfTolerance`, `NotMeasured`를 직접 구분합니다. 기존 `Success=false`이면서 `HasMeasurement=true`인 조합은 `OutOfTolerance`이며, 단위·프레임 불일치, 잘못된 ROI, 샘플 수나 커버리지 부족은 `NotMeasured`입니다. 상세 계약은 [3D inspection](docs/three-d-inspection.md)을 참고하세요.

## 동반 검증 애플리케이션

OpenVisionLab Vision SDK는 UI를 포함하지 않습니다. 다음 공개 애플리케이션에서 실제 편집·실행·검토 흐름을 개발하고 검증합니다.

| 애플리케이션 | OpenVisionLab Vision SDK 사용 경계 |
| --- | --- |
| [OpenVisionLab](https://github.com/Noah8218/OpenVisionLab) | OpenCvSharp 4 기반 2D rule-based 검사 워크벤치. `OpenVisionLab.Core`, `OpenVisionLab.Vision2D`, `OpenVisionLab.Vision2D.Blob`의 Tool, 레이어, 파이프라인과 결과 표시 흐름을 검증합니다. |
| [OpenVisionLab 3D Studio](https://github.com/Noah8218/OpenVisionLab-3D-Studio) | C3D/mesh/point-cloud/height-map용 3D 검사 워크벤치. 고정된 `OpenVisionLab.Vision3D` NuGet 패키지와 명시적 어댑터를 통해 ROI, Preview/Run, 메트릭, 오버레이와 레시피 replay를 검증합니다. |

두 애플리케이션은 OpenVisionLab Vision SDK 소스 체크아웃에 암묵적으로 연결되지 않습니다. 특히 3D Studio는 검증된 패키지 버전을 고정하므로 새 API는 패키지·해시·어댑터를 명시적으로 갱신한 뒤 사용해야 합니다.

## 3D 입력 계약

`HeightMap3D`의 좌표 규칙은 고정되어 있습니다.

```text
X = OriginX + Column * ColumnPitch
Y = OriginY + Row * RowPitch
H = Values[Row * Columns + Column]
```

| 항목 | 계약 |
| --- | --- |
| `PlanarUnit` | `OriginX`, `OriginY`, `ColumnPitch`, `RowPitch`의 단위 |
| `HeightUnit` | scalar height `H`와 높이 기반 허용값의 단위 |
| `FrameId` | X/Y/H 데이터가 선언된 좌표 프레임 ID |
| `SourceId` | 입력 추적용 ID. 좌표 호환성을 증명하지는 않음 |
| `double.NaN` | 결측 샘플. 보간하거나 이웃을 연결하지 않고 제외 |
| `±Infinity` | 손상된 입력. `HeightMap3D` 생성 시 거부 |

`HeightMapInputRequirements`가 있으면 단위와 프레임을 대소문자까지 정확히 비교하며 자동 단위 변환, 별칭 추론 또는 좌표 변환을 수행하지 않습니다. `MinimumValidSamples`와 `MinimumValidCoverageRatio`를 모두 만족해야 측정이 시작됩니다. 기존 단일 `Unit` 생성자는 호환성을 위해 평면과 높이에 같은 단위를 선언합니다.

## 샘플 데이터

- 입력 샘플: `docs/samples/vision_sample.png`
- README 검출 결과 이미지: `docs/images/*.png`

기본 예제는 저장소 루트에서 실행하는 것을 기준으로 `docs/samples/vision_sample.png`를 사용합니다. 다른 위치에서 실행하는 경우 이미지 경로를 실행 파일 기준으로 조정하세요.

## Matching Contract References

- Auto MPoint teaching core: `docs/AUTO_MPOINT_V1.md`
- Edge-based fail-closed unique result: `docs/EDGE_BASED_UNIQUE_MATCH_V1.md`

## Build / Smoke Check

빌드 확인:

```powershell
dotnet restore OpenVisionLab.VisionSdk.sln
dotnet build OpenVisionLab.VisionSdk.sln -c Debug
dotnet run --project tests\OpenVisionLab.Inspection.Smoke\OpenVisionLab.Inspection.Smoke.csproj -c Debug --no-build
```

패키징까지 포함한 smoke check:

```powershell
dotnet restore OpenVisionLab.VisionSdk.sln
dotnet build OpenVisionLab.VisionSdk.sln -c Debug
dotnet run --project tests\OpenVisionLab.Inspection.Smoke\OpenVisionLab.Inspection.Smoke.csproj -c Debug --no-build
dotnet pack OpenVisionLab.VisionSdk.sln -c Debug --no-build
```

`OpenVisionLab.Inspection.Smoke`는 합성 2D/3D 입력으로 결정론적 계약과 회귀를 검사합니다. 실제 센서 데이터, 교정, Gauge R&amp;R 또는 생산 승인 시험을 대체하지 않습니다.

## CI

GitHub Actions workflow는 `.github/workflows/build.yml`에 있습니다. `main` 브랜치 push와 pull request에서 다음 작업을 수행합니다.

1. .NET SDK 설치
2. `dotnet restore OpenVisionLab.VisionSdk.sln`
3. `dotnet build OpenVisionLab.VisionSdk.sln -c Debug --no-restore`
4. `dotnet run --project tests/OpenVisionLab.Inspection.Smoke/OpenVisionLab.Inspection.Smoke.csproj -c Debug --no-build`
5. `dotnet pack OpenVisionLab.VisionSdk.sln -c Debug --no-build`

## 라이선스

이 프로젝트는 MIT License로 배포됩니다. 상업적 사용, 수정, 배포는 허용되지만, 이 프로젝트 또는 주요 소스 일부를 사용하는 경우 저작권 고지, 라이선스 문구, NOTICE의 귀속 고지를 유지해야 합니다.

Copyright (c) 2026 최노아(Noah-Choi)

- 라이선스 전문: [LICENSE](LICENSE)
- 귀속 고지: [NOTICE](NOTICE)

재배포, 패키징, 또는 파생 작업에 이 라이브러리의 주요 부분이 포함되는 경우 `LICENSE`와 `NOTICE`를 삭제하거나 흐리게 표시하지 마세요.

## 개발 환경

- Visual Studio 2022 또는 .NET SDK
- C# / .NET Standard 2.0
- Windows 런타임 권장
- OpenCvSharp 관련 DLL은 `src/OpenVisionLab.Core/DLL`에 포함되어 있습니다.

빌드:

```powershell
dotnet restore OpenVisionLab.VisionSdk.sln
dotnet build OpenVisionLab.VisionSdk.sln -c Release
```

## 프로젝트 구조

```text
OpenVisionLab-Vision-SDK
|- src
|  |- OpenVisionLab.Core
|  |  |- Converter
|  |  |- Line
|  |  |- DLL
|  |  `- build
|  |- OpenVisionLab.Vision2D
|  |  `- OpenCV
|  |     |- Pipeline
|  |     |- Property
|  |     |- Result
|  |     `- Tool
|  |- OpenVisionLab.Vision2D.Blob
|  |- OpenVisionLab.Vision3D
|  |  |- Geometry
|  |  |- FeatureExtraction
|  |  |  |- Filtering
|  |  |  |- GeometryConstruction
|  |  |  |- GridAndStatistics
|  |  |  |- Metrology
|  |  |  |- Mesh
|  |  |  |- Registration
|  |  |  `- SurfaceMatching
|  |  `- Inspection
|  `- OpenVisionLab.Inspection
`- tests
   `- OpenVisionLab.Inspection.Smoke
      |- Suites
      `- Support
```

| 프로젝트 | 역할 |
| --- | --- |
| `OpenVisionLab.Core` | UI 독립적인 좌표/ROI 변환, 수치·기하 계산, 라인 계산, OpenCV 런타임 자산 |
| `OpenVisionLab.Vision2D` | 주요 OpenCV 검사 도구, 속성 인터페이스, 결과 모델, 파이프라인 실행 구조 |
| `OpenVisionLab.Vision2D.Blob` | Blob 라벨링/면적 필터링 도구 |
| `OpenVisionLab.Vision3D` | UI 독립적인 height-map/full-XYZ 계약, 특징 추출과 3D 검사 알고리즘 |
| `OpenVisionLab.Inspection` | 2D와 3D Tool을 순서대로 실행하고 각 원래 결과를 보존하는 실행 계약 |
| `OpenVisionLab.Inspection.Smoke` | 합성 입력을 사용하는 실행형 계약·회귀 검증. entry point와 도메인별 suite, 공통 지원 코드가 분리됨 |

참조 관계:

```text
OpenVisionLab.Core
|- OpenVisionLab.Vision2D
|  `- OpenVisionLab.Vision2D.Blob
|- OpenVisionLab.Vision3D
`- OpenVisionLab.Inspection
   |- OpenVisionLab.Vision2D
   `- OpenVisionLab.Vision3D
```

## 코드 구조

### OpenVisionLab.Core

- `Converter`: `Point`, `Rect`, `Rectangle` 등 UI 독립적인 좌표·기하 변환 유틸리티
- `Line`: 직선 피팅, 수직선 계산, 교차점 계산용 모델과 계산기
- `CFormula`, `FormulaUtil`: 각도, 교차점, 원근 변환, 폴리곤 판정 등 수식 유틸리티
- `DLL`, `build`: OpenCvSharp managed/native 런타임 자산과 소비자 출력 복사 계약

### OpenVisionLab.Vision2D

- `OpenCV/Tool`: 실제 검사 도구 구현
- `OpenCV/Property`: 각 도구가 사용하는 설정 인터페이스와 일부 기본 속성 클래스
- `OpenCV/Result`: Matching, Contour, Mean, LineGauge 등 도구별 결과 모델
- `OpenCV/Pipeline`: 여러 도구를 순차 실행하는 파이프라인 모델과 런타임
- `OpenCvHelper`: Mat 유효성 검사와 채널 변환 유틸리티

### OpenVisionLab.Vision2D.Blob

- `BlobTool`: 새 실행 구조를 사용하는 Blob 도구
- `BlobResult`: Blob 결과 모델
- `CVBlob`, `CResultBlob`: 기존 코드 호환을 위한 레거시 API

### OpenVisionLab.Vision3D

- `Geometry`: immutable `HeightMap3D`, X/Y/H 격자와 ROI 계약
- `FeatureExtraction`: source-neutral full-XYZ 선/평면/affine, reference-grid regrid, median/edge/line-fit 알고리즘
- `Inspection`: thickness, warpage, datum deviation과 독립적인 3D 치수 검사

### OpenVisionLab.Inspection

- `CombinedInspectionRunner`: 2D `IVisionTool`과 3D `IThreeDInspectionTool`을 독립적으로 실행
- `CombinedInspectionRunResult`: 실패 이후 단계의 증거를 포함해 원래 결과 형식을 보존

## 2D Tool 실행 구조

새 2D 이미지 Tool은 대부분 `OpenCvAlgorithmBase`를 상속합니다.

```text
IVisionTool
`- OpenCvAlgorithmBase
   |- ThresholdTool
   |- MorphologyTool
   |- FilterTool
   |- EdgeDetectionTool
   |- RotateScaleTool
   |- ContourTool
   |- CornerTool
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
using VisionToolResult result = tool.Execute(source);

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

## 지원 2D Tool 요약

| Tool | 주요 용도 | Property |
| --- | --- | --- |
| `ThresholdTool` | 이진화, 범위 이진화, Adaptive Threshold | `ThresholdToolProperty` |
| `MorphologyTool` | Erode, Dilate, Open, Close 등 형태학 연산 | `MorphologyToolProperty` |
| `FilterTool` | Blur, Gaussian, Median, Bilateral 등 필터 | `FilterToolProperty` |
| `EdgeDetectionTool` | Canny, Sobel, Scharr, Laplacian 엣지 검출 | `EdgeDetectionToolProperty` |
| `RotateScaleTool` | 이미지 회전/스케일 변환 | `RotateScaleToolProperty` |
| `ContourTool` | Contour 검출과 면적 필터링 | `ContourToolProperty` 또는 `IOpenCVPropertyContour` 구현체 |
| `CornerTool` | sub-pixel corner 검출과 전역 좌표 결과 | `ContourToolProperty` 또는 `IOpenCVPropertyContour` 구현체 |
| `BlobTool` | Blob 라벨링과 면적 필터링 | `BlobToolProperty` 또는 `IOpenCVPropertyBlob` 구현체 |
| `MatchingTool` | Template Matching, Scale/Angle 탐색 | `MatchingToolProperty` 또는 `IOpenCVPropertyMatching` 구현체 |
| `EdgeBasedTemplateMatchingTool` | 엣지 기반 템플릿 매칭 | `EdgeBasedTemplateMatchingToolProperty` 또는 `IOpenCVPropertyEdgeBasedTemplateMatching` 구현체 |
| `AutoMPointTool` | 고정 크기 매칭 후보 자동 제안, 유일성/합성 변형/속도 검증 | `AutoMPointToolProperty` |
| `SiftTool` | SIFT 특징점 기반 매칭 | `SiftToolProperty` 또는 `IOpenCVPropertyFeatureSIFT` 구현체 |
| `LineGaugeTool` | ROI 내 엣지 검출 후 직선 피팅 | `LineGaugeToolProperty` 또는 `IOpenCvPropertyLineGauge` 구현체 |
| `MeanTool` | ROI 평균/표준편차 계산 | `MeanToolProperty` 또는 `IOpenCVPropertyMean` 구현체 |

`MeanTool`의 multi-ROI 실행은 `CvROIS` 순서대로 각 영역을 측정하고 같은 순서의 `MeanResult.index`를 제공합니다. `CornerTool`은 sub-pixel 보정된 각 점을 전역 이미지 좌표의 `CornerResult`로 제공하며, 검출점이 없으면 `CornerNoResult`를 반환합니다.

## 지원 3D 기능 요약

3D API는 입력 형태에 따라 다음 세 계층으로 사용합니다. `IThreeDInspectionTool`은 단일 `HeightMap3D` 검사만 위한 좁은 인터페이스이며, 다중 surface나 mesh Tool을 이 인터페이스에 넣지 않습니다.

| 계층 | 입력/결과 | 사용 시점 | `CombinedInspectionRunner` |
| --- | --- | --- | --- |
| Height-map 검사 | `HeightMap3D` → `ThreeDInspectionResult` | thickness, warpage, datum처럼 하나의 정규 격자를 검사할 때 | 지원 |
| Source-neutral Tool | Tool별 typed input/options/result | full-XYZ geometry, regrid, filtering, matching, mesh 비교 | 미지원. Tool을 직접 실행 |
| 다중 입력 치수 검사 | 호출자가 준비한 점·영역·통계 → typed result | flatness, point pair, gap/flush, volume, cross-section | 미지원. Tool을 직접 실행 |

Height-map 검사는 입력/ROI/커버리지 오류를 통제된 `NotMeasured` 결과로 반환합니다. Source-neutral 및 다중 입력 Tool은 각 typed result의 `Success` 또는 `Passed` 계약을 사용하며, 잘못 구성된 호출 인자는 `ArgumentException`으로 거부할 수 있습니다. 전체 공개 Tool과 입력 선택 기준은 [3D inspection 문서](docs/three-d-inspection.md#public-tool-catalog)를 참고하세요.

| 영역 | 주요 타입 | 역할 |
| --- | --- | --- |
| Height-map 검사 | `ThicknessInspectionTool`, `WarpageInspectionTool`, `DatumPlaneRawHeightDeviationInspectionTool` | 단위·프레임·ROI·결측 커버리지 계약을 확인한 뒤 scalar map 측정 |
| 기하/정합 | `TwoPointLineTool`, `ThreePointPlaneTool`, `LineIntersectionTool`, `FullXyzAffineSolveTool`, `AffinePointCloudApplyTool` | 명시적 full-XYZ 입력의 순수 기하 계산과 affine solve/apply |
| 정규 격자화 | `ReferenceGridRegridTool` | 명시적 오른손 U/V/H 축으로 nearest-cell regrid, hole 보존과 커버리지 보고 |
| 특징 추출 | `DeterministicMedianFilterTool`, `DeterministicHeightDifferenceEdgeTool`, `DeterministicLineFitTool`, `LeastSquaresHeightFieldPlaneFitTool` | 결정론적 필터·edge·line/plane fit |
| 치수 검사 | `PlaneFlatnessInspectionTool`, `PointPairDimensionsInspectionTool`, `GapFlushInspectionTool`, `VolumeInspectionTool`, `CrossSectionDimensionsInspectionTool` | caller가 준비한 점·영역·평면을 이용한 독립 측정 |

## 기본 사용 예제

### ThresholdTool

```csharp
using System;
using OpenVisionLab.Vision2D;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;
using OpenCvSharp;

public static class ThresholdExample
{
    public static void Run()
    {
        using (Mat source = Cv2.ImRead("docs/samples/vision_sample.png", ImreadModes.Color))
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
using OpenVisionLab.Vision2D;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;
using OpenCvSharp;

using (Mat source = Cv2.ImRead("docs/samples/vision_sample.png", ImreadModes.Grayscale))
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

`BlobToolProperty`는 `IOpenCVPropertyBlob`의 모든 필수 값을 제공하므로 별도 설정 클래스를 작성하지 않고 바로 사용할 수 있습니다. 애플리케이션 전용 저장 모델이 필요하면 기존 인터페이스를 직접 구현할 수도 있습니다.

```csharp
using OpenVisionLab.Vision2D.Blob;

BlobToolProperty property = new BlobToolProperty();
```

사용:

```csharp
using System;
using OpenVisionLab.Vision2D.Blob;
using OpenVisionLab.Vision2D.Tool;
using OpenCvSharp;

using (Mat source = Cv2.ImRead("docs/samples/vision_sample.png", ImreadModes.Grayscale))
{
    BlobTool tool = new BlobTool();
    tool.SetProperty(new BlobToolProperty
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
- `affine`, `affinematrix` 또는 `affinetransform`

Pipeline configuration fails closed:

- Omitted built-in tool parameters use documented defaults. Supplied values must be finite and valid for their declared type.
- Unknown, empty, or case-insensitive duplicate parameter names are rejected with `ArgumentException` before tool execution.
- Empty and disabled-only pipelines return `Success == false`; a pipeline must execute at least one enabled step to pass.
- `UseAcceptance = true` makes the acceptance contract authoritative. `ExpectedSuccess = false` is supported only on the final enabled step and never creates a synthetic output layer.

예제:

```csharp
using OpenVisionLab.Vision2D.Pipeline;
using OpenVisionLab.Vision2D.Property;
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

using (Mat source = Cv2.ImRead("docs/samples/vision_sample.png", ImreadModes.Color))
using (VisionPipelineContext context = new VisionPipelineContext())
{
    context.SetLayer("input", source);

    VisionPipelineRuntime runtime = new VisionPipelineRuntime();
    using VisionPipelineRunResult runResult = runtime.Run(pipeline, context);

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

## 네이티브 이미지 리소스 소유권

- 호출자는 `Execute(Mat source)`에 전달한 입력 `Mat`을 계속 소유합니다. Tool이나 Runner는 이 입력을 해제하지 않습니다.
- `OpenCvAlgorithmBase` 기반 Tool은 내부 source/result/template 복사본을 소유하므로 사용 후 Tool을 `Dispose()`해야 합니다.
- `VisionToolResult`는 `ResultImage`를 소유합니다. 결과를 다 사용한 뒤 `VisionToolResult.Dispose()`를 호출하며, 그 이후에는 기존 `ResultImage` 참조를 사용하지 않습니다.
- `VisionPipelineContext.SetLayer`는 입력 이미지를 복제해 보관하고, `GetLayer`는 호출자가 해제해야 하는 새 복사본을 반환합니다.
- `VisionPipelineRunResult.Dispose()`는 모든 step의 `VisionToolResult`와 결과 이미지를 해제합니다. 기본 Runtime은 기본 팩터리가 생성한 Tool도 해제합니다.
- 사용자 팩터리를 받는 `VisionPipelineRuntime(factory)`는 호환성을 위해 Tool을 호출자 소유로 유지합니다. Runtime이 팩터리 생성 Tool을 소유하게 하려면 `VisionPipelineRuntime(factory, true)`를 사용합니다.
- `CombinedInspectionRunResult.Dispose()`는 포함된 2D 결과 이미지만 해제합니다. 입력 `Image`, `HeightMap`, 전달한 Tool은 호출자 소유입니다.

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

## 검출 결과 이미지 표시

검사 프로그램에서는 Tool 실행 결과를 바로 화면에 표시해야 하는 경우가 많습니다. 이 라이브러리는 UI 프레임워크에 직접 의존하지 않도록 `Mat`과 `VisionToolResult.Overlays`를 제공합니다.

권장 흐름:

1. 원본 이미지 `Mat`을 Tool에 입력합니다.
2. `VisionToolResult`를 받습니다.
3. 표시용 이미지에는 원본 이미지를 복사한 뒤 `Overlays`를 그립니다.
4. UI 프로젝트에서는 자체 프레임워크 어댑터로 표시용 `Mat`을 화면 컨트롤이 요구하는 타입으로 변환합니다. SDK Core는 WinForms/WPF 이미지 타입이나 변환 API를 제공하지 않습니다.

### 실제 검출 예시 이미지

아래 이미지는 README용 샘플 이미지에 Edge, Matching, Edge-Based Matching, Contour, Blob, LineGauge 검출/피팅을 적용한 결과입니다. 각 Tool을 사용하면 어떤 형태의 검출 결과가 화면에 표시되는지 빠르게 확인할 수 있습니다.

<table>
  <tr>
    <th>Edge Detection</th>
    <th>Matching</th>
    <th>Edge-Based Matching</th>
  </tr>
  <tr>
    <td><img src="./docs/images/edge_detection_result.png" alt="Edge Detection result" width="280"></td>
    <td><img src="./docs/images/matching_detection_result.png" alt="Template Matching result" width="280"></td>
    <td><img src="./docs/images/edge_based_matching_result.png" alt="Edge-Based Matching result" width="280"></td>
  </tr>
  <tr>
    <th>Contour</th>
    <th>Blob</th>
    <th>LineGauge</th>
  </tr>
  <tr>
    <td><img src="./docs/images/contour_detection_result.png" alt="Contour detection result" width="280"></td>
    <td><img src="./docs/images/blob_detection_result.png" alt="Blob detection result" width="280"></td>
    <td><img src="./docs/images/line_gauge_result.png" alt="LineGauge result" width="280"></td>
  </tr>
</table>

### 공통 오버레이 렌더러

`MatchingTool`, `EdgeBasedTemplateMatchingTool`, `ContourTool`, `BlobTool`, `LineGaugeTool`은 `VisionToolResult.Overlays`에 사각형, 점, 점 목록, 직선 정보를 담습니다. 다음 헬퍼를 UI 프로젝트에 두면 대부분의 검출 결과를 같은 방식으로 표시할 수 있습니다.

```csharp
using System;
using System.Drawing;
using OpenVisionLab.Vision2D;
using OpenVisionLab.Vision2D.Tool;
using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;

public static class VisionDisplayHelper
{
    public static Mat DrawVisionResult(Mat source, VisionToolResult result)
    {
        if (source == null || source.Empty())
        {
            return new Mat();
        }

        Mat display = source.Clone();
        OpenCvHelper.SetImageChannel3(display);

        if (result == null || !result.Success)
        {
            return display;
        }

        foreach (VisionToolOverlay overlay in result.Overlays)
        {
            DrawOverlay(display, overlay);
        }

        return display;
    }

    private static void DrawOverlay(Mat image, VisionToolOverlay overlay)
    {
        Scalar color = new Scalar(50, 205, 50);

        switch (overlay.Kind)
        {
            case VisionToolOverlayKind.Rectangle:
                DrawRectangle(image, overlay.Bounds, color);
                DrawText(image, overlay.Label, overlay.Bounds.X, overlay.Bounds.Y - 6, color);
                if (overlay.Center != PointF.Empty)
                {
                    DrawPoint(image, overlay.Center, Scalar.Yellow);
                }
                break;

            case VisionToolOverlayKind.Point:
                DrawPoint(image, overlay.Center, color);
                DrawText(image, overlay.Label, overlay.Center.X + 5, overlay.Center.Y - 5, color);
                break;

            case VisionToolOverlayKind.Points:
                foreach (PointF point in overlay.Points)
                {
                    DrawPoint(image, point, Scalar.Yellow, 2);
                }
                DrawText(image, overlay.Label, overlay.Center.X + 5, overlay.Center.Y - 5, color);
                break;

            case VisionToolOverlayKind.Line:
                Scalar lineColor = new Scalar(255, 191, 0);
                Cv2.Line(image, ToCvPoint(overlay.Start), ToCvPoint(overlay.End), lineColor, 2, LineTypes.AntiAlias);
                DrawText(image, overlay.Label, overlay.Center.X + 5, overlay.Center.Y - 5, lineColor);
                break;
        }
    }

    private static void DrawRectangle(Mat image, RectangleF bounds, Scalar color)
    {
        Rect rect = new Rect(
            (int)Math.Round(bounds.X),
            (int)Math.Round(bounds.Y),
            Math.Max(1, (int)Math.Round(bounds.Width)),
            Math.Max(1, (int)Math.Round(bounds.Height)));

        Cv2.Rectangle(image, rect, color, 2, LineTypes.AntiAlias);
    }

    private static void DrawPoint(Mat image, PointF point, Scalar color, int radius = 4)
    {
        Cv2.Circle(image, ToCvPoint(point), radius, color, Cv2.FILLED, LineTypes.AntiAlias);
    }

    private static void DrawText(Mat image, string text, float x, float y, Scalar color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Cv2.PutText(
            image,
            text,
            new CvPoint(Math.Max(0, (int)Math.Round(x)), Math.Max(15, (int)Math.Round(y))),
            HersheyFonts.HersheySimplex,
            0.45,
            color,
            1,
            LineTypes.AntiAlias);
    }

    private static CvPoint ToCvPoint(PointF point)
    {
        return new CvPoint((int)Math.Round(point.X), (int)Math.Round(point.Y));
    }
}
```

사용:

```csharp
VisionToolResult result = tool.Execute(source);

using (Mat display = VisionDisplayHelper.DrawVisionResult(source, result))
{
    Cv2.ImWrite("display_result.png", display);

    // UI가 필요하면 소비자 프로젝트의 프레임워크별 어댑터에서 display Mat을 변환합니다.
}
```

### Tool별 표시 기준

| Tool | 표시 방법 |
| --- | --- |
| `EdgeDetectionTool` | `result.ResultImage`가 Edge 이미지입니다. 그대로 표시하거나, 필요하면 `OpenCvHelper.SetImageChannel3` 후 색상 표시를 추가합니다. |
| `MatchingTool` | `tool.results`에 `MatchingResult`가 들어 있고, `result.Overlays`에 매칭 사각형/중심점/점수 라벨이 들어갑니다. 공통 오버레이 렌더러를 사용하면 됩니다. |
| `EdgeBasedTemplateMatchingTool` | `MatchingTool`과 같은 `MatchingResult` 구조를 사용합니다. `USE_DRAW_IMAGE = true`이면 Tool 내부에서 Edge 모델 윤곽을 `ResultImage`에 그립니다. |
| `ContourTool` | `USE_DRAW_IMAGE = true`이면 `ResultImage`에 Contour가 그려집니다. UI에서 일관된 스타일이 필요하면 공통 오버레이 렌더러를 사용합니다. |
| `BlobTool` | `tool.results`에 `BlobResult`가 들어 있고, `result.Overlays`에 Bounding/Center/Area 정보가 들어갑니다. 공통 오버레이 렌더러를 사용하면 됩니다. |
| `LineGaugeTool` | `tool.resultList`에 FitLine과 Edge 목록이 들어 있고, `result.Overlays`에 Edge points와 Fit line이 들어갑니다. 공통 오버레이 렌더러를 사용하면 됩니다. |

### Matching / EdgeBasedMatching 표시 예제

```csharp
using OpenVisionLab.Vision2D.Result;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;
using OpenCvSharp;

MatchingTool tool = new MatchingTool();
tool.SetProperty(new MatchingToolProperty
{
    USE_FIND_ANGLE = false,
    NUM_MATCH = 1
});
tool.SetTemplateImage(template);

VisionToolResult result = tool.Execute(source);

using (Mat display = VisionDisplayHelper.DrawVisionResult(source, result))
{
    Cv2.ImWrite("display_matching.png", display);
}

foreach (MatchingResult match in tool.results)
{
    Console.WriteLine($"#{match.Index}, Score={match.Score:0.000}, Center={match.Center}, Angle={match.Angle:0.00}, Scale={match.Scale:0.000}");
}
```

엣지 기반 매칭도 표시 방식은 동일합니다.

```csharp
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;

EdgeBasedTemplateMatchingTool tool = new EdgeBasedTemplateMatchingTool();
tool.SetProperty(new EdgeBasedTemplateMatchingToolProperty());
tool.SetTemplateImage(template);

VisionToolResult result = tool.Execute(source);

using (Mat display = VisionDisplayHelper.DrawVisionResult(source, result))
{
    Cv2.ImWrite("display_edge_matching.png", display);
}
```

### Contour / Blob 표시 예제

```csharp
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;

ContourTool contourTool = new ContourTool();
contourTool.SetProperty(new ContourToolProperty
{
    MIN_AREA = 50,
    MAX_AREA = 5000
});

VisionToolResult contourResult = contourTool.Execute(source);

using (Mat contourDisplay = VisionDisplayHelper.DrawVisionResult(source, contourResult))
{
    Cv2.ImWrite("display_contour.png", contourDisplay);
}
```

```csharp
using OpenVisionLab.Vision2D.Blob;

BlobTool blobTool = new BlobTool();
blobTool.SetProperty(new BlobToolProperty
{
    MIN_AREA = 50,
    MAX_AREA = 5000
});

VisionToolResult blobResult = blobTool.Execute(source);

using (Mat blobDisplay = VisionDisplayHelper.DrawVisionResult(source, blobResult))
{
    Cv2.ImWrite("display_blob.png", blobDisplay);
}
```

### LineGauge 표시 예제

```csharp
using OpenCvSharp;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Tool;

LineGaugeTool lineTool = new LineGaugeTool();
lineTool.SetProperty(new LineGaugeToolProperty
{
    CvROI = new Rect(100, 100, 300, 200)
});

VisionToolResult lineResult = lineTool.Execute(source);

using (Mat lineDisplay = VisionDisplayHelper.DrawVisionResult(source, lineResult))
{
    Cv2.ImWrite("display_line_gauge.png", lineDisplay);
}

foreach (var item in lineTool.resultList)
{
    Console.WriteLine($"#{item.Index}, EdgeCount={item.EdgePointCount}, FitLine={item.FitLine.Start}->{item.FitLine.End}");
}
```

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

## Known Limitations

- Windows x64 환경을 우선 지원합니다. `OpenCvSharpExtern.dll`은 `runtimes/win-x64/native` 경로로 패키징됩니다.
- UI 프레임워크는 포함하지 않습니다. 화면 표시는 `VisionToolResult.ResultImage`와 `VisionToolResult.Overlays`를 애플리케이션에서 렌더링해야 합니다.
- 일부 `CV*`, `C*` 계열 레거시 API가 호환성을 위해 남아 있습니다. 신규 코드는 `*Tool`과 `VisionToolResult` 기반 API를 권장합니다.
- `OpenVisionLab.Inspection.Smoke`는 합성 데이터 기반 계약 회귀이며 실제 센서·교정·생산 metrology를 증명하지 않습니다.
- `HeightMapInputRequirements`를 생략하면 2.x 호환 모드로 수치/ROI만 검사합니다. 생산 recipe는 기대 단위와 프레임을 명시해야 합니다.
- OpenCvSharp DLL은 저장소에 포함된 버전을 기준으로 동작합니다. DLL 버전을 교체할 때는 native DLL 호환성과 패키징 결과를 함께 확인해야 합니다.

## 패키징 참고

공통 패키지 메타데이터는 `Directory.Build.props`에 정의되어 있습니다.

- `Version`: `3.0.0`
- `PackageOutputPath`: `artifacts/packages`
- `GeneratePackageOnBuild`: `false`

각 NuGet 패키지는 역할과 첫 사용법이 다른 전용 README를 포함합니다.

| 패키지 | 패키지 README |
| --- | --- |
| `OpenVisionLab.Core` | [native runtime과 공통 지원](src/OpenVisionLab.Core/README.md) |
| `OpenVisionLab.Vision2D` | [2D Tool Quick Start](src/OpenVisionLab.Vision2D/README.md) |
| `OpenVisionLab.Vision2D.Blob` | [Blob Tool 계약](src/OpenVisionLab.Vision2D.Blob/README.md) |
| `OpenVisionLab.Vision3D` | [Surface Match와 Mesh Quick Start](src/OpenVisionLab.Vision3D/README.md) |
| `OpenVisionLab.Inspection` | [2D/3D 통합 실행 Quick Start](src/OpenVisionLab.Inspection/README.md) |

패키지를 생성하려면 필요한 프로젝트를 명시해서 pack을 실행합니다.

```powershell
dotnet pack src\OpenVisionLab.Core\OpenVisionLab.Core.csproj -c Release
dotnet pack src\OpenVisionLab.Vision2D\OpenVisionLab.Vision2D.csproj -c Release
dotnet pack src\OpenVisionLab.Vision2D.Blob\OpenVisionLab.Vision2D.Blob.csproj -c Release
dotnet pack src\OpenVisionLab.Vision3D\OpenVisionLab.Vision3D.csproj -c Release
dotnet pack src\OpenVisionLab.Inspection\OpenVisionLab.Inspection.csproj -c Release
```

`OpenVisionLab.Core`는 `OpenCvSharpExtern.dll`을 `runtimes/win-x64/native` 경로로 패키징하고, `buildTransitive/OpenVisionLab.Core.targets`를 통해 출력 폴더로 복사합니다.

GitHub Actions는 pack 결과만 참조하는
`tests/OpenVisionLab.PackageConsumer.Smoke`를 별도로 restore/run합니다. 이 검사는
ProjectReference 없이 2D native 호출, height-map 검사, Surface Match와 Mesh Comparison이
동작하는지 확인합니다.
