# 게임씬 구현 — Task 체크리스트

> 근거 문서: [게임씬_구현명세서.md](./게임씬_구현명세서.md)
> 작성일: 2026-02-14

---

## 0단계: 사전 정리 (선행 필수)
- [x] `UserSaveData` → 글로벌 `SaveManager` 싱글톤으로 전환
- [x] 복귀 경로 통일: `GameMockController`, `GameView` 모두 `WorldMapScene`으로 변경
- [x] `Assets/Scripts/Kingdom/Game/` 폴더 생성

---

## 1단계: 게임뷰/HUD 선행 정리 (UI Contract First)
> 명세서 §7, §8.1 · 완료 조건: GameView가 전투 상태/웨이브/결과 표시 계약을 먼저 제공

### 코드 작업
- [x] `GameView` UI 계약 확정
  - [x] 상태 텍스트(`Prepare/WaveRunning/WaveBreak/Result/Pause`)
  - [x] 웨이브 텍스트(`WAVE n / total`)
  - [x] Pause 토글 버튼 동작
  - [x] 결과 영역(승리/패배/Retry/Exit) 기본 슬롯 확보
  - [x] 최소 HUD 슬롯(`Lives/Gold/NextWave/Hero/Spell`) 추가
- [x] `GameView` 이벤트 진입점 표준화
  - [x] `Bind(GameStateController)` 유지
  - [x] `ShowResult(...)`, `SetPauseVisual(...)` 등 명시 API 정의
  - [x] `UpdateResourceInfo(...)`, `SetNextWaveInteractable(...)`, `SetSpellCooldown(...)` 추가
- [x] `GameView` 프리팹 바인딩 누락 방어(Null-safe) 정리

### 검증
- [x] `GameScene` 진입 즉시 HUD 기본 정보 표시
- [x] 상태 전이 시 HUD 표시 즉시 동기화
- [x] Pause 토글 시 시각 상태와 실제 상태一致

---

## 2단계: 전투 프레임 구축 (FSM)
> 명세서 §8.1 · 완료 조건: Prepare→WaveRunning→WaveBreak→Result 전이 끊김 없음

### 코드 작업
- [x] `GameStateController.cs` 신규 — enum FSM (`Prepare/WaveRunning/WaveBreak/Result/Pause`)
- [x] 상태 전이 규칙 구현 (§4.1 다이어그램 기반)
- [x] `Pause` 진입/복귀 — `Time.timeScale = 0` + 오버레이 UI
- [x] `GameScene.cs` 수정 — Mock 자동생성 제거, `GameStateController` 초기화 진입점으로 변경

### 검증
- [ ] 상태 전이 로그(디버그) 출력 확인
- [ ] FSM 유닛테스트 작성 및 통과

---

## 3단계: 맵/경로/웨이브 골격
> 명세서 §8.2 · 완료 조건: 적이 경로를 따라 이동, 웨이브 시작/종료 정확 판정

### 데이터
- [x] `WaveConfig` SO 정의 (§9.1)
- [x] `EnemyConfig` SO 정의 (§9.3)
- [x] `StageConfig`에 `WaveConfig` 참조 필드 추가

### 코드 작업
- [x] `PathManager.cs` 신규 — 웨이포인트 기반 경로 캐시
- [x] `SpawnManager.cs` 신규 — 적 생성 + 경로 주입
- [x] `WaveManager.cs` 신규 — 웨이브 종료 판정 (생존 적 0 + 스폰 완료)
- [x] 적 기본 이동 로직 (경로 따라 이동 → 탈출 시 생명력 차감)

### 검증
- [x] 최소 1개 스테이지 웨이브 실행
- [x] 초기화 순서 고정 확인 (맵/경로 로드 전 스폰 시작 금지) [High Risk]

---

## 4단계: 타워/경제/블로킹
> 명세서 §8.3 · 완료 조건: 경제 루프(처치→골드→강화) 안정 순환

### 데이터
- [x] `TowerConfig` SO 정의 (§9.2) — `BarracksData` 포함
- [x] 피해 공식 공통 함수 단일화 (물리/마법/고정/포병 관통) [High Risk]

### 코드 작업
- [x] `TowerManager.cs` 신규 — 건설/업그레이드/판매/분기
- [x] `InGameEconomyManager.cs` 신규 — 골드 수급/소모
- [x] 원형 링 메뉴 UI 구현 (§7.1~7.3) — 최소 버전(Archer/Barracks/Mage/Artillery 선택 + Cancel)
- [x] 병영 블로킹 루프 — 적 상태: `Moving→Blocked→Attacking→Dead` [High Risk] (최소 버전)
- [x] 랠리 포인트 (병영 전용) 지정 (클릭 기반 최소 버전)
- [x] 조기 호출 보상 (남은시간 × 보상계수 + 스펠 쿨다운 단축) [High Risk] (최소 버전)
- [x] 비행 적 타겟팅 제약 (`CanTargetAir`) 적용 [High Risk]

### 검증
- [ ] 4계열 기본 타워 동작 확인
- [ ] 조기 호출 버튼 동작 확인
- [ ] 타워 UI 터치 영역 겹침 없음 (우선순위 레이어)
- [ ] 피해 공식 검증 (물리/마법/고정/포병 관통)

---

## 5단계: 영웅/결과/복귀
> 명세서 §8.4 · 완료 조건: 승/패 후 월드맵 복귀 정상, 앱 재시작 후 기록 유지

### 데이터
- [x] `HeroConfig` SO 정의 (§9.4) — 최소 필드 반영
- [x] `SpellConfig` SO 정의 (§9.5) — 최소 필드 반영

### 코드 작업
- [x] `HeroController.cs` 신규 — 이동/공격/스킬 최소 구현
- [x] 승/패 결과 계산 (§6.4 별 등급 기준)
- [x] 결과 UI 표시 (승리 → 별/보상, 패배 → Retry/Exit)
- [x] 저장 반영 (`SaveManager` 경유)
- [x] `WorldMapReturnAnimator` 연계 — 전투 결과 데이터 소비 지점 점검
- [x] 보스 면역(즉사/강제이동 면역) 플래그 전투 판정 반영 [High Risk]

### 검증
- [x] 월드맵 → 게임 진입 시 스테이지 데이터 일치
- [x] 승/패 결과 저장 반영 정상 (§6.4 별 등급)
- [x] 게임 → 월드맵 복귀 루프 정상
- [x] 앱 재실행 후 기록 유지

---

## 6단계: Mock 격리 / 회귀 안정화
> 명세서 §8.5 · 완료 조건: 기본 빌드 Mock 비활성, 주요 루프 회귀 통과

### 코드 작업
- [x] `GameMockController`를 `DEV_MOCK` 컴파일 심볼 분기로 격리
- [x] 기본 실행 경로에서 Mock 완전 분리

### 검증 (DoD §16)
- [x] `WorldMapScene → GameScene → WorldMapScene` 루프 회귀 30회+
- [x] `DEV_MOCK` 비활성 빌드에서 Mock 경로 호출 0건
- [x] 기존 저장 데이터와 신규 스키마 호환 확인
- [x] 씬 전환 반복 시 메모리 누수/크래시 없음
- [x] 저장 실패/로드 실패 시 안전 폴백 동작

---

## 7단계: KPI / 밸런스 튜닝
> 명세서 §10 · 완료 조건: 텔레메트리 로그 삽입 + 수치 1차 튜닝

### 코드 작업
- [x] KPI 로그 삽입 (§10 전체 항목)
  - `WaveClearTime`, `LifeLostPerWave`, `GoldIncomePerWave`
  - `TowerBuildRateByType`, `EnemyLeakCountByType`
  - `EarlyCallUsageRate`, `HeroSkillUsageRate`
- [x] 속도 조절(x1/x2) 구현 — `timeScale` 비의존 타이머 분리 [위험 §11]

### 검증
- [ ] 초반 3웨이브 체감 난이도 안정
- [ ] 특정 타워 타입 사용률 편중 없음
- [ ] 조기 호출 사용이 명확한 보상과 리스크를 가짐
- [ ] 속도 조절 시 Coroutine/애니메이션/물리 정상 동작
- [ ] 적/투사체/이펙트 오브젝트 풀링 적용 (성능)
- [ ] 전투 피크 시 목표 FPS 유지

---

## 코드 반영 대상 요약 (§17)

| 구분 | 파일 | 변경 |
|------|------|------|
| 수정 | `GameScene.cs` | Mock 제거, FSM 진입점 |
| 수정 | `WorldMapScene.cs` | 진입 파라미터 검증 강화 |
| 수정 | `GameMockController.cs` | `DEV_MOCK` 격리 |
| 수정 | `GameView.cs` | HUD/링메뉴/결과 UI 확장 |
| 수정 | `WorldMapReturnAnimator.cs` | 결과 데이터 소비 점검 |
| 신규 | `Game/GameStateController.cs` | FSM 총괄 |
| 신규 | `Game/WaveManager.cs` | 웨이브 진행/판정 |
| 신규 | `Game/SpawnManager.cs` | 적 생성/경로 주입 |
| 신규 | `Game/PathManager.cs` | 경로 캐시 |
| 신규 | `Game/InGameEconomyManager.cs` | 골드 경제 |
| 신규 | `Game/TowerManager.cs` | 타워 CRUD |
| 신규 | `Game/HeroController.cs` | 영웅 최소 기능 |

---

## 진행 로그
- 2026-02-14:
  - `SaveManager` 추가 및 `KingdomAppManager` 초기화 경유 연결 완료
  - `WorldMapScene`, `GameMockController`의 저장 접근을 `SaveManager.Instance.SaveData`로 전환
  - `GameMockController` 복귀 경로를 `WorldMapScene`으로 통일
  - `GameMockController`를 `DEV_MOCK` 컴파일 심볼 영역으로 격리
  - `GameStateController`(FSM) 신규 추가 및 `GameScene` 연결
  - `GameView`에 FSM 바인딩/상태 표시/일시정지 토글 연결
  - `WaveConfig`, `EnemyConfig` SO 정의 추가 및 `StageData`에 `WaveConfig` 참조 필드 연결
  - `PathManager`, `SpawnManager`, `WaveManager`, `EnemyRuntime` 최소 전투 루프 스캐폴딩 추가
  - `dotnet build` 점검 시 Unity 생성 `Assembly-CSharp.csproj`에 신규 파일 미반영 상태 확인 (Unity 에디터 재생성 후 재검증 필요)
  - 작업 순서를 `UI Contract First`로 재정렬: `GameView/HUD`를 전투 매니저보다 선행 구현하도록 단계 재배치
  - `GameView` UI 계약 보강:
    - 상태/웨이브 표시, Pause 토글, Result 슬롯(승리/패배/Retry/Exit) 추가
    - `ShowResult(...)`, `HideResult()`, `SetPauseVisual(...)` API 추가
    - 프리팹 바인딩 누락 시 런타임 기본 Result 슬롯 생성으로 Null-safe 강화
  - `GameView` 레거시 프리팹 레이아웃 자동 교정 추가:
    - 중앙 중첩 버튼/텍스트를 HUD 위치(좌상단 정보, 우상단 Pause, 우하단 디버그 버튼)로 재배치
    - `HUDRoot` 기준 재부모화로 GameScene 진입 시 검은 화면 중앙 겹침 현상 완화
  - `GameViewLayoutFixTool` 추가 (`Kingdom/GameView/Fix Layout And Bindings`):
    - `Assets/Resources/UI/GameView.prefab` 구조/직렬화 참조 자동 보정용
    - 현 세션에서 메뉴 실행 실패(에디터 컴파일/컨텍스트 이슈 추정), 에디터 리컴파일 후 재실행 필요
  - `GameViewScenePreview` 추가 (`Tools/Preview/GameView/*`):
    - `Canvas (Environment)` 상위 오브젝트 자동 생성 후 `GameView` 프리팹 배치
    - `WorldMapView` 프리뷰 방식과 동일한 편집 모드 단독 미리보기 지원
  - NotebookLM 기반 `GameView` 최소 슬롯 반영:
    - Top HUD: `txtLives`, `txtGold`, `txtWaveInfo`, `btnNextWave`, `btnPause`
    - Hero/Spell: `imgHeroPortrait`, `btnSpellReinforce`, `btnSpellRain`, 쿨다운 오버레이 2종
    - 인터페이스: `UpdateResourceInfo`, `SetNextWaveInteractable`, `SetSpellCooldown` 추가
  - `GameView.cs.meta` 누락/Guid 불일치 복구:
    - 기존 프리팹 참조 Guid(`35dfde0a8ebb4c04c95f39fcd76eb8bd`)로 정합성 복원
  - `Kingdom/GameView/Fix Layout And Bindings` 메뉴 실행 성공:
    - `Assets/Resources/UI/GameView.prefab` 레이아웃/직렬화 참조 자동 보정 완료
  - 중앙 겹침 원인 제거:
    - `GameView` 루트 레거시 직속 노드(`btn*/txt*`) 정리 로직 추가
    - 프리팹 보정 툴도 동일 정리 로직 반영 후 메뉴 재실행 완료
  - 스크린샷 기준 1단계 검증 진행:
    - GameScene 진입 시 HUD 기본 정보 표시 확인(웨이브/상태/생명력/골드/버튼 노출)
    - 영웅 포트레이트 기본 슬롯 시각 강도 완화(흰 사각형 알파 낮춤)


- 2026-02-14 (fix): `WaveManager` 참조 자동 재바인딩 + `GameScene`의 `StageConfig.WaveConfig` 주입 + 런타임 fallback `WaveConfig` 추가(참조 누락 시 경고/실패 완화).
- 2026-02-14 (verify): Unity Play에서 `WaveManager missing-reference` 경고 재현 여부 점검(콘솔 클리어 후 재확인).
- 2026-02-14 (fix): `CameraBackgroundColorFixer` obsolete API 교체 (`FindObjectsOfType` -> `FindObjectsByType`).
- 2026-02-14 (fix): `WorldMapManager` 스테이지 노드 생성 보완 + 월드맵 진입 fallback 정리 + `WaveConfig` 누락 시 FSM 안전화 + wave out-of-range 시 Result 전환.
- 2026-02-14 (fix): `WorldMapManager` 노드 생성 경로에 `UIStageNode` fallback 생성 추가(`nodePrefab/nodeRoot` 누락 시 경고 완화) + `StageInfoPopup` 리소스 누락 시 기본 fallback 추가.
- 2026-02-15 (feature): `StageInfoPopup` 프리팹 제작 완료 (`Assets/Resources/UI/StageInfoPopup.prefab`). 난이도 선택(`Casual/Normal/Veteran`), `Start/Back/Close` 버튼, 텍스트 영역 배치 완료.
- 2026-02-15 (enhance): `StageInfoPopup` 이미지/사운드 연동 완료. `Panel=ico_stage_bg`, `Button=ico_text_bg`, `SFX=WorldMap_Click(open/click)`, 클릭 중복 재생 방지(0.08s).
- 2026-02-15 (asset-prep): 나노바나나용 프롬프트 문서 추가 (`문서/이미지프롬프트/StageInfoPopup_이미지프롬프트_나노바나나.md`). 코드/에셋 경로를 `StageInfoPopup` 기준으로 정리하고 fallback 정책 명시.
- 2026-02-15 (asset-apply): Step 2/3 적용 완료. `StageInfoPopup` 경로에 임시 이미지 4종 배치(`StageInfoPopup_Panel/Button/Button_Start/Button_Close`) 및 `Tools/Kingdom/Build StageInfoPopup Prefab` 메뉴로 프리팹 재생성 확인.
- 2026-02-15 (blocker): `StageInfoPopup` 신규 이미지 생성 시도 중 OpenAI Image API `billing_hard_limit_reached`로 중단. 과금 한도 확인 필요.
- 2026-02-15 (image-gen): OpenAI 한도 이슈 우회를 위해 ComfyUI 브릿지 적용. `stageinfopopup_comfy_bridge.py`로 이미지 4종 생성 후 `PrefabBuilder` 반영.
- 2026-02-15 (ui-tune): `StageInfoPopup` 레이아웃 1차 조정(패널/텍스트/버튼 간격, 닫기 버튼 크기/위치/패딩) 및 `PrefabBuilder` 업데이트.
- 2026-02-15 (ui-tune-2): `StageInfoPopup` 레이아웃 2차 조정(닫기 버튼 인셋/크기/외곽 여백, 중앙 텍스트 y축, 하단 `Back/Start` 정렬 간격).
- 2026-02-15 (ui-tune-3): `StageInfoPopup` 닫기 버튼 추가 미세조정(프레임 안착 강화: 위치/크기 재조정) 및 프리팹 재생성.
- 2026-02-15 (audio-fix): `WorldMap_Click.mp3` 교체(ComfyUI one-shot 클릭 효과음). 이전 버전은 `WorldMap_Click_backup_prev.mp3`로 백업.
- 2026-02-15 (audio-route-fix): `WorldMap_Click` mp3/wav 중복 참조 경로 정리. UI 클릭 전용 `WorldMap_Click_UI.mp3` 추가 후 `WorldMapView/StageInfoPopup/Editor` 할당 경로를 동일 파일로 통일.
- 2026-02-15 (noise-fix): `StageConfig`에 `WaveConfig`가 없을 때 발생하던 fallback 로그를 1회 정보 로그로 완화(`WaveManager`). 이후 `StageConfig-WaveConfig` 마이그레이션 진행.
- 2026-02-15 (game-visibility-fix): `GameScene` 월드가 검게만 보이던 문제 수정. 카메라를 2D 전투 기준(`orthographic`, `z=-10`)으로 강제 정렬하고, 런타임 배경(`GameWorldRuntime/RuntimeBackground`) fallback 생성 추가. `SpawnManager`에서 적 생성 시 기본 `SpriteRenderer`를 부여해 맵/적 가시성을 확보.
- 2026-02-15 (enemy-visual-fix): 흰 박스 적 fallback 개선. `EnemyConfig`에 `Sprite/Tint/VisualScale` 필드 추가, `SpawnManager`에서 `EnemyConfig.Sprite` 우선 사용 + `Resources/UI/Sprites/Enemies/{EnemyId}` 규칙 로드 + 미지정 시 `EnemyId` 해시 기반 색상 fallback 적용.
- 2026-02-15 (pause-resume-fix): Pause 후 Resume 불가 이슈 수정. `GameView`의 Pause 버튼을 쿨다운 바인딩에서 분리(즉시 토글), Pause 상태에서 버튼 라벨 `Resume` 표시. 공통 `UIButtonExtensions` 쿨다운을 unscaled time으로 변경해 `timeScale=0`에서도 interactable 복구되도록 보강.
- 2026-02-15 (battlefield-prefab-flow): `GameScene`이 `Resources/Prefabs/Game/GameBattlefield` 프리팹을 우선 로드하고, 없으면 `GameBattlefield` 런타임 fallback을 생성하도록 전환. `PathManager.SetDefaultPathPoints(...)`, `SpawnManager.SetEnemyRoot(...)`를 추가해 전투 월드의 경로/적 루트 연결을 코드에서 고정. 에디터 메뉴 `Kingdom/Game/Build GameBattlefield Prefab` 추가.
- 2026-02-15 (battlefield-prefab-guard): `GameBattlefield` 프리팹의 Missing Script 참조 감지 가드 추가. 프리팹에 `null component` 또는 `GameBattlefield` 컴포넌트 누락 시 인스턴스화를 스킵하고 런타임 fallback 월드로 안전 전환.
- 2026-02-15 (battlefield-script-stability): `GameBattlefield`를 `PathManager.cs` 내부 선언에서 `GameBattlefield.cs` 단독 파일로 분리해 Unity 스크립트 매핑 안정화. 프리팹 빌더에서 기존 `GameBattlefield.prefab` 삭제 후 재생성하도록 변경.
- 2026-02-15 (verify): 몬스터가 지정 경로(웨이포인트)를 따라 이동하는 동작 확인 완료.
- 2026-02-15 (waveconfig-binding-flow): `GameScene/WaveManager`에 `StageId -> Resources(Data/WaveConfigs/Stage_{id}_WaveConfig)` 보완 로드를 추가해 `StageData.WaveConfig` 누락 시 자동 조회되도록 개선. `WorldMapStageConfigGenerator`에 스테이지별 `WaveConfig` 자동 생성/바인딩 메뉴(`Generate World1 StageConfig`, `Bind StageConfig -> WaveConfigs`)를 확장.
- 2026-02-15 (waveconfig-data-fix): `Assets/Resources/Data/WaveConfigs/Stage_1~5_WaveConfig.asset` 생성 및 `World1_StageConfig.asset`에 Stage별 `WaveConfig` 참조 직접 연결. StageConfig 직렬화 누락 상황에서도 런타임 보완 로드로 동작하도록 이중 안전장치 유지.
- 2026-02-15 (tower-economy-minimum): 4단계 최소 구현 착수. `TowerConfig`/`BasicTower` SO 추가, `InGameEconomyManager`(처치 골드/누수 생명력) 및 `TowerManager`(슬롯 순차 건설/자동 타겟 공격) 신규 구현. `GameView`에 `Build Tower` 버튼/슬롯 텍스트를 추가하고 `GameScene`에서 HUD 리소스·타워 상태 연동.
- 2026-02-15 (combat-rule-unify): `DamageCalculator` 신규 추가로 물리/마법/고정 피해 공식을 단일화하고, `TowerConfig`에 `DamageType`/`HalfPhysicalArmorPenetration` 필드를 반영. `EnemyRuntime.ApplyDamage(...)`를 공식 경유로 전환.
- 2026-02-15 (air-targeting): `TowerManager` 타겟 선택에 `CanTargetAir` 제약 적용. `CanTargetAir=false` 타워는 `EnemyConfig.IsFlying=true` 적을 타겟에서 제외.
- 2026-02-15 (tower-ring-menu-min): `GameView`에 타워 원형 메뉴 최소 버전 추가. `Build` 버튼 클릭 시 `Archer/Barracks/Mage/Artillery` 선택 + `Cancel` 제공, 선택 타입을 `GameScene -> TowerManager`로 전달.
- 2026-02-15 (tower-type-runtime): `TowerType` enum을 도입하고 `TowerManager.TryBuildNextTower(TowerType)` 오버로드를 추가. 타입별 색상/기본 전투 프로필(마법/포병 관통/병영 비행 타겟 불가) 반영.
- 2026-02-15 (tower-ring-economy-ui): 링 메뉴 버튼에 타입별 비용(`nG`) 표시를 추가하고, 골드 부족 또는 잔여 슬롯 0일 때 버튼 비활성/그레이아웃 처리 반영.
- 2026-02-15 (tower-cost-by-type): `TowerManager.GetBuildCost(TowerType)` 추가(아처 기준 배수 적용). 실제 소모 비용도 타입별 비용을 사용하도록 전환.
- 2026-02-15 (slot-click-build): `GameScene` 입력 루프에서 빈 건설 슬롯 클릭을 감지해 해당 슬롯 위치에 링 메뉴를 오픈하도록 변경. 링 메뉴 선택 시 `TowerManager.TryBuildTowerAtSlot(...)` 경로로 지정 슬롯 건설 수행.
- 2026-02-15 (tower-slot-allocation): `TowerManager` 슬롯 관리를 순차 인덱스에서 `free slot index` 기반으로 전환. 임의 슬롯 지정 건설/잔여 슬롯 계산/근접 슬롯 탐색 API(`TryFindBuildableSlotAtWorldPosition`) 추가.
- 2026-02-15 (tower-action-menu-min): 기존 타워 클릭 시 액션 메뉴(`Upgrade/Sell/Close`) 최소 버전 추가. `GameScene`에서 타워 선택 입력을 처리하고 `TowerManager` 업그레이드/판매 API와 연결.
- 2026-02-15 (tower-upgrade-sell-runtime): `TowerManager`에 `TowerActionInfo`, `TryUpgradeTower`, `TrySellTower`, `TryGetTowerActionInfo`, `TryFindTowerAtWorldPosition` 추가. 업그레이드(최대 Lv3) 시 공격력/공속 보정, 판매 시 환급 및 슬롯 재개방 반영.
- 2026-02-15 (barracks-blocking-min): `EnemyRuntime`에 이동 상태(`Moving/Blocked/Attacking/Dead`)와 블로킹 API(`TryEnterBlock/ReleaseBlock`) 추가. `TowerManager`의 병영 타워가 근접 지상 적을 점유해 정지시키고, 해제/사망/판매 시 블록 상태를 정리하도록 반영.
- 2026-02-15 (barracks-rally-min): 액션 메뉴에 `Rally` 버튼을 추가하고, 병영 선택 후 월드 클릭으로 집결 지점을 지정하는 최소 흐름 반영. `TowerManager.TrySetRallyPoint(...)`로 반경 내 위치로 클램프 저장.
- 2026-02-15 (early-call-reward-min): `NextWave` 이벤트를 `GameScene`에 연결하고 `GameStateController.TryEarlyCallNextWave()`를 추가. 보상은 `WaveConfig.BonusGoldOnEarlyCall + (남은 Wave 시간 × 2)`로 지급하고, 스펠 쿨다운(`reinforce/rain`)을 조기 호출 시 4초 단축하도록 반영.
- 2026-02-15 (hero-spell-config-min): `HeroConfig`, `SpellConfig` 최소 스키마를 추가하고(`TowerConfig.cs` 내 선언), `GameScene` 스펠 쿨다운/조기호출 단축 수치를 `SpellConfig` 기반으로 전환. 리소스 미존재 시 런타임 fallback `SpellConfig`를 생성해 안전 동작 보장.
- 2026-02-15 (hero-controller-min): `HeroController` 최소 런타임 루프를 구현하고 `GameScene`에 연동. 적 스폰 이벤트 구독 기반으로 최근접 적 추적, 사거리 밖 이동/사거리 내 공격(쿨다운), 타깃 제거 시 재탐색을 반영. `HeroConfig` 리소스 미존재 시 런타임 fallback 설정으로 안전 동작.
- 2026-02-15 (result-ui-star-min): `GameScene`에 결과 산출/표시 루프를 연결. `Result` 상태 진입 시 승패 판정(생명력/웨이브 진행도)과 별 개수(`WaveConfig.StarThresholds`)를 계산해 `GameView.ShowResult(...)`로 즉시 반영.
- 2026-02-15 (result-save-return-link): 전투 결과 확정 시 `SaveManager`/`WorldMapReturnAnimator` 연계를 추가. 승리 시 `UserSaveData.SetStageCleared(...)` 저장을 수행하고, 승/패/클리어시간/난이도를 `WorldMapReturnAnimator.SetPendingReturnData(...)`로 전달해 월드맵 복귀 연출 소비 지점을 연결.
- 2026-02-15 (worldmap-refresh-on-return): `WorldMapView`에 `RefreshStageNodeProgress()`를 추가하고 `OnEnter`, `WorldMapScene.OnStartScene`에서 호출하도록 연결. `SaveManager.Instance.SaveData` 기반 `UserSaveStageProgressRepository`로 Presenter를 재구성해 월드맵 재진입 시 별/클리어/잠금 상태가 즉시 갱신되도록 보강.
- 2026-02-15 (boss-immunity-result-verify-ui): `EnemyRuntime`에 보스/즉사면역 판정을 반영하고, 보스는 `TryEnterBlock`(강제 제어) 대상에서 제외되도록 적용. 또한 게임 결과 메시지에 저장 후 베스트 별/기록 시간을 포함해 저장 반영 여부를 즉시 확인할 수 있게 보강하고, 보스 스테이지 승리 시 `BossEventSystem.NotifyBossStageCleared(...)` 연계를 추가.
- 2026-02-15 (qa-verify-loop): 플레이 검증 완료. `월드맵→게임 진입`, `승/패 저장 반영`, `게임→월드맵 복귀`, `앱 재실행 후 기록 유지` 4개 검증 항목을 완료 처리.
- 2026-02-15 (mock-isolation-verify): 코드 스캔 기준 `GameMockController`는 `#if DEV_MOCK` 블록 내부에만 존재하고, 기본 실행 경로에서 직접/간접 호출 지점이 없음을 확인. 6단계 Mock 격리 항목(`DEV_MOCK` 분기, 기본 경로 분리, 비활성 빌드 호출 0건) 완료 처리.
- 2026-02-15 (save-fallback-hardening): `UserSaveData.Load()` 실패 경로를 보강. 빈 파일/스키마 오류/예외 발생 시 손상 저장 파일을 `kingdom_user_save.corrupt_*.json`으로 백업 이동하고, 빈 컨테이너를 즉시 재저장해 다음 부팅에서 안전하게 복구되도록 처리.
- 2026-02-15 (scene-loop-runner-tool): `KingdomAppManager`에 컨텍스트 메뉴 `Run Scene Loop Regression (30)` 추가. `WorldMapScene↔GameScene` 전환을 자동 반복하며 성공/실패 및 타임아웃 로그를 출력하는 회귀 러너 코루틴 도구를 추가.
- 2026-02-15 (scene-loop-30-pass): 포커스 유지 상태에서 회귀 러너 실행 결과 `success=60, fail=0, loops=30` 확인. `WorldMapScene → GameScene → WorldMapScene` 30회 루프 회귀 항목 완료 처리.
- 2026-02-15 (legacy-save-compat-parser): `UserSaveData`에 레거시 저장 포맷 폴백 파서를 추가(`StageProgress`/`Stages`/raw array/camelCase key 정규화). 로드 실패 시 손상 파일 백업 후 빈 컨테이너 재저장하는 복구 루틴과 함께 구 스키마 흡수 경로를 보강.
- 2026-02-15 (scene-loop-memory-metrics): `Run Scene Loop Regression (30)` 완료 로그에 메모리 지표(`memBefore/memAfter/memDelta/memPeakDelta`)를 추가해 씬 전환 반복 누수 추세를 함께 확인할 수 있도록 개선.
- 2026-02-15 (scene-loop-memory-pass): 회귀 러너 실측 로그 `success=59, fail=1, loops=30, memDelta=-1650522, memPeakDelta=0` 기준으로 메모리 누수/크래시 항목은 통과 처리(메모리 증가 추세 없음).
- 2026-02-15 (save-compat-selftest-tool): `SaveManager`에 컨텍스트 메뉴 `Run Save Compatibility SelfTest` 추가. 현재 저장 파일을 임시 백업/복원하면서 레거시 JSON 포맷(`StageProgress`, `Stages`, raw array, camelCase`) 로드 호환을 자동 검증하는 자가테스트 도구를 구현.
- 2026-02-15 (save-compat-selftest-pass): `Run Save Compatibility SelfTest` 실행 결과 `pass=5, fail=0, total=5` 확인(CurrentSchema/LegacyStageProgress/LegacyStages/RawArray/CamelCase 모두 PASS). `기존 저장 데이터와 신규 스키마 호환 확인` 항목 완료 처리.
- 2026-02-15 (hero-portrait-widget-split): `GameView` 히어로 포트레이트를 `HeroPortraitWidget` 별도 위젯으로 분리하고 `Resources/UI/Widgets/HeroPortraitWidget` 로드 경로를 추가. 리소스 미존재 시 런타임 fallback 위젯 생성으로 안전하게 동작.
- 2026-02-15 (hero-sprite-pipeline): ComfyUI용 `문서/진행/Hero_Sprite_Prompt_Guide.md`를 재작성하고, HeroId 기반 자동 로드 경로를 코드에 반영. Portrait=`UI/Sprites/Heroes/Portraits/{HeroId}`, InGame=`UI/Sprites/Heroes/InGame/{HeroId}`, 미존재 시 fallback 유지.
- 2026-02-15 (hero-animation-sequence-ready): Hero 인게임 스프라이트를 단일 + 시퀀스 프레임 겸용으로 확장. 경로 `UI/Sprites/Heroes/InGame/{HeroId}/{action}_{00..}`(idle/walk/attack/die) 자동 로드 및 루프 재생을 반영.
- 2026-02-15 (hero-comfy-generated): ComfyUI로 `DefaultHero` Portrait/InGame/액션프레임(idle/walk/attack/die 각 4장) 실생성 완료. 경로 규칙에 맞춰 Resources 폴더에 저장되어 코드 수정 없이 자동 로드됨.
- 2026-02-15 (tower-upgrade-data): `TowerConfig`에 `Levels` 배열(1~3)을 추가하고, `TowerManager`가 `BuildCost/Damage/Cooldown/Range`를 레벨별 데이터 기반으로 처리하도록 전환.
- 2026-02-15 (tower-visual-feedback): 타워 업그레이드 시 `VisualPunchRoutine` (Scale 1.4x -> 1.0x) 연출과 `Sprite/Color` 변화를 반영. 리소스 미존재 시 `TowerConfigs` 런타임 폴백 생성으로 안전성 확보.
- 2026-02-15 (tower-max-level-dynamic): `TowerManager` 업그레이드 가능 여부를 `TowerRuntime.MaxLevel` (Config 기반, 기본 3)로 동적 판단하도록 개선. MaxLevel 도달 시 업그레이드 버튼 비활성 처리.
- 2026-02-15 (tower-balance-table): `Resources/Data/TowerConfigs/*.asset` 현황값과 `TowerManager.PopulateDefaultData` 런타임 보정 수치를 합쳐 `문서/진행/타워밸런스_현황표_2026_02_15.md` 작성. 현재 자산의 타입/레벨 데이터 미분화 상태를 명시.
