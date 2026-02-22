# 오브젝트 클릭정보 + 체력게이지 구현 계획

전투 중 오브젝트(적/영웅/타워/배럭 병사) 클릭 시 정보를 시각화하고, 실시간 체력 상태를 머리 위 HP 바(World HP Bar)로 제공하여 전투 피드백을 강화합니다.

## Proposed Changes

### [Selection System]

#### [NEW] [SelectionController.cs](file:///c:/study/unity/CommonTest/Assets/Scripts/Kingdom/Game/SelectionController.cs)
- 마우스 클릭을 통한 대상 선택(`Raycast`) 및 선택 해제 관리.
- `EventSystem.current.IsPointerOverGameObject()`를 사용하여 UI 클릭 시 월드 선택 방지.
- 선택 시 발밑에 `SelectionCircle` 표시 관리.

#### [NEW] [ISelectableTarget.cs](file:///c:/study/unity/CommonTest/Assets/Scripts/Kingdom/Game/ISelectableTarget.cs)
- 선택 가능한 인터페이스 정의 (이름, 타입, HP 정보 등 제공).
- `EnemyRuntime`, `BarracksSoldierRuntime`, `HeroController`, `TowerRuntime` 등이 구현.

### [UI Components]

#### [NEW] [SelectionInfoPanel.cs](file:///c:/study/unity/CommonTest/Assets/Scripts/Kingdom/UI/SelectionInfoPanel.cs)
- 하단(또는 측면)에 선택된 대상의 상세 능력치 표시.
- 유닛 타입별 맞춤형 스탯 노출 (적은 아머/이속, 병사는 공격력/아머 등).
- **Juice**: Scale 애니메이션을 통한 Snappy한 팝업 효과 적용.

#### [NEW] [WorldHpBarManager.cs](file:///c:/study/unity/CommonTest/Assets/Scripts/Kingdom/UI/WorldHpBarManager.cs)
- 월드 공간의 유닛 머리 위에 HP Bar를 일괄 관리.
- 체력이 100% 미만일 때만 선택적으로 노출하여 화면 복잡도 완화.
- 오브젝트 풀링을 통한 최적화.

#### [NEW] [WorldHpBarView.cs](file:///c:/study/unity/CommonTest/Assets/Scripts/Kingdom/UI/WorldHpBarView.cs)
- 개별 HP Bar UI 컴포넌트.

## Verification Plan

### Automated Tests
- `KingdomRegressionMenu` 내에 클릭 선택 및 HP바 노출 리그레션 시나리오 추가.
- 적 생성 -> 공격(HP 감소) -> HP바 노출 확인 -> 사망 -> HP바 제거 확인 자동화.

### Manual Verification
1. **Selection Feedback**: 게임 플레이 중 적, 영웅, 타워를 클릭하여 발밑에 선택 서클이 정상적으로 나타나는지 확인.
2. **Info Panel**: 각 오브젝트 타입별로 올바른 정보(공격력, 아머 등)가 패널에 표시되는지 확인.
3. **HP Bar Policy**: 풀체력인 적은 HP바가 안 보이다가, 타워 공격을 맞은 순간 HP바가 나타나는지 확인.
4. **Performance**: 다수의 적(50+)이 화면에 존재할 때 HP바 표시로 인한 프레임 드랍 여부 확인.
