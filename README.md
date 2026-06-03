실무에서 자주 사용하는 OpenCvSharp 기반의 비전 검사 알고리즘을 모아둔 C# 라이브러리입니다.
매번 새로 구현하기 번거로운 엣지(Edge), 라인(Line), 템플릿 매칭(Template Matching), 블랍(Blob), 컨투어(Contour) 등의 필수 기능들을 다른 프로젝트에서도 쉽게 가져다 쓸 수 있도록 모듈화했습니다.

개발 환경

IDE: Visual Studio 2022

Framework: C# (.NET Framework 4.8 기반, .NET 호환)

Library: OpenCvSharp

🚀 주요 기능
Edge / Line: 엣지 및 라인 검출

Template Matching: 템플릿 매칭을 통한 객체 탐색

Blob: 블랍 검출 및 특징 분석

Contour: 외곽선(컨투어) 추출 및 가공

📂 프로젝트 구성
Lib.Common

공통 유틸리티, 데이터 모델, 헬퍼 등 프로젝트 전반에서 사용하는 공용 코드

Lib.OpenCV

OpenCvSharp을 활용한 핵심 비전 검사 모듈 (라인, 매칭, 컨투어 등)

Lib.OpenCV.Blob

블랍 분석 및 처리에 특화된 전용 모듈
