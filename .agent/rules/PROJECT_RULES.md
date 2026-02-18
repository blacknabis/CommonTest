---
trigger: always_on
glob: "**/*"
description: 프로젝트 전반에 항상 적용되는 공통 작업 규칙.
---

# Common Library & Project Guidelines

## 공용 라이브러리 (Common)
- 모든 코딩 작업 시 `Assets/Scripts/Common` 폴더에 있는 유틸리티 스크립트를 최우선으로 활용할 것.
- `Assets/Scripts/Common/README.md`에 정의된 규칙을 반드시 준수하여 코드를 작성할 것.
- **언어 사용 (Language)**: 모든 주석, 문서, 커밋 메시지, 그리고 AI와의 대화는 항상 **한국어(Korean)**를 사용한다.
- **문서 경로 (Documentation)**: 개발 노트(DevLog), 기획 문서 등 모든 문서 파일은 `문서/` 폴더(`C:\study\unity\CommonTest\문서\`)에 저장한다.

## AI 에이전트 공통 실행 규칙 (Codex/Antigravity/Kilo 공통)
> 이 섹션은 에이전트 공통 동작 기준이다.  
> 단, 플랫폼 시스템 정책/보안 정책이 이 문서보다 우선한다.

### 1) 규칙 우선순위
1. 플랫폼 시스템/보안 정책
2. 워크스페이스 로컬 규칙 (`AGENTS.md`, 툴 설정)
3. 본 문서 `PROJECT_RULES.md`
4. 사용자의 현재 요청

### 2) 작업 방식
- 요청받은 기능은 **분석만 하지 말고 바로 구현**을 기본으로 한다.
- 작업 중에는 짧은 중간 진행 공유를 유지한다.
- 완료 시에는 다음을 함께 보고한다.
  - 변경 파일 목록
  - 무엇을 왜 바꿨는지
  - 사용자가 직접 확인할 테스트 포인트

### 3) 코드 탐색/수정 규칙
- 파일/문자열 검색은 기본적으로 `rg`를 사용한다.
- 단일 파일의 국소 수정은 `apply_patch`를 우선 사용한다.
- 불필요한 대규모 리포맷/자동변환은 금지한다.
- 기존 구조/네이밍/패턴을 우선 유지한다.

### 4) Git 규칙
- 사용자 요청 없는 `git reset --hard`, 강제 되돌리기 금지.
- 관련 없는 변경사항은 임의로 되돌리지 않는다.
- 커밋/푸시 전에는 반드시 범위를 확인한다.
  - 어떤 파일이 포함되는지
  - 서브모듈 포함 여부
  - 제외 대상(예: 개인 실험 폴더) 여부

### 5) 문서 동기화 규칙
- 기능 작업 시 아래 문서를 함께 갱신한다.
  - `문서/진행/task.md`
  - `문서/진행/작업상세로그_YYYY_MM_DD.md`
- 완료 문서는 `문서/완료/YYYY_MM_DD/`로 이관한다.
- `task.md`는 “현재 트랙” 중심으로 간결하게 유지한다.

### 6) NotebookLM 참조 규칙
- 기획/밸런스/UX 의사결정이 필요하면 NotebookLM `킹덤러쉬`를 우선 참조한다.
- 참조 노트북: `778e7a86-d054-4560-95de-b7378c2a11d3`
- 참조 결과는 문서에 “근거 요약” 형태로 남긴다.

### 7) Unity 작업 규칙
- 프리팹/리소스 경로는 본 문서의 `리소스 폴더 구조`를 따른다.
- 런타임 fallback은 허용하되, 최종 목표는 명시적 프리팹/에셋 바인딩이다.
- 경고 로그는 임시 허용 가능하나, 머지 전 “의도된 로그만 남기기”를 원칙으로 한다.

## 프로젝트 지식 및 디자인 원칙 (NotebookLM)
- **참조 노트북**: [킹덤러쉬](https://notebooklm.google.com/notebook/778e7a86-d054-4560-95de-b7378c2a11d3) (ID: `778e7a86-d054-4560-95de-b7378c2a11d3`)
- **핵심 메커니즘**:
    - **Barracks**: 적을 물리적으로 저지하고 아군 유닛을 배치하여 지연시키는 전략 준수.
    - **Snappy Feedback**: 시각적/청각적 피드백(Juice)을 극대화하여 조작감을 향상시킬 것.
    - **Heroes & Skill Tree**: 영구적인 성장을 지원하는 영웅 및 스킬 시스템 설계 반영.
- 게임 로직 구현 시 위 노트북의 분석 데이터를 바탕으로 밸런스와 아키텍처를 결정할 것.

## 리소스 폴더 구조 (Resource Folder Structure)
- UI 프리팹은 `Assets/Resources/UI/` 폴더에 클래스명과 동일한 이름으로 저장한다. (UIManager가 `Resources/UI/{ClassName}`으로 자동 로드)
- 스프라이트/이미지 에셋은 `Assets/Resources/UI/Sprites/{씬 또는 기능명}/` 하위에 저장한다.
- 폰트 에셋은 `Assets/Resources/UI/Fonts/`에 저장한다.
- 폴더 구조 예시:
  ```
  Assets/Resources/UI/
  ├── TitleView.prefab        ← UI 프리팹 (클래스명 = 파일명)
  ├── GameView.prefab
  ├── Sprites/                ← 이미지 에셋
  │   ├── Title/              ← 씬별 하위 폴더
  │   ├── Game/
  │   └── Common/             ← 공용 아이콘 등
  └── Fonts/                  ← 폰트 에셋
  ```

## 작업 흐름 자동화 (Workflow Automation)
- **Disposable Tools**: 반복적이지 않은 에셋 생성/설정 작업은 `Assets/Scripts/Kingdom/Editor` (또는 해당 모듈 Editor 폴더)에 일회성 스크립트(예: `*Generator.cs`, `*Maker.cs`)를 작성하여 수행하고, 작업 완료 후 즉시 삭제한다.
- **MCP 활용**: 복잡한 씬 조작이나 에셋 설정은 MCP로 개별 조작하기보다, Unity Editor Script API를 활용한 툴을 작성하여 일괄 처리함으로써 안정성을 확보한다.

## 주석 및 코딩 컨벤션 (Code & Comment Guidelines)
> 참조: `antigravity-awesome-skills/skills/documentation-templates/SKILL.md` (Comment Guidelines)

- **주석 언어**: 모든 주석은 **한국어(Korean)**로 작성한다.
- **XML 주석 필수 (`///`)**: `public` 클래스, 메서드, 프로퍼티에는 반드시 XML 주석을 작성하여 인텔리센스를 지원한다.
- **주석 작성 기준**:
  - **Why (이유)**: 비즈니스 로직의 의도나 복잡한 알고리즘의 설명 위주로 작성한다.
  - **What (무엇)**: 코드가 자명한 경우(Self-explanatory) 불필요한 주석은 생략한다.

### 예시 (C# XML Style)
```csharp
/// <summary>
/// 플레이어에게 데미지를 적용하고 사망 처리를 수행합니다.
/// </summary>
/// <param name="amount">적용할 데미지 양</param>
/// <returns>사망 여부 (true: 사망, false: 생존)</returns>
public bool TakeDamage(float amount) 
{
    // ...
}
```

## 개발 환경 (Environment)
- **ComfyUI 설치 경로**: `C:\Stability\Data\Packages\ComfyUI`
