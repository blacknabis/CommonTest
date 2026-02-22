# 오브젝트 클릭 정보 및 HP 게이지 구현 상세 로그 (2026-02-22)

## 1. 개요
선택 시스템(Selection System)과 월드 공간 HP 바(World HP Bar), 그리고 HUD 정보 패널(Selection Info Panel)을 구현하여 사용자 피드백을 강화함.

## 2. 주요 변경 사항

### 인터페이스 및 기반 구조
- `ISelectableTarget.cs`: 선택 가능한 대상에 대한 표준 인터페이스 정의. (이름, HP 비율, 현재/최대 HP, 유닛 타입 등)
- `BaseUnit.cs`: `ISelectableTarget` 기본 구현 및 HP 관리 로직 통합.

### 선택 시스템 (Phase 1)
- `SelectionController.cs`: 마우스 클릭 및 레이캐스트 레이어 필터링을 통한 오브젝트 선택 처리. (싱글톤)
- `SelectionCircleVisual.cs`: 선택된 대상의 발밑에 나타나는 원형 시각 피드백. 유닛 타입별 크기 조절 기능 포함.
- **씬 구성**: `SelectionManager`, `SelectionCircleVisual` 게임 오브젝트 생성 및 컴포넌트 설정.

### 월드 HP 바 시스템 (Phase 2)
- `WorldHpBar.cs`: SpriteRenderer를 사용한 경량 월드 UI HP 바. 체력이 100% 미만일 때만 표시되도록 최적화.
- `WorldHpBarManager.cs`: 오브젝트 풀링을 사용하여 다수의 유닛에 대한 HP 바 생성을 효율적으로 관리.
- **연동**: `SpawnManager`(적), `TowerManager`(병사), `HeroController`(영웅) 스폰 시 자동 추적 등록.

### 선택 정보 패널 UI (Phase 3)
- `SelectionInfoPanel.cs`: HUD 상의 상세 정보 표시 스크립트. 이름과 상세 HP 수치(`Current / Max`) 및 슬라이더 표시.
- **UI 통합**: `GameView.prefab` 하위에 `SelectionInfoPanel` 계층 구조 추가 및 자동 바인딩 로직 구현.

## 3. 테스트 및 검증 결과
- **선택 피드백**: 적/아군/타워/영웅 클릭 시 즉각적인 서클 표시 확인.
- **HP 바 노출**: 데미지 입기 전에는 숨김, 피격 즉시 노출 및 실시간 갱신 확인.
- **정보 패널**: 선택 대상 변경 시 이름 및 HP 수치 동기화 확인.
- **안정성**: 유닛 사망 시 HP 바 및 선택 상태 자동 해제 확인.

## 4. 향후 과제
- 타워 업그레이드/판매 등 선택 시 나타날 추가 기능 버튼 연동 (Tower UI 확장 시).
- 특정 유닛군(예: 보스)에 대한 특수 HP 바 디자인 적용 검토.
