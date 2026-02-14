# CommonTest

킹덤러쉬 스타일 2D 타워디펜스 프로토타입(Unity) 프로젝트입니다.  
`Common`(공용 프레임워크)와 `Kingdom`(게임 로직)을 분리해서 개발 중입니다.

## 프로젝트 상태
- 씬 흐름: `InitScene -> TitleScene -> WorldMapScene -> GameScene`
- 월드맵/스테이지 선택/저장 연동은 동작 중
- `GameScene`은 전투 루프 고도화 진행 중(플랜 문서 기준 단계적 구현)

## 빠른 실행
1. Unity 2022.3 이상으로 프로젝트를 엽니다.
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
- Unity (2022.3+)
- C#
- uGUI + TextMeshPro
- UniTask (`com.cysharp.unitask`)

## 문서
주요 문서는 `문서/` 폴더에 있습니다.

- `문서/게임씬_구현_플랜.md`
- `문서/월드맵_사이드보더_실행플랜.md`
- `문서/스테이지버튼고도화_코덱스_플랜.md`
- `문서/UIStageNode_이미지프롬프트_나노바나나.md`

## 참고
- 공용 라이브러리 문서: `Assets/Scripts/Common/README.md`
- Unity MCP 도구 문서: `Assets/MCPForUnity/README.md`
