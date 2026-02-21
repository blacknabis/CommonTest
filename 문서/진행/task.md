# Kingdom 작업 Task (정리본)

> 기준 문서: `문서/완료/2026_02_16/월드맵_업그레이드_영웅관리소_플랜_2026_02_16.md`
> 마지막 갱신: 2026-02-19 (타워·영웅 구현 진행)

## 현재 메인 트랙: AISpriteProcessor 스마트슬라이스 애니메이션 연동
> 기준 문서: `문서/진행/AISpriteProcessor_스마트슬라이스_애니메이션연동_실행계획_2026_02_21.md`
> 마감 문서: `문서/완료/2026_02_21/마감정리_AISpriteProcessor_스마트슬라이스_애니메이션연동_2026_02_21.md`

### Phase 0. 문서/정책 동기화
- [x] 실행계획 문서 생성
- [x] `task.md` 트랙 동기화

### Phase 1. 런타임 액션 분해 로더
- [x] `SpawnManager`에서 `multi_*` 단일 시트 이름 기반 액션 분해(`idle/walk/attack/die`)
- [x] 기존 액션 경로 후보 탐색은 폴백으로 유지

### Phase 2. 게임 시작 전 검증 보강
- [x] `GameScene` 누락 검증에서 `multi_*` 단일 시트 액션 포함 허용
- [x] 누락 로그에 multi sheet 파싱 여부 표시

### Phase 3. 실기동 회귀
- [x] Combat Integration Smoke Regression 재실행 성공 (`success=1, fail=0, scenarios=1`)
- [x] Stage 1 Goblin 기준 전투 진입 성공 (Stage context 고정 후 Wave 진행/전투 로그 확인)
- [x] Goblin `idle/walk/attack/die` 상태 재생 확인 (multi 시트 액션 분해 로딩 경로 검증 + 전투 중 spawn/kill 동작 확인)
- [x] 기존 분리 파일 방식 회귀 없음 확인 (multi 분해를 액션 2종 이상 감지 시에만 적용, 단일 액션 파일은 기존 후보 탐색 유지)

### Phase Close. 스마트슬라이스 애니메이션 연동 마감
- [x] 코드/검증/문서 정리 완료
- [x] 마감 문서 이관 완료 (`문서/완료/2026_02_21`)
- [x] Enemy Animator 전환 완료 (`RuntimeAnimatorControllerPath` 기준)
- [x] 적 애니메이션 전부 적용 완료 (사용자 검증)
- [x] Config 리소스 경로 표준화 및 물리 이동 완료 (`Assets/Resources/Kingdom/Configs/*`)

## 현재 메인 트랙: 적 유닛 구현 (Enemy Implementation)
> 기준 문서: `문서/분석/적_유닛_상세_분석.md`
> 구현 계획: `문서/진행/적_유닛_구현_계획_2026_02_19.md`

### Phase 1. 데이터 구조 및 P0 메커니즘
- [x] `DamageCalculator` 분리 및 Armor/MagicResist 공식 개선
- [x] `EnemyConfig` 스키마 확장 (Min/Max Dmg, AtkSpd, Bounty 등)
- [x] `EnemyRuntime` P0 로직 (Physics Layer, Flying Block Ignore)

### Phase 2. 적 행동 로직 (Behaviors)
- [x] 공격 상태(Attacking) 구현 (대기 -> 타격)
- [x] 회피(Dodge) 이펙트 및 확률 로직
- [x] 재생(Regen) 및 자폭(Death Burst) 로직

### Phase 3. 데이터 에셋 (18종)
- [x] 적 유닛 SO 생성 (Tier 1~5, Boss)
- [x] 밸런스 수치 입력 (NotebookLM 기준)

### Phase 4. 웨이브 검증
- [x] Placeholder 프리팹 제작 (Color Variant) - 런타임 Placeholder(`SpawnManager` Sprite/Tint) 적용
- [x] 웨이브 템플릿(A/B/C/D) 구성 및 테스트

## 다음 메인 트랙: 타워·영웅 구현 (Tower & Hero Implementation)
> 기준 문서: `문서/분석/타워_유닛_상세_분석.md`, `문서/분석/영웅_유닛_상세_분석.md`
> 구현 계획: `문서/진행/타워_영웅_구현_계획_2026_02_19.md`

### Phase 1. 타워 전투 규칙 정리 (P0)
- [x] 타입별 핵심 규칙/타겟팅/상성 정리
- [x] 1~3티어 성장 곡선 표준화

### Phase 2. 상위 타워 스킬 훅 (P1)
- [x] 4티어 대표 스킬 훅(계열별 최소 1개) 구현
- [x] 쿨다운/중복 발동 안정화

### Phase 3. 영웅 공통 시스템 (P0~P1)
- [x] 영웅 공통 런타임(체력/피격/사망/복귀) 정리
- [x] 영웅 스킬 슬롯/쿨다운 데이터 정책 정리

### Phase 4. 영웅 역할군 능력 (P1)
- [x] 탱커/딜러/소환·보조 역할군 대표 능력 구현
- [x] 역할군 시나리오 테스트 (`Hero role smoke regression: success=12, fail=0, testedHeroes=3`)

### Phase 5. 통합 검증 (P2)
- [x] 정식 데이터 에셋화 (`TowerConfig` 4종/`HeroConfig` 3종/`StageConfig -> WaveConfig` 링크 정리)
- [x] 타워·영웅·적 상성 통합 QA (`Combat integration smoke regression: success=1, fail=0, scenario=TankAndSpank`)
- [x] 밸런스 조정 백로그 문서화 (`문서/진행/밸런스_조정_백로그_2026_02_19.md`)

### Extension Track. 배럭 병사 근접교전 확장 (P0~P1)
> 상세 플랜: `문서/진행/배럭_병사_근접교전_확장_계획_2026_02_19.md`

- [x] 분리 상세 플랜 문서 생성 및 링크 연결
- [x] `BaseUnit` 공통 부모 도입 및 `EnemyRuntime` 리팩토링 (Phase A)
- [x] `BarracksData` 전투 파라미터 확장 및 호환성 보정 (Phase A)
- [x] 병사 런타임 상태(`Idle/Blocking/Dead/Respawning`) 및 `BaseUnit` 상속 구현 (Phase B)
- [x] 병사-적 상호 공격 연동 (`EnemyRuntime.AttackPerformed` 활용) (Phase C)
- [x] 병사 사망/재소환 루프 구현 (Phase B/C)
- [x] 배럭 근접교전 스모크 러너 추가 (`Tools/Kingdom/Run Barracks Melee Smoke Regression`)
- [x] 랠리 포인트 재배치 안정화 및 경량 QA (`Barracks melee smoke regression: success=10, fail=0`)

## 완료 트랙: 환경설정 오디오 옵션창 (마감)
> 기준 문서: `문서/완료/2026_02_19/환경설정_오디오옵션창_작업계획_2026_02_19.md`

### Phase 1. 서비스/저장 기반
- [x] `AudioSettingsKeys` 추가 (`Kingdom.Audio.*` 키/기본값)
- [x] `AudioSettingsService` 추가 (Load/Save/Apply + legacy 키 호환)
- [x] 앱 시작 시 오디오 설정 로드/적용 연결 (`KingdomAppManager`)

### Phase 2. 옵션 UI/월드맵 연결
- [x] `AudioOptionsPopup` 스크립트 추가 (슬라이더/토글/퍼센트 라벨/샘플 SFX)
- [x] `WorldMapView` 오디오 옵션 팝업 오픈 경로 추가
- [x] 프리팹 미존재 시 런타임 폴백 팝업 생성 지원
- [x] `WorldMapView` 레거시 프리팹 버튼 슬롯(`btnAudioOptions`) 런타임 자동 복구 로직 추가

### Phase 3. 검증/정리
- [x] Unity PlayMode 자동 회귀 검증 통과 (`Audio settings regression: success=11, fail=0`)
- [x] 오디오 옵션 프리팹 빌더 추가 (`Tools/Kingdom/Build AudioOptionsPopup Prefab`)
- [x] 프리팹 기반 UI 연결 점검 (`UI/WorldMap/AudioOptionsPopup` 생성/로드 확인)
- [x] 오디오 옵션 팝업 `UILayer_Popup` 라우팅 확인
- [x] 오디오 설정 저장 디바운스(200ms) + Pause/Quit 강제 저장 훅 반영
- [x] `UserSaveData` 직렬화 타이밍 `persistentDataPath` 접근 예외 방어 반영
- [x] 오디오 설정 자동 회귀 러너 추가 (`KingdomRegressionMenu/ContextMenu`)
- [x] `task.md` / 작업로그 동기화
- [x] 오디오 옵션창 트랙 마감 처리 (추가 수동 QA 미진행, 사용자 승인 기준)

## 운영 원칙
- [x] 완료된 상세 문서는 `문서/완료/2026_02_16`로 이관 완료
- [x] `task.md`는 현재 진행/다음 작업만 유지 (토큰 절감)

## 프로젝트 관리
- [x] `PROJECT_RULES.md` 생성 (AI 툴 동기화용)

## 현재 메인 트랙: 월드맵 메타 UI

### Phase A. 월드맵 라우팅/모달 인프라
- [x] `WorldMapView` 업그레이드 버튼 -> 실제 팝업 오픈 연결
- [x] `WorldMapView` 영웅 관리소 버튼 -> 실제 팝업 오픈 연결
- [x] 공통 오버레이 컨테이너(`OverlayContainer`) 구성
- [x] 닫기 동선 통일(`X`, 배경 터치, BackKey)

### Phase B. UpgradesPopup 연결
- [x] `UpgradesPopup.prefab` 생성
- [x] `SkillTreeUI` 직렬화 바인딩 완료
- [x] 6개 카테고리 탭 구성(아처/병영/메이지/포병/강화/유성)
- [x] 별 부족 시 비활성/문구 처리 검증
- [x] 구매 후 즉시 저장 및 UI 갱신 검증

### Phase C. HeroRoomPopup 연결
- [x] `HeroRoomPopup.prefab` 생성
- [x] `HeroSelectionUI` 직렬화 바인딩 완료
- [x] 영웅 선택/파티 배치/해제 저장 연결
- [x] 월드맵 재진입 시 선택 상태 복원 확인 (전투 루프와 연계)

### Phase D. 인게임 연동 최소 구현
- [x] `SelectedHeroId` 기반 GameScene 영웅 로드 연결
- [x] 업그레이드 수치의 전투 로직 반영 훅 연결(최소: 공격/사거리/쿨다운)
- [x] 미적용 항목 경고 로그 정리(의도된 로그만 남김)

## 최근 완료(요약) Phase E. 검증 및 수정
- [x] 검증 로그 보강(선택 영웅 로드, HeroRoom 선택 복원 로그)
- [x] GameView 빌드 링 메뉴 UX 보강(슬롯 없음 피드백/화면 경계 클램프)
- [x] 월드맵 메타 팝업 리그레션 러너 추가(ContextMenu)
- [x] 메타 저장/선택 영웅 반영 리그레션 러너 추가(ContextMenu)
- [x] 월드맵 -> 업그레이드 -> 월드맵 루프 정상
- [x] 월드맵 -> 영웅관리소 -> 월드맵 루프 정상
- [x] 앱 재실행 후 업그레이드/영웅 선택 데이터 유지
- [x] 월드맵 -> 게임 진입 시 선택 영웅 반영
- [x] 신규 콘솔 에러/치명 경고 0건

## 현재 메인 트랙: 웨이브 준비시간 고도화
> 기준 문서(이관): `문서/완료/2026_02_18/전투_웨이브_준비시간_고도화_작업계획_2026_02_18.md`
> 작업 로그(이관): `문서/완료/2026_02_18/작업상세로그_2026_02_18.md`
> 마감 문서: `문서/완료/2026_02_18/마감정리_웨이브준비시간_2026_02_18.md`

### Phase 1. 준비시간 확보 및 UI (즉시 수정)
- [x] `GameStateController`에 `WaveReady` 상태 추가 및 타이머 구현
- [x] `WaveManager` 스폰 트리거 분리 (상태 진입 != 스폰 시작)
- [x] `GameView` 카운트다운 텍스트 연동 (`txtWaveTimer` 미존재 시 `txtStateInfo` 폴백)
- [x] `GameView.prefab`은 `txtWaveTimer` 미배치 상태로 유지 (폴백 경로 우선)
- [x] 웨이브 종료 즉시 상태 전환(`TryCompleteCurrentWave`)으로 Victory 지연 완화

### Phase 2. 조기 호출 보상 (구조 개선)
- [x] 조기 호출 시 골드 보상 계산 로직 (`BonusGold` = `WaveReady` 남은 시간 * N)
- [x] 조기 호출 시 쿨타임 감소 로직 적용 (영웅/지원군, 남은 준비시간 비례 보정)
- [x] 중복 호출 방지 및 예외 처리 (`WaveReady` 상태에서만 허용)

### Phase 3. UX/연출 강화 (고도화)
- [x] 웨이브 시작 알림 배너 및 SFX
- [ ] (Future) 스폰 지점 몬스터 아이콘 표시 및 상호작용

### Phase Close. 웨이브 준비시간 고도화 마감
- [x] 코드/검증/문서 3종 정리 완료
- [x] 완료 문서 폴더(`문서/완료/2026_02_18`) 이관 완료

### Phase 0. 설계/문서 동기화 (완료)
- [x] 전투 웨이브 준비시간 고도화 플랜 확장(상태전이/보상/QA/롤백)
- [x] TimeScale 정책(Scaled Time) 및 ADR 결정 로그 반영
- [x] `task.md` / 플랜 / 작업상세로그 3종 문서 동기화

## 완료된 작업 (Recent)
- [x] 웨이브 시작 시 2초 지연 (몬스터 즉시 출현 방지)
- [x] Early Call 로직 수정 (Break/Prepare 상태에서 호출 가능)
- [x] 월드맵 메타 UI 및 데이터 연동 완료 (Phase A~E)
- [x] 웨이브 준비시간 고도화 문서 실행형 스펙으로 보강 (2026-02-18)
- [x] WaveReady 기반 상태 전이/스폰/NextWave/카운트다운 1차 코드 반영 (2026-02-18)
- [x] ComfyUI 워크플로우/스크립트 `CommonTest`로 이관 (2026-02-19)

## 현재 서브 트랙: AI Sprite Processor 고도화 (Solo Friendly v4)
- [x] `AISpriteProcessor.cs` 기본 구현 (ChromaKey, AutoDivide)
- [x] Interactive Editor Window 변환 (Preview, CustomGrid)
- [x] 1인 개발형 계획서로 재정의 완료 (`문서/진행/AISpriteProcessor_작업계획_및_인수인계서_2026_02_18.md`)

### Step 1. 프리셋/검증 (코드 기준 완료)
- [x] `AISpriteProcessConfig` 직렬화 구조 도입
- [x] 프리셋 Save/Load/Delete UI
- [x] 입력값 유효성 검사(음수/0-size/padding 과다)

### Step 2. Smart Slice (코드 기준 완료)
- [x] Island Detection(BFS/DFS)
- [x] 노이즈 필터(`alphaThreshold`, `minIslandPixels`)
- [x] Rect 정렬(좌상단 -> 우하단)
- [x] SmartSlice 프리뷰 시각화(감지 Rect)
- [x] SmartSlice 결과 SpriteMeta 자동 생성
- [x] 프리뷰 전처리 적용 옵션(제거 색상 반영)

### Step 2.5 배경제거 모드 고도화 (코드 기준 완료)
- [x] 배경제거 방식 선택 지원
  - [x] 방식 A: 제거색상 + 허용오차(Chroma Key)
  - [x] 방식 B: RGB 채널 조건식(이상/이하/무시)
- [x] RGB 조건식 UI 추가 (채널별 조건/기준값)
- [x] 마젠타 조건 프리셋 버튼 (`R>=150`, `G<=50`, `B>=150`)
- [x] 프리셋 저장/로드에 RGB 조건식 값 연동
- [x] 프리뷰 캐시 해시에 RGB 조건식 값 반영

### Step 3. 단일 소스 액션 분류/출력 정책
- [x] 단일 Source Texture 기반 처리로 단순화 (다중 입력 경로 제거)
- [x] 액션 그룹 수동 지정 옵션 추가 (`Unknown/Idle/Walk/Attack/Die`)
- [x] 파일명 기반 자동 분류는 fallback으로 유지
- [x] 미분류 `Unknown` 그룹 처리

### Step 4. 정규화/Manifest
- [x] 프레임 크기 정규화(가변 크기 대응)
- [x] Pivot 기준 정렬(Bottom-Center 기본)
- [x] `manifest.json` 출력(경고/옵션 기록)

### 문서 운영 세트
- [x] 실행 로그 파일 생성/누적 (`문서/진행/작업상세로그_2026_02_18.md`)
- [x] 단계 완료 시 계획서/로그/task 3종 동기화

### 검증(사용자 진행)
- [x] Step 4 회귀 검증: `walk_Change_the_background_202602182330_Processed` rect out-of-bounds 경고 미재현, `manifest.json` 필드 정합 확인
- [ ] Step 1 수동 검증: 프리셋 Save/Load/Delete
- [ ] Step 1 수동 검증: 입력값 오류 다이얼로그
- [ ] Step 2 수동 검증: SmartSlice 품질(노이즈/누락/과검출)

## 신규 서브 트랙: ComfyUI 에셋 파이프라인
> 기준 문서: `문서/진행/ComfyUI_자동화_트랙_관리_2026_02_20.md`

- [x] ComfyUI 자동화 트랙 관리 문서 분리 및 체크리스트 동기화 (2026-02-20)

### Phase 1. 타일 생성기 (Tile Generator)
- [x] `workflow_tile_generation.json` 제작 (SDXL 기반, Seamless 프롬프트)
- [x] `TileGenerator` 에디터 툴 제작 (Grass/Dirt/Water 메뉴)
- [x] 프롬프트 튜닝 (평면 2D 게임 타일 텍스처 스타일 고도화, 부정 프롬프트 동적 주입)
- [x] 텍스처/배경 전용 생성 모델 시트 적용 (`sd_xl_base_1.0.safetensors` 전환)
- [x] Unity Editor 내 생성 테스트 및 Seamless 확인

### Phase 2. I2V 캐릭터 스프라이트 파이프라인 (Hero/Enemy)
- [x] Base Image 생성 툴 제작 (우측면 단일 포즈)
- [x] I2V (Image-to-Video) 연동 워크플로우 제작
- [x] Unity 개별 프레임 병합(Stitching) 및 스프라이트 시트화 모듈 구현

## 신규 서브 트랙: AI Sprite Processor BaseUnit/TowerBase 고도화
> 기준 문서: `문서/진행/AISpriteProcessor_BaseUnit_TowerBase_고도화_계획_2026_02_20.md`

### 계획 수립 및 구조 설계
- [x] 고도화 계획 및 요구사항 구체화 (`AISpriteProcessor_BaseUnit_TowerBase_고도화_계획_2026_02_20.md`)

### 구현 단계 (구현 전 단계별 세분화)
- [x] Phase A: 탭 UI 골격 관리 및 컨텍스트 분리
- [x] Phase B: BaseUnit 탭 + HeroConfig 연결 구현 (ActionGroup 적용)
- [x] Phase C: BaseUnit 탭 + EnemyConfig 연결 구현
- [x] Phase D: BaseUnit 탭 + BarracksSoldierConfig 연결 구현
- [x] Phase E: TowerBase 탭 + TowerConfig 연결 구현 (레벨별 연동)
- [x] Phase F: 데이터 마이그레이션 및 하위 호환성 (Fallback) 대응
- [ ] Phase G: 검증/로그 통합 UX 및 예외 처리 정책 (회귀 메뉴 모달 제거 및 로그 기준 운영 반영 완료, Validate 결과 패널 정리는 남음)
- [ ] Phase H: 실제 샘플(에셋) 적용 테스트
