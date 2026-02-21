# CommonTest

Unity 기반 2D 전략/디펜스 프로젝트입니다.  
공통 프레임워크(`Assets/Scripts/Common`)와 게임 로직(`Assets/Scripts/Kingdom`)을 분리해 운영합니다.

## 실행 환경
- Unity: `6000.3.3f1` (프로젝트 기준)
- 주요 패키지: `UniTask`, `TextMeshPro`, `MCPForUnity`

## 빠른 실행
1. Unity로 프로젝트를 엽니다.
2. 메뉴 `Kingdom/Setup/Register Scenes in Build Settings` 실행
3. `Assets/Scenes/InitScene.unity`를 열고 Play

## 현재 진행 트랙
- 메인 작업 보드: `문서/진행/task.md`
- AI Sprite Processor 고도화: `문서/진행/AISpriteProcessor_BaseUnit_TowerBase_고도화_계획_2026_02_20.md`
- ComfyUI 자동화(분리 트랙): `문서/진행/ComfyUI_자동화_트랙_관리_2026_02_20.md`
- 스프라이트 회귀 운영: `문서/진행/스프라이트_회귀_운영_가이드_2026_02_20.md`

## 최근 반영 요약 (2026-02-20)
- `AISpriteProcessor` BaseUnit/TowerBase 워크플로우 운영
- `BarracksSoldierConfig` 도입 및 `TowerConfig` 참조 연결
- 배럭 병사 스프라이트 로딩 우선순위 정리  
  `BarracksSoldierConfig -> legacy(SoldierSpriteResourcePath) -> 관용 경로`
- 마이그레이션 메뉴 추가  
  `Tools/Kingdom/Sprites/Migrate Barracks Soldier Config References`
- 회귀 메뉴 실행 시 모달 다이얼로그 제거(자동 검증 중단 방지)

## 주요 코드 경로
- `Assets/Scripts/Kingdom/App/`: 씬/게임 흐름(`GameScene`, `WorldMapScene` 등)
- `Assets/Scripts/Kingdom/Game/`: 전투/타워/적/영웅 런타임
- `Assets/Scripts/Kingdom/Editor/`: 회귀/검증/마이그레이션 에디터 메뉴
- `Assets/Scripts/Common/Editor/`: `AISpriteProcessor` 등 공통 툴

## 회귀 점검 메뉴
1. `Tools/Common/AI Sprite Processor/Run Preset Load Regression`
2. `Tools/Kingdom/Sprites/Validate Runtime Sprite Bindings`
3. `Tools/Kingdom/Sprites/Run Missing Hint Regression`
4. `Tools/Kingdom/Sprites/Migrate Barracks Soldier Config References` (필요 시)

## 참고
- 공통 모듈 문서: `Assets/Scripts/Common/README.md`
- MCP for Unity 문서: `Assets/MCPForUnity/README.md`

## Config 경로 규칙 (2026-02-21)
- 표준(Resources) 경로는 `Assets/Resources/Kingdom/Configs/<Category>/`를 사용한다.
- 카테고리:
  - `Heroes`
  - `Enemies`
  - `Towers`
  - `Waves`
  - `Stages`
  - `BarracksSoldiers`
- 런타임 로딩은 표준 경로를 우선 사용한다.
- 레거시 경로(`Assets/Resources/Data/...`, `Assets/Resources/Kingdom/Enemies/Config`)는 마이그레이션 호환을 위해 fallback으로만 유지한다.
- 신규/수정 에셋은 레거시 경로에 생성하지 않는다.

상세 마이그레이션 내역: `문서/완료/2026_02_21/리소스_컨피그_경로_통일_마이그레이션_2026_02_21.md`