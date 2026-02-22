# 선택 시스템 및 HP 게이지 구현 결과 리포트

## 구현 완료 사항
### 1. 선택 시스템 (Phase 1)
- `ISelectableTarget` 인터페이스: 적, 아군, 타워, 영웅에 공통 적용.
- `SelectionController`: 마우스 클릭 및 Raycast를 통한 오브젝트 선택 처리.
- `SelectionCircleVisual`: 선택된 대상 발밑에 시각적 피드백 제공 (유닛 타입별 스케일 조절).

### 2. 월드 체력 게이지 (Phase 2)
- `WorldHpBar`: 오브젝트를 따라다니는 스케일 기반 HP 바.
- `WorldHpBarManager`: 오브젝트 풀링을 통한 성능 최적화 관리.
- **연동 완료**: `EnemyRuntime`, `BarracksSoldierRuntime`, `HeroController`가 스폰될 때 자동으로 HP 바가 추적되도록 연동.
- **UX 최적화**: 체력이 100%일 때는 보이지 않다가, 데미지를 입는 순간부터 표시됨.

## 테스트 방법
1. 유닛(적/아군)을 클릭하여 선택 서클이 나타나는지 확인.
2. 유닛이 데미지를 입어 HP가 깎일 때 머리 위에 HP 바가 나타나는지 확인.
3. 유닛이 죽으면 HP 바와 선택 서클이 자동으로 사라지는지 확인.

## 다음 작업
- Phase 3. 선택 정보 패널 UI (이름, HP 텍스트 등) 구현
