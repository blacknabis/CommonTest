# 마감정리_AISpriteProcessor_단일소스정책_2026_02_18

## 1) 작업 배경
- AI Sprite Processor에서 다중 입력 기반 분류 흐름은 실제 사용성 측면에서 복잡도가 높았음.
- 파일명 규칙만으로 액션 구분이 어려운 데이터셋이 많아, 수동 지정 옵션 우선 정책으로 전환 필요.

## 2) 최종 적용 정책
- 처리 입력은 단일 `Source Texture` 1개 기준으로 고정.
- 액션 분류는 `수동 지정` 우선:
  - `Unknown / Idle / Walk / Attack / Die`
- 파일명 기반 자동 분류는 fallback 경로로만 유지.
- 출력 파일명은 `<group>_<원본명>_Processed.png` 정책 유지.

## 3) 주요 코드 반영
- 대상: `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`
- 반영 요약:
  1. 다중 입력 관련 UI/리스트/배치 처리 경로 제거
  2. 단일 처리 경로(`ProcessSpriteSheet -> ProcessSingleTexture`) 유지
  3. `액션 그룹 수동 지정` 토글 + Action Group 드롭다운 추가
  4. 자동 분류 fallback + Unknown 처리 유지
  5. Preview 줌아웃 시 Grid/영역 오프셋 정렬 보정

## 4) 문서 동기화
- 진행 문서 업데이트 완료:
  - `문서/진행/task.md`
  - `문서/진행/작업상세로그_2026_02_18.md`
  - `문서/진행/AISpriteProcessor_작업계획_및_인수인계서_2026_02_18.md`
  - `README.md`
  - `Assets/Scripts/Common/README.md`

## 5) 수동 검증 결과
- 검증 항목:
  1. 줌아웃 오프셋 정합
  2. 단일 Source + 액션 수동 지정 동작
  3. 기존 기능 회귀(SmartSlice, 배경제거 2모드, 입력 검증)
- 결과: 전체 통과(✅)

## 6) 커밋 이력
- Common 저장소 커밋 완료:
  - `feat(editor): simplify AI sprite flow to single source + manual action group`

## 7) 후속 권장
- 수동 Action Group 값을 프리셋 Save/Load와 연동해 반복 작업 시간을 추가 절감할 것.
