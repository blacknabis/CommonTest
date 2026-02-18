# 월드맵 구현 작업 목록 (Task_WorldMap)

> **상태**: 진행 중 (Core Logic 구현 완료, GameScene 연동 필요)
> **참조 문서**: `Design_WorldMap_Final.md`, `Design_WorldMap_Codex_Final.md`
> **최종 수정**: 2026-02-14

---

## Phase 1: 핵심 시스템 (Foundation)
- [x] **데이터 에셋 (Data Assets)**
    - [x] `StageConfig` ScriptableObject 정의 (ID, Name, WorldID, Icons, Unlock Condition)
    - [x] 1월드 9개 스테이지 더미 데이터(Dummy Data) 생성 (`WorldMapStageConfigGenerator`)
- [x] **월드맵 씬 (WorldMap Scene)**
    - [x] 배경 리소스(Parallax Background) 배치 (레이어 3개: 근경, 경로, 원경)
        - [x] `Resources/UI/Sprites/WorldMap/WorldMap_Background.png` 생성 및 반영
        - [x] `ParallaxBackground.cs` 구현 완료
    - [x] `Orthographic Camera` 설정 및 `CameraClamp` (맵 경계 제한) 스크립트 구현
    - [ ] `ScrollRect` 관성(Inertia) 튜닝 및 터치 입력 처리 (`WorldMapUI`) -> *Editor 설정을 통해 값 튜닝 필요*
- [x] **노드 시스템 (Node System)**
    - [x] `StageNode` 프리팹 및 스크립트 (`StageNode.cs`, 상태 패턴 적용)
    - [x] `StagePathRenderer` 구현 (LineRenderer 기반 경로 표시 및 점선 애니메이션)

## Phase 2: UI 및 인터랙션 (Interaction)
- [x] **월드맵 기본 UI 정렬/스킨 보정**
    - [x] `WorldMapView.prefab` 버튼 중앙 몰림 현상 보정 (앵커/크기 재배치)
    - [x] `WorldMapView.cs`에서 배경 자동 로드/폴백/런타임 보강 처리
- [x] **팝업 시스템 (Popup System)**
    - [x] `Common.UI.BasePopup` 상속받아 `StageInfoPopup` 구현
    - [x] `StageConfig` 데이터 바인딩 (Title, Description, Icon) 및 UI 레이아웃
- [ ] **씬 전환 (Scene Transition)**
    - [x] `GameManager` 및 `SceneSwitcher` 연동: '전투 시작' 버튼 클릭 시 `GameScene` 로드 (`WorldMapScene.HandleStartStage`)
    - [ ] 씬 전환 효과 (Fade In/Out) 적용 (`Common` 라이브러리 `ScreenTransition` 확인 필요)
- [x] **저장 시스템 (Save System)**
    - [x] `UserSaveData` 클래스 정의 (TotalStars, ClearedStages 목록)
    - [x] `GameManager` 대신 `UserSaveData` 자체 저장/불러오기 로직 구현
    - [x] 앱 시작 시 데이터 로드 및 월드맵 초기화

## Phase 3: 게임플레이 통합 (Gameplay Integration)
- [x] **클리어 복귀 (Clear Return)**
    - [x] `GameScene`에 임시 클리어/실패 버튼 추가 (테스트용) - `GameMockController`
    - [x] `GameResultPopup`에서 '월드맵으로' 버튼 연결 (Mock 컨트롤러로 대체)
    - [x] 월드맵 씬 재진입 시 클리어 정보 갱신 (`WorldMapReturnAnimator`)
- [x] **경로 연출 (Path Animation)**
    - [x] 스테이지 간 연결 선(`LineRenderer`) 구현 (`StagePathRenderer`)
    - [x] 클리어 시 다음 스테이지로 이어지는 점선 애니메이션 추가 (`StagePathRenderer`)
- [x] **별 시스템 (Star System)**
    - [x] 스테이지 클리어 등급(별 1~3개) 평가 로직 구현 (`UserSaveData`에 저장)
    - [x] `UserSaveData`에 별 획득량 누적 저장 및 UI 갱신

## Phase 4: 확장 콘텐츠 (Expansion)
- [ ] **난이도 모드 (Difficulty Modes)**
    - [x] Heroic/Iron 모드 선택 UI 추가 (`StageInfoPopup`에 버튼 있음)
    - [ ] 모드별 진입 조건 검사 로직 (데이터 바인딩 시 처리 필요)
- [ ] **업그레이드 & 영웅 (Upgrades & Heroes)**
    - [x] 별 소비 스킬 트리 UI (`UpgradePopup`) 진입 시 "준비 중" 토스트 메시지 연결
    - [x] 영웅 선택 슬롯 UI (`HeroSelectPopup`) 진입 시 "준비 중" 토스트 메시지 연결
- [x] **업적 & 빌런 (Achievements & Villains)**
    - [x] 업적 시스템 데이터 구조 정의 및 달성 체크 로직 (`AchievementSystem`, `AchievementConfig`)
    - [ ] 특정 스테이지 클리어 시 빌런 등장 연출 (Dialog/Cutscene)

---

## 작업 계획 (Plan)
1.  **배선 및 연결 (Wiring)**
    - `WorldMapScene` 로드 시 `StageConfig` 자동 로드 확인.
    - `StageInfoPopup` 프리팹 생성 및 `Resources/UI/Popups/` 배치.
2.  **GameScene 연동 (Mocking)**
    - `GameScene`에 임시 UI(Win/Lose) 추가.
    - 승리 시 `UserSaveData` 갱신 후 `TitleScene` -> `WorldMapScene` 복귀 흐름 검증.
3.  **마무리 폴리싱**
    - `ScrollRect` 감도 조절.
    - 미구현 버튼(Hero, Upgrade)에 "준비 중" 메세지 연결.

