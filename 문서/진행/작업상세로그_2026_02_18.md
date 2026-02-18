# 작업상세로그_2026_02_18 (진행)

## 2026-02-18 AI Sprite Processor 트랙 재정의
- 기준 문서: `문서/진행/AISpriteProcessor_작업계획_및_인수인계서_2026_02_18.md`
- 결론: 1인 개발 환경과 가변 이미지 입력(크기/비율/액션 분산 파일) 제약을 반영하여 범위를 재설계함.

### 오늘 반영 사항
1. 계획서 v4(1인 개발형)로 갱신
   - 런타임 자동화보다 툴 자동화 우선으로 방향 고정
   - 멀티 입력/액션 자동 분류/프레임 정규화/Manifest 출력까지를 이번 사이클 범위로 정의
2. task.md 동기화
   - Step 1~4 순차 로드맵으로 재구성
   - 현재 진행 상태를 Step 1(프리셋/검증)로 지정
3. 문서 운영 세트 확정
   - 마스터 계획서 + task + 작업상세로그 3종 운영

### 현재 상태
- 진행 중: Step 1. 프리셋/검증
- 구현 완료(코드 반영):
  - `AISpriteProcessConfig` 직렬화 구조 도입
  - 프리셋 Save/Load/Delete UI 추가
  - 입력값 유효성 검사(음수/0-size/padding 과다) 추가
- 남은 작업:
  - Unity Editor 수동 검증(프리셋 저장/로드/삭제)
  - 검증 실패 다이얼로그 동작 확인

### 코드 변경 메모 (Step 1)
- 대상: `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`
- 추가 사항:
  1. Preset 섹션 UI 추가 (`DrawPresetSection`)
  2. 프리셋 파일 관리 로직 추가
     - `SaveCurrentAsPreset`
     - `LoadSelectedPreset`
     - `DeleteSelectedPreset`
     - `RefreshPresetList`
  3. 입력 검증 로직 추가
     - `TryValidateInputs`
     - 처리 버튼/처리 함수 진입 전 검증 적용
  4. 프리셋 저장 경로
     - `Assets/Editor/AISpritePresets/*.json`

### 코드 변경 메모 (Step 2)
- 대상: `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`
- 추가 사항:
  1. SmartSlice 모드 추가 (`SlicingMode.SmartSlice`)
  2. SmartSlice 파라미터 UI 추가
     - `Alpha Threshold`
     - `Min Island Pixels`
     - `Outer Padding`
  3. 알파 기반 아일랜드 감지 알고리즘 추가 (`DetectIslands`)
  4. SmartSlice 프리뷰 반영
     - Preview 화면에서 감지 Rect를 오렌지 라인으로 시각화
  5. SmartSlice 결과로 SpriteMetaData 자동 생성
     - 이름 규칙: `{파일명}_{index:00}`

### 상태 정리 (Step 2 완료 기준)
- Step 1: 코드 반영 완료
  - 프리셋 Save/Load/Delete
  - 입력 검증(음수/0-size/padding 과다)
- Step 2: 코드 반영 완료
  - SmartSlice 모드/옵션(Alpha Threshold, Min Pixels, Outer Padding)
  - Island Detection + Rect 정렬
  - SmartSlice Preview 시각화
  - SmartSlice SpriteMeta 자동 생성
  - 프리뷰 전처리 적용(제거 색상/허용오차 반영)

### 코드 변경 메모 (Step 2.5 배경제거 모드 고도화)
- 대상: `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`
- 추가 사항:
  1. 배경제거 2가지 방식 선택 지원
     - 방식 A: 제거색상 + 허용오차(기존 Chroma Key)
     - 방식 B: RGB 채널 조건식 필터
  2. RGB 채널별 조건식 UI 추가
     - 채널별 `이상(>=) / 이하(<=) / 무시` 선택
     - 채널별 기준값(0~255)
  3. 기본 조건 프리셋 버튼 추가
     - `R>=150`, `G<=50`, `B>=150`
  4. 프리셋 직렬화 연동
     - RGB 조건식 설정값 Save/Load 포함
  5. 프리뷰 캐시 해시 연동
     - RGB 조건식 변경 시 프리뷰 즉시 갱신

### 상태 정리 (Step 2.5 완료 기준)
- Step 2.5: 코드 반영 완료
  - 배경제거 모드 선택(키컬러/오차 vs RGB 조건식)
  - RGB 조건식 필터 적용 로직 반영
  - 프리셋 저장/로드 연동
  - 프리뷰 갱신 해시 연동

### 코드 변경 메모 (Step 3 재정의 - 단일 소스 기반)
- 대상: `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`
- 변경 사항:
  1. 다중 입력 경로 제거
     - `다중 입력 사용`, 입력 개수, 다중 Source 슬롯 UI 제거
     - 배치 처리 함수/리스트/검증 분기 제거
  2. 단일 Source Texture 기반으로 고정
     - 처리 진입은 `ProcessSpriteSheet -> ProcessSingleTexture` 단일 경로 유지
  3. 액션 분류는 옵션화
     - `액션 그룹 수동 지정` 토글 추가
     - 수동 모드: `Unknown/Idle/Walk/Attack/Die` 직접 선택
     - 자동 모드: 파일명 키워드 fallback (`idle`, `walk|run`, `attack|atk`, `die|death|dead`)
     - 미검출은 `unknown` 처리
  4. 출력 파일명 정책
     - `<group>_<원본명>_Processed.png`

### 코드 변경 메모 (추가 - 프리뷰 줌아웃 오프셋 보정)
- 대상: `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`
- 변경 사항:
  1. Preview 렌더 rect를 고정 좌표계로 통일
  2. 텍스처와 Grid를 동일 rect에 렌더링하도록 정렬
  3. Zoom out 시 영역박스 오프셋 어긋남 현상 수정

### 현재 상태 (Step 3)
- Step 3 코드 반영 완료(단일 소스 정책 기준)
  - 단일 Source 처리
  - 액션 수동 지정 옵션 + 자동 fallback
  - Unknown 그룹 처리

### Unity 수동 검증 체크리스트 (2026-02-19)
1) 줌아웃 오프셋 검증
- 준비: `AI Sprite Processor` 열기, Source Texture 지정
- 절차:
  - Zoom 값을 `1.0 -> 0.5 -> 0.2` 순으로 낮춤
  - 각 단계에서 Grid(녹색/빨강)와 실제 스프라이트 프레임 경계 일치 여부 확인
  - Scroll 이동 후에도 동일 위치 정합 유지 확인
- 통과 기준:
  - Zoom out 상태에서 Grid가 이미지와 분리되어 떠 보이지 않음
  - Scroll 전/후 좌표 어긋남 재현되지 않음
- 결과: ✅ 통과

2) 단일 Source 액션 지정 검증
- 준비: 파일명에 액션 키워드가 없는 테스트 텍스처 1개
- 절차 A(수동 지정 ON):
  - `액션 그룹 수동 지정` ON
  - `Action Group = Walk` 선택
  - `Process & Slice` 실행
- 기대 결과 A:
  - 출력 파일 prefix가 `walk_`로 생성
- 절차 B(수동 지정 OFF):
  - `액션 그룹 수동 지정` OFF
  - 동일 텍스처로 `Process & Slice` 실행
- 기대 결과 B:
  - 파일명 키워드 미검출 시 `unknown_` prefix로 생성
- 결과: ✅ 통과

3) 회귀 검증(기존 기능)
- 배경제거 모드 A/B 전환 후 프리뷰 정상 표시
- SmartSlice 모드에서 감지 Rect 시각화 정상
- 입력값 검증 다이얼로그 정상 노출
- 결과: ✅ 통과

### 수동 검증 종합 결과
- 상태: ✅ 전체 통과
- 결론: 단일 Source + 액션 수동 지정 정책 및 줌아웃 오프셋 보정이 실제 Editor 동작에서 정상 확인됨

### 리스크/메모
- 이미지 데이터셋 가변성이 크므로, 자동 처리 실패 시 수동 Override 경로를 반드시 제공할 것.
- 원본 덮어쓰기 금지 정책 유지.
- 현재 단계는 "Step 3 1차 코드 반영 완료, 후속(문서/검증/분류 로직) 진행" 상태.
- 사용자 검증 체크:
  1) 다중 입력에서 N개 소스가 각각 `*_Processed.png`로 생성되는지
  2) 일부 슬롯이 비어 있어도 나머지 유효 입력만 정상 처리되는지
  3) 기존 단일 입력 모드 동작이 회귀 없이 유지되는지
