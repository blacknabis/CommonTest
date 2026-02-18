# AI Sprite Processor 작업 계획 및 인수인계서 (Solo Friendly v4, 2026-02-18)
> **문서 성격**: 1인 개발 기준 실행 계획서 (과업 축소 + 자동화 우선)
> **대상 코드**: `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`
> **핵심 전략**: 런타임 애니메이션 확장보다, 먼저 **가변 입력을 견디는 툴 자동화** 완성

---

## 0. 결론 요약 (먼저 결정)
- 현재 문서는 삭제하지 않고 **유지/갱신**하는 것을 권장합니다.
- 이유: 지금 필요한 의사결정(범위 축소, 가변 입력 대응, 순차 개발)이 이 문서에 누적되어 이후 재작업 비용을 줄여줍니다.
- 단, 내용이 무거우므로 **1인 개발용 v4 계획으로 슬림화**합니다.

---

## 1. 현실 제약 반영 (문제 정의)
현재 실제 제약:
1) 이미지 소스 수급/생성 자체가 오래 걸림
2) 소스가 규격화되어 있지 않음 (해상도/비율/배경/프레임 수 가변)
3) 액션(Idle/Walk/Attack/Die)이 한 파일이 아닌 여러 파일로 분산될 수 있음
4) 액션 간 프레임 크기, 정렬 기준(발 위치), 캔버스 크기가 다름

따라서 우선순위는 아래 1개로 고정:
- **"입력이 엉망이어도 최종 스프라이트 세트를 자동 정리하는 툴"**

---

## 2. 범위 재설계 (반드시 지킬 것)

### 2-1. 이번 사이클 포함 (Must Have)
- 프리셋 저장/로드(JSON)
- Smart Slice(아일랜드 검출) + 노이즈 필터
- 멀티 입력 처리(여러 파일 동시 처리)
- 액션 분류 보조(파일명 키워드 기반: idle/walk/attack/die)
- 캔버스 정규화(프레임 크기 통일, Pivot 기준 정렬)
- 결과 Manifest(JSON) 생성

### 2-2. 이번 사이클 제외 (Won’t Have)
- Animator Controller 자동 생성
- 런타임 자동 배선(코드 자동 패치)
- 고급 보간/리타게팅

---

## 3. 핵심 산출물 정의

### 3-1. 폴더 출력 구조 (권장)
`Assets/Resources/UI/Sprites/Heroes/InGame/{HeroId}/`
- `idle_00.png`, `idle_01.png` ...
- `walk_00.png`, `walk_01.png` ...
- `attack_00.png` ...
- `die_00.png` ...
- `manifest.json`

### 3-2. Manifest(JSON) 최소 스키마
```json
{
  "heroId": "Knight",
  "sourceFiles": ["raw_idle_sheet.png", "walk_frames.png"],
  "actions": {
    "idle": { "count": 6, "maxW": 320, "maxH": 320, "pivot": [0.5, 0.08] },
    "walk": { "count": 8, "maxW": 360, "maxH": 340, "pivot": [0.5, 0.08] },
    "attack": { "count": 7, "maxW": 420, "maxH": 360, "pivot": [0.5, 0.08] },
    "die": { "count": 5, "maxW": 380, "maxH": 260, "pivot": [0.5, 0.08] }
  },
  "warnings": ["attack 프레임 크기 편차 큼", "walk 소스 2개 병합됨"]
}
```

---

## 4. 1인 개발 순차 계획 (2주 기준)

### Step 1 (D1~D2): 프리셋/검증 기반 만들기
목표: 반복 입력 제거
- `AISpriteProcessConfig` 직렬화
- Save/Load/Delete 프리셋
- 입력 검증(음수, 0-size, padding 과다)

완료 기준:
- 동일 소스 재작업 시 1회 클릭 + 프리셋 로드로 재현 가능

### Step 2 (D3~D5): Smart Slice + 노이즈 대응
목표: 불규칙 시트 자동 분할
- BFS/DFS island 검출
- `alphaThreshold`, `minIslandPixels`
- Rect 정렬(좌상단→우하단)

완료 기준:
- 수동 rect 보정 비율 20% 이하

### Step 3 (D6~D8): 멀티 입력 + 액션 분류
목표: 분산 파일 대응
- 다중 텍스처 일괄 입력
- 파일명 키워드 규칙으로 액션 자동 라벨링
  - 예: `*idle*`, `*walk*`, `*attack*`, `*die*`
- 액션 미검출 시 `Unknown` 그룹으로 격리

완료 기준:
- 최소 1영웅 데이터셋에서 4액션 자동 그룹화 성공

### Step 4 (D9~D10): 캔버스 정규화 + Manifest
목표: 크기 제각각 문제 해결
- 액션별/전체 최대 bbox 계산
- Pivot 기준(기본 `0.5, 0.08`)으로 프레임 정렬
- PNG 출력 + `manifest.json` 기록

완료 기준:
- 크기 다른 원본으로도 런타임에서 떨림 없이 재생 가능한 시퀀스 확보

---

## 5. 데이터 가변성 대응 규칙 (중요)

### 5-1. 프레임 크기 불일치
- 기본 정책: **큰 캔버스에 작은 프레임을 배치**
- 정렬 기준: Bottom-Center Pivot 고정

### 5-2. 액션별 파일 분산
- 1차: 파일명 기반 자동 분류
- 2차: 사용자 수동 override (드롭다운으로 idle/walk/attack/die 지정)

### 5-3. 배경 품질 불량
- Chroma Key 실패 대비 `alphaThreshold` 우선
- 미세 노이즈는 `minIslandPixels`로 제거

### 5-4. 파일명 충돌
- `_v2`, `_v3` 자동 넘버링
- 원본 덮어쓰기 금지

---

## 6. KPI (1인 개발형으로 조정)
- 이미지 1세트(다중 파일 포함) 처리: **2~5분 이내**
- 재처리(프리셋 사용): **30초~1분 이내**
- 수동 개입 포인트: **초기 6회 → 2회 이하**
- 실패 시 원복 시간: **0분**(원본 불변 정책)

---

## 7. 코드 적용 지점 가이드
- 1차 구현 파일(고정): `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`
- 2차 분리 후보:
  - `AISpritePresetStore.cs`
  - `AISpriteSmartSlice.cs`
  - `AISpriteManifestWriter.cs`

런타임 연동은 이번 사이클 범위 밖이지만, 후속 연결 지점은 아래를 기준으로 유지:
- `Assets/Scripts/Kingdom/Game/SpawnManager.cs` 내 `LoadHeroFrameSequence`

---

## 8. 리스크 및 방어선
- 리스크: 기능 욕심으로 일정 붕괴
  - 방어: 이번 사이클은 "툴 자동화"만
- 리스크: 데이터셋이 매번 달라 알고리즘 불안정
  - 방어: 자동 + 수동 override 동시 제공
- 리스크: 품질 문제 디버깅 장기화
  - 방어: `manifest.json`에 경고/처리옵션 기록

---

## 9. 최종 의사결정
- 질문: 기존 문서를 지워도 되는가?
- 답: **삭제보다 덮어쓰기/갱신이 정답**.
  - 파일 경로 연속성 유지
  - 작업 히스토리 단절 방지
  - 이후 체크리스트/로그 연동이 쉬움

현재 문서는 위 기준으로 v4로 재정의되었으며, 다음 액션은 **Step 1(프리셋/검증)** 착수입니다.
