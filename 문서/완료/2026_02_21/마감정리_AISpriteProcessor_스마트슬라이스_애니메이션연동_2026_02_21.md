# 마감정리: AISpriteProcessor 스마트슬라이스 애니메이션 연동 (2026-02-21)

## 목표
- `AISpriteProcessor` SmartSlice 출력에서 `AnimationClip/AnimatorController`를 자동 생성한다.
- 게임 런타임은 Enemy를 Animator 기반으로 재생한다.
- EnemyConfig 전 종에 대해 Animator 적용을 완료한다.

## 반영 내용
1. Animator 기반 Enemy 런타임 전환
- 파일: `Assets/Scripts/Kingdom/Game/SpawnManager.cs`
- 내용:
  - Enemy 스폰 시 `RuntimeAnimatorControllerPath` 우선 로드
  - 미설정 시 관례 경로 `Animations/Enemies/<EnemyId>/<EnemyId>` 로드
  - Animator 미해결 시 스폰 중단 및 에러 출력

2. 게임 시작 전 검증 보강(Animator 기준)
- 파일: `Assets/Scripts/Kingdom/App/GameScene.cs`
- 내용:
  - Enemy 검증을 Sprite 경로 기반에서 Animator 해석 기반으로 전환
  - 누락 힌트를 `RuntimeAnimatorControllerPath`/관례 경로 기준으로 갱신

3. AISpriteProcessor 자동 생성/바인딩 강화
- 파일: `Assets/Scripts/Kingdom/Editor/AISpriteProcessor.cs`
- 내용:
  - `Generate Animator + Clips` / `Run All` 파이프라인 제공
  - Enemy 대상 생성 시 `RuntimeAnimatorControllerPath` 자동 반영
  - Enemy 대상은 더 이상 `RuntimeSpriteResourcePath`를 사용하지 않음

4. Config 경로 표준화 + 물리 마이그레이션
- 파일:
  - `Assets/Scripts/Kingdom/Game/ConfigResourcePaths.cs`
  - `Assets/Resources/Kingdom/Configs/*`
- 내용:
  - 표준 경로 `Kingdom/Configs/<Category>` 도입
  - 기존 `Data/*`, `Kingdom/Enemies/Config/*` 에셋을 표준 경로로 이동
  - 런타임/에디터 로더는 표준 경로 우선 + legacy fallback 유지

## 검증 결과
- Unity compile 에러 0건
- Stage 1 진입 후 Enemy 스폰 시 Animator 로드 로그 확인
  - 예: `[SpawnManager] Enemy animator resolve. enemyId=Goblin, configuredPath=Animations/Enemies/Goblin/Goblin, loaded=Goblin`
- 적 애니메이션 전 종 적용 완료(사용자 검증 완료)

## 산출물
- 코드:
  - `Assets/Scripts/Kingdom/Game/ConfigResourcePaths.cs`
  - `Assets/Scripts/Kingdom/Game/SpawnManager.cs`
  - `Assets/Scripts/Kingdom/App/GameScene.cs`
  - `Assets/Scripts/Kingdom/Editor/AISpriteProcessor.cs`
  - `Assets/Scripts/Kingdom/KingdomAppManager.cs`
- 문서:
  - `README.md`
  - `문서/완료/2026_02_21/리소스_컨피그_경로_통일_마이그레이션_2026_02_21.md`
  - `문서/진행/AISpriteProcessor_스마트슬라이스_애니메이션연동_실행계획_2026_02_21.md`
  - `문서/진행/task.md`
  - `문서/완료/2026_02_21/마감정리_AISpriteProcessor_스마트슬라이스_애니메이션연동_2026_02_21.md`

## 후속 권장
1. Enemy Animator 회귀 러너(전 EnemyConfig 순회)를 별도 메뉴로 추가
2. legacy fallback 제거 시점 확정 후 `ConfigResourcePaths` 정리
