# 마감정리: AISpriteProcessor 스마트슬라이스 애니메이션 연동 (2026-02-21)

## 목표
- `AISpriteProcessor` SmartSlice 출력(`multi_*`)을 게임 런타임에서 액션(`idle/walk/attack/die`)으로 재생 가능하게 연결한다.
- 기존 분리 파일(`idle_*`, `attack_*`, `die_*`) 방식과의 호환을 유지한다.

## 반영 내용
1. 런타임 액션 분해 로더 추가
- 파일: `Assets/Scripts/Kingdom/Game/SpawnManager.cs`
- 내용:
  - `RuntimeSpriteResourcePath`가 단일 멀티 시트일 때 스프라이트 이름 토큰으로 액션 분해
  - alias 지원: `walk/move/run`, `attack/atk`, `die/death/dead`
  - 프레임 순서 정렬(숫자 suffix 기반)

2. 분리 파일 회귀 방지
- 파일: `Assets/Scripts/Kingdom/Game/SpawnManager.cs`
- 내용:
  - 액션 토큰이 2종 이상 검출될 때만 multi 분해로 인정
  - 단일 액션 파일(`idle_*` 등)은 기존 후보 탐색 경로 유지

3. 게임 시작 전 검증 보강
- 파일: `Assets/Scripts/Kingdom/App/GameScene.cs`
- 내용:
  - `RequiredEnemyActions` 검증 시 multi 시트 내부 액션 토큰 검사 허용
  - 누락 로그에 multi 파싱 사유(reason) 포함

4. 검증 시나리오 고정
- 파일: `Assets/Scripts/Kingdom/KingdomAppManager.cs`
- 내용:
  - Combat Integration Smoke Regression 실행 시 Stage 컨텍스트를 `Stage 1 / Normal`로 고정

## 검증 결과
- `Combat integration smoke regression: success=1, fail=0, scenarios=1`
- Stage 1 진입 후 Wave 진행/적 스폰/처치 로그 확인
- Goblin(`Sprites/Enemies/multi_Goblin_Processed`) 전투 동작 확인
- 기존 분리 파일 경로 회귀 방지 로직 적용 완료

## 산출물
- 코드:
  - `Assets/Scripts/Kingdom/Game/SpawnManager.cs`
  - `Assets/Scripts/Kingdom/App/GameScene.cs`
  - `Assets/Scripts/Kingdom/KingdomAppManager.cs`
- 문서:
  - `문서/진행/AISpriteProcessor_스마트슬라이스_애니메이션연동_실행계획_2026_02_21.md`
  - `문서/진행/task.md`
  - `문서/완료/2026_02_21/마감정리_AISpriteProcessor_스마트슬라이스_애니메이션연동_2026_02_21.md`

## 후속 권장
1. 필요 시 2차 확장으로 `Animator/AnimationClip` 자동 생성 트랙을 분리 추진
2. EnemyConfig별 샘플 2~3종(분리형/멀티형 혼합) 회귀 러너를 별도 메뉴로 추가
