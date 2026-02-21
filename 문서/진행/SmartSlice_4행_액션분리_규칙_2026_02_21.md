# SmartSlice 4행 액션 분리 규칙 (2026-02-21)

## 목적
한 장의 스프라이트 시트에서 `SmartSlice`로 프레임을 감지한 뒤, 행 기준으로 `idle/walk/attack/die`를 자동 분리해 사용할 수 있도록 한다.

## 적용 대상
- 파일: `Assets/Scripts/Kingdom/Editor/AISpriteProcessor.cs`
- 모드: `SlicingMode.SmartSlice`
- 옵션: `4행 액션 분리 사용`

## 동작 규칙
1. SmartSlice로 알파 영역(Rect)을 감지한다.
2. 감지 Rect를 Y 중심값 기준으로 상단 -> 하단 정렬한다.
3. 설정된 행 수(기본 4)로 분할해 행 버킷을 만든다.
4. 각 행 내부 프레임은 X 오름차순으로 정렬한다.
5. 행 라벨링:
   - 4행일 때: `idle`, `walk`, `attack`, `die`
   - 4행이 아닐 때: `row00`, `row01`, ...
6. 최종 스프라이트 이름은 `baseName_{actionLabel}_{frameIndex}` 형식으로 생성한다.

## 실패/폴백 규칙
아래 조건이면 액션 분리 모드를 포기하고 기존 SmartSlice 단순 인덱스 이름(`baseName_00`)으로 폴백한다.
- 감지 프레임 수 < 행 수
- 수직 범위가 너무 작아 행 분할이 불가능
- 분할 결과에서 비어 있는 행이 발생

폴백 시 `warnings`에 사유를 기록한다.

## 프리셋/매니페스트 반영
다음 설정을 프리셋/매니페스트에 저장한다.
- `smartSliceSplitActionsByRows`
- `smartSliceActionRowCount`

## 사용 가이드
- 한 장 시트에서 액션 분리를 원하면:
  - `Mode = SmartSlice`
  - `4행 액션 분리 사용 = ON`
  - `액션 행 수 = 4`
- 입력 시트는 가능한 한 4행 구조(Idle/Walk/Attack/Die)가 유지되도록 생성한다.
