# Library-Noah

OpenCvSharp을 사용한 검사 알고리즘 라이브러리입니다.  
엣지(Edeg), 라인(Line), 매칭(Matching), 블랍(Blob), 컨투어(Contour) 등 기본적인 비전 검사 기능을 C#에서 재사용 가능한 형태로 정리했습니다.

> 개발 환경: Visual Studio / C# (.NET Framework 또는 .NET) + OpenCvSharp

## 주요 기능
- 엣지 검출 
- 라인 검출
- 템플릿 매칭 
- 블랍 
- 컨투어 
  
## 프로젝트 구성
- `Lib.Common`  
  공통 유틸/모델/헬퍼 등 (공용 코드)
- `Lib.OpenCV`  
  OpenCvSharp 기반 핵심 기능 (라인/매칭/컨투어 등)
- `Lib.OpenCV.Blob`  
  블랍 분석 관련 기능



  
