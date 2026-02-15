# CommonTest

킹덤러쉬 스타일 2D 타워디펜스 프로토타입(Unity) 프로젝트입니다.  
`Common`(공용 프레임워크)와 `Kingdom`(게임 로직)을 분리해서 개발 중입니다.

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
- 작업 상세 로그(일자별): `문서/진행/작업상세로그_2026_02_15.md`
- 개발일지: `문서/개발일지/DevLog_2026_02_15_1.md`
- 이미지 프롬프트(나노바나나): `문서/이미지프롬프트/StageInfoPopup_이미지프롬프트_나노바나나.md`

## 참고
- 공용 라이브러리 문서: `Assets/Scripts/Common/README.md`
- Unity MCP 도구 문서: `Assets/MCPForUnity/README.md`
