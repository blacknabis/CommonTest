---
trigger: always_on
glob: "**/*.cs"
description: Rules for using the Common library in the project.
---

# Common Library & Project Guidelines

## 공용 라이브러리 (Common)
- 모든 코딩 작업 시 `Assets/Scripts/Common` 폴더에 있는 유틸리티 스크립트를 최우선으로 활용할 것.
- `Assets/Scripts/Common/README.md`에 정의된 규칙을 반드시 준수하여 코드를 작성할 것.
- **언어 사용 (Language)**: 모든 주석, 문서, 커밋 메시지, 그리고 AI와의 대화는 항상 **한국어(Korean)**를 사용한다.
- **문서 경로 (Documentation)**: 개발 노트(DevLog), 기획 문서 등 모든 문서 파일은 `문서/` 폴더(`C:\study\unity\CommonTest\문서\`)에 저장한다.

## 프로젝트 지식 및 디자인 원칙 (NotebookLM)
- **참조 노트북**: [킹덤러쉬](https://notebooklm.google.com/notebook/778e7a86-d054-4560-95de-b7378c2a11d3) (ID: `778e7a86-d054-4560-95de-b7378c2a11d3`)
- **핵심 메커니즘**:
    - **Barracks**: 적을 물리적으로 저지하고 아군 유닛을 배치하여 지연시키는 전략 준수.
    - **Snappy Feedback**: 시각적/청각적 피드백(Juice)을 극대화하여 조작감을 향상시킬 것.
    - **Heroes & Skill Tree**: 영구적인 성장을 지원하는 영웅 및 스킬 시스템 설계 반영.
- 게임 로직 구현 시 위 노트북의 분석 데이터를 바탕으로 밸런스와 아키텍처를 결정할 것.

## 작업 흐름 자동화 (Workflow Automation)
- **Disposable Tools**: 반복적이지 않은 에셋 생성/설정 작업은 `Assets/Scripts/Kingdom/Editor` (또는 해당 모듈 Editor 폴더)에 일회성 스크립트(예: `*Generator.cs`, `*Maker.cs`)를 작성하여 수행하고, 작업 완료 후 즉시 삭제한다.
- **MCP 활용**: 복잡한 씬 조작이나 에셋 설정은 MCP로 개별 조작하기보다, Unity Editor Script API를 활용한 툴을 작성하여 일괄 처리함으로써 안정성을 확보한다.
