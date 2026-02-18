# CommonTest

킹덤러쉬 스타일 2D 타워디펜스 프로토타입(Unity) 프로젝트입니다.  
`Common`(공용 프레임워크)와 `Kingdom`(게임 로직)을 분리해서 개발 중입니다.

## 최근 업데이트 (2026-02-18)
- 전투 시작 직후 몬스터 웨이브가 즉시 출현하던 흐름을 개선하여 준비 시간 기반 전투 루프를 반영했습니다.
- AI 스프라이트 후처리 도구 [`Assets/Scripts/Common/Editor/AISpriteProcessor.cs`](Assets/Scripts/Common/Editor/AISpriteProcessor.cs) 고도화:
  - 프리셋 Save/Load/Delete
  - 입력값 검증(잘못된 Rows/Cols/패딩 방지)
  - 선택 영역 Crop 저장
  - SmartSlice(알파 아일랜드 감지) + 프리뷰 시각화
  - 프리뷰에서 전처리(제거 색상/허용 오차) 반영
  - 슬라이스 애니메이션 프리뷰 재생

## 프로젝트 상태
- 씬 흐름: `InitScene -> TitleScene -> WorldMapScene -> GameScene`
- 월드맵/스테이지 선택/저장 연동 동작
- `GameScene` 전투 루프 단계 구현 진행 중
- 현재 반영된 전투 기능(최소 구현):
  - 웨이브/스폰/경로 이동 루프
  - HUD(`Lives/Gold/Wave/Pause/NextWave`)
  - 타워 건설 링 메뉴(타입 선택/비용 표시/골드 부족 비활성)
  - 빈 건설 슬롯 클릭 후 해당 위치 건설
  - 기존 타워 클릭 액션 메뉴(Upgrade/Sell 최소 버전)
  - 피해 공식 공통화(물리/마법/고정, 포병 관통)
  - 공중 타겟 제약(`CanTargetAir`)

## 빠른 실행
1. Unity `6000.3.3f1`(또는 호환 버전)으로 프로젝트를 엽니다.
2. 메뉴 `Kingdom/Setup/Register Scenes in Build Settings` 실행
3. `Assets/Scenes/InitScene.unity`를 열고 Play

## 씬 구성
- `Assets/Scenes/InitScene.unity`: 앱 초기화 진입점
- `Assets/Scenes/TitleScene.unity`: 타이틀 화면
- `Assets/Scenes/WorldMapScene.unity`: 월드맵(스테이지 선택)
- `Assets/Scenes/GameScene.unity`: 인게임 전투 씬

## 주요 코드 위치
- `Assets/Scripts/Common/`
  - UI 프레임워크, AppManager, 유틸리티, 확장 메서드
- `Assets/Scripts/Kingdom/App/`
  - 씬 컨트롤러(`InitScene`, `TitleScene`, `WorldMapScene`, `GameScene`)
- `Assets/Scripts/Kingdom/WorldMap/`
  - 스테이지 노드, 해금 정책, 월드맵 프레젠터/매니저
- `Assets/Scripts/Kingdom/UI/`
  - `WorldMapView`, `GameView` 등 UI 구현
- `Assets/Scripts/Kingdom/Save/`
  - `UserSaveData` 기반 진행도 저장/로드
- `Assets/Scripts/Kingdom/Editor/`
  - 빌드 세팅 등록, 월드맵 자동 배치/검증 등 에디터 도구

## 기술 스택
- Unity (6000.x 계열)
- C#
- uGUI + TextMeshPro
- UniTask (`com.cysharp.unitask`)

## 문서
주요 문서는 `문서/` 폴더에 있습니다.

- 구현 명세: `문서/진행/게임씬_구현명세서.md`
- 작업 체크리스트: `문서/진행/task.md`
- 작업 상세 로그(최신): `문서/진행/작업상세로그_2026_02_18.md`
- 개발일지: `문서/개발일지/DevLog_2026_02_15_1.md`
- 이미지 프롬프트(나노바나나): `문서/이미지프롬프트/StageInfoPopup_이미지프롬프트_나노바나나.md`

### AI Sprite Processor 관련 문서
- 작업 계획/인수인계: `문서/진행/AISpriteProcessor_작업계획_및_인수인계서_2026_02_18.md`
- 진행 체크: `문서/진행/task.md`
- 작업 로그: `문서/진행/작업상세로그_2026_02_18.md`

## 참고
- 공용 라이브러리 문서: `Assets/Scripts/Common/README.md`
- Unity MCP 도구 문서: `Assets/MCPForUnity/README.md`
