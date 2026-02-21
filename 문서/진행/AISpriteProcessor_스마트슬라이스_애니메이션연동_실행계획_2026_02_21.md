# AISpriteProcessor 스마트슬라이스 -> 캐릭터 애니메이션 연동 실행계획 (2026-02-21)

## 1. 목표
- `AISpriteProcessor`에서 SmartSlice로 한 장의 시트(기존 형태)에서 프레임을 추출한다.
- 추출 결과를 액션(`idle/walk/attack/die`) 단위로 안정적으로 사용 가능하게 만든다.
- 게임 런타임에서 해당 액션 프레임을 실제 전투 재생에 사용한다.

## 2. 현재 상태 요약
- SmartSlice 4행 액션 분리 옵션은 이미 존재하며, 스프라이트 이름도 액션 라벨 기반으로 생성 가능.
- 런타임(`SpawnManager`, `EnemyRuntime`)은 현재 `Animator`가 아니라 `Sprite[]` 프레임 배열 직접 재생 방식.
- 현재 적 로더는 리소스 경로 후보(`idle_*`, `attack_*`, `die_*`)를 찾는 방식이라 `multi_*` 단일 시트는 액션 탐색에서 누락될 수 있음.

## 3. 핵심 결정
1. 1차는 저위험 경로로 진행
- 런타임 재생 구조(`Sprite[]`)는 유지하고, `multi_*` 단일 시트에서도 액션별 프레임을 분해해 공급한다.

2. 2차는 선택 확장
- 필요 시 `Animator + AnimationClip` 생성/바인딩으로 전환한다.
- 단, 현재 구조 안정화 전에 전면 전환하면 회귀 범위가 커지므로 2차로 분리한다.

## 4. 목표 아키텍처 (1차)
1. 출력 규칙
- SmartSlice 4행 액션 분리 모드 출력 기본 리소스 경로: `Sprites/Enemies/multi_<EnemyId>_Processed`
- 스프라이트 서브 에셋 이름 규칙: `<base>_idle_00`, `<base>_walk_00`, `<base>_attack_00`, `<base>_die_00`

2. 런타임 로딩 규칙
- `EnemyConfig.RuntimeSpriteResourcePath`가 `multi_*` 시트를 가리켜도 동작해야 함.
- 로더가 `Resources.LoadAll<Sprite>(runtimePath)`로 전체 프레임을 가져온 후, 이름 접미사(`_idle_`, `_walk_`, `_attack_`, `_die_`)로 액션별 분해.
- 분해 실패 시 기존 후보 탐색 로직으로 폴백.

3. 검증 규칙
- `GameScene` 사전 검증에서 `multi_*` 단일 경로도 유효한 액션 소스로 인정.
- `idle/attack/die` 필수 조건은 유지하되, 소스 형태는 다중 파일/단일 시트 모두 허용.

## 5. 구현 단계
### Phase 0. 문서/정책 동기화
- 본 문서 + `task.md` 트랙 추가

### Phase 1. 런타임 액션 분해 로더 추가
- 대상: `Assets/Scripts/Kingdom/Game/SpawnManager.cs`
- 작업:
  - 다중 스프라이트 로드 후 이름 기반 액션 분해 함수 추가
  - `ResolveEnemyAnimationClips`에서 `runtimePath`가 단일 시트일 때 우선 분해 사용
  - 기존 `TryResolveEnemyActionFrames`는 폴백으로 유지

### Phase 2. 게임 시작 전 검증 보강
- 대상: `Assets/Scripts/Kingdom/App/GameScene.cs`
- 작업:
  - `TryResolveEnemyActionSpriteBinding` 또는 그 상위 경로에서 `multi_*` 시트 액션 포함 여부 검사 추가
  - 누락 로그에 "multi sheet parsed" 여부를 표시해 원인 추적 용이화

### Phase 3. AISpriteProcessor 출력/가이드 정리
- 대상: `Assets/Scripts/Kingdom/Editor/AISpriteProcessor.cs`, 가이드 문서
- 작업:
  - BaseUnit(Enemy) 기준 `RuntimeSpriteResourcePath` 권장 패턴 안내 고정
  - SmartSlice 4행 모드에서 액션 그룹 무시 동작 안내 문구 강화
  - SmartSlice 프리뷰/처리 성능 회귀 확인

### Phase 4. 회귀 테스트
- Stage 1 Goblin 고정으로 재검증
- 검증 항목:
  - 씬 시작 중단 없음
  - Goblin `idle/walk/attack/die` 모두 재생 확인
  - 공격/사망 프레임 튐 현상 최소화

## 6. 수용 기준 (DoD)
1. `EnemyConfig.RuntimeSpriteResourcePath = Sprites/Enemies/multi_Goblin_Processed`로도 전투 시작 가능.
2. `GameScene` 누락 검증에서 `attack/die` 미존재 오류가 발생하지 않음.
3. 전투 중 고블린이 `idle/walk/attack/die`를 상태에 맞게 재생.
4. 기존 분리 파일 방식(`idle_*`, `attack_*`, `die_*`)도 회귀 없이 유지.

## 7. 위험요소 및 대응
1. 스프라이트 이름 불일치
- 대응: 분해 시 액션 alias(`attack/atk`, `die/death/dead`) 허용.

2. 프레임 정렬 꼬임
- 대응: 동일 액션 내 숫자 suffix 기준 정렬(`_00`, `_01` ...).

3. 에디터 성능 저하
- 대응: 기존 SmartSlice preview cache 유지, 처리 시에만 전체 재계산.

## 8. 2차 확장(선택): Animator 전환
- 조건: 1차 안정화 완료 후.
- 내용:
  - 액션별 `AnimationClip` 자동 생성
  - `AnimatorController` 자동 구성(Idle/Move/Attack/Die)
  - `EnemyRuntime` 상태를 Animator 파라미터로 구동
- 비고: 현재는 범위 외(리스크 관리 목적).

## 9. 즉시 착수 항목
1. `SpawnManager`에 multi 시트 액션 분해 로직 추가
2. `GameScene` 검증 로직에 multi 시트 허용 추가
3. Stage 1 Goblin 실기동 재검증
