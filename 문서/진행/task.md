# Kingdom 작업 Task (정리본)

> 기준 문서: `문서/완료/2026_02_16/월드맵_업그레이드_영웅관리소_플랜_2026_02_16.md`
> 마지막 갱신: 2026-02-19 (적 유닛 구현 시작)

## 현재 메인 트랙: 적 유닛 구현 (Enemy Implementation)
> 기준 문서: `문서/분석/적_유닛_상세_분석.md`
> 구현 계획: `문서/진행/적_유닛_구현_계획_2026_02_19.md`

### Phase 1. 데이터 구조 및 P0 메커니즘
- [ ] `DamageCalculator` 분리 및 Armor/MagicResist 공식 개선
- [ ] `EnemyConfig` 스키마 확장 (Min/Max Dmg, AtkSpd, Bounty 등)
- [ ] `EnemyRuntime` P0 로직 (Physics Layer, Flying Block Ignore)

### Phase 2. 적 행동 로직 (Behaviors)
- [ ] 공격 상태(Attacking) 구현 (대기 -> 타격)
- [ ] 회피(Dodge) 이펙트 및 확률 로직
- [ ] 재생(Regen) 및 자폭(Death Burst) 로직

### Phase 3. 데이터 에셋 (18종)
- [ ] 적 유닛 SO 생성 (Tier 1~5, Boss)
- [ ] 밸런스 수치 입력 (NotebookLM 기준)

### Phase 4. 웨이브 검증
- [ ] Placeholder 프리팹 제작 (Color Variant)
- [ ] 웨이브 템플릿(A/B/C/D) 구성 및 테스트

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
- [ ] 웨이브 시작 알림 배너 및 SFX
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
- [ ] 프레임 크기 정규화(가변 크기 대응)
- [ ] Pivot 기준 정렬(Bottom-Center 기본)
- [ ] `manifest.json` 출력(경고/옵션 기록)

### 문서 운영 세트
- [x] 실행 로그 파일 생성/누적 (`문서/진행/작업상세로그_2026_02_18.md`)
- [x] 단계 완료 시 계획서/로그/task 3종 동기화

### 검증(사용자 진행)
- [ ] Step 1 수동 검증: 프리셋 Save/Load/Delete
- [ ] Step 1 수동 검증: 입력값 오류 다이얼로그
- [ ] Step 2 수동 검증: SmartSlice 품질(노이즈/누락/과검출)
