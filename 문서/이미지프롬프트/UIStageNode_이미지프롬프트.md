# UIStageNode 이미지 프롬프트

## 1) 노트 기준 공통 가이드
- 스타일: 킹덤러시풍 2D 판타지 UI, hand-painted, clean outline, high readability
- 품질 키워드: game-ready icon, centered composition, crisp edge, no watermark
- 금지 키워드: text, letters, words, logo, watermark, blurry, photorealistic, 3d render
- 생성 엔진: ComfyUI (SD1.5 계열 + rembg)

## 2) 자산별 프롬프트 + 크기

### A. 선택 하이라이트 (`UIStageNode_SelectedHighlight`)
- 용도: 선택된 스테이지 노드 강조 FX 링
- 권장 크기: `512 x 512` (투명 배경 PNG)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/UIStageNode_SelectedHighlight.png`
- Positive Prompt:
  - `fantasy game ui selection ring, warm golden magical glow, subtle blue inner light, circular badge aura, centered, clean outline, hand painted 2d, isolated on solid black background`
- Negative Prompt:
  - `text, letters, logo, watermark, character, scenery, photorealistic, 3d, noisy, blurry`

### B. 잠금 아이콘 (`UIStageNode_LockIcon`)
- 용도: 잠금 상태 표시
- 권장 크기: `384 x 384` (투명 배경 PNG)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/UIStageNode_LockIcon.png`
- Positive Prompt:
  - `fantasy ui lock icon, medieval metal padlock with rivets, slight gold trim, front view, centered, high contrast, hand painted 2d icon, isolated on solid black background`
- Negative Prompt:
  - `text, letters, logo, watermark, key, chain clutter, photorealistic, 3d render, blurry`

### C. 알림 점 (`UIStageNode_NotificationDot`)
- 용도: 새 이벤트/보상 알림
- 권장 크기: `256 x 256` (투명 배경 PNG)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/UIStageNode_NotificationDot.png`
- Positive Prompt:
  - `fantasy ui notification badge, red gem dot with tiny gold rim, glossy highlight, centered, simple silhouette, hand painted 2d icon, isolated on solid black background`
- Negative Prompt:
  - `text, letters, logo, watermark, realistic plastic, 3d, blurry, complex background`

### D. 별 아이콘 (`UIStageNode_Star`)
- 용도: 클리어 별 개수 표시(1~3개)
- 권장 크기: `256 x 256` (투명 배경 PNG)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/UIStageNode_Star.png`
- Positive Prompt:
  - `fantasy game ui golden star icon, five-point star, polished metal with soft glow, centered, clear outline, hand painted 2d icon, isolated on solid black background`
- Negative Prompt:
  - `text, letters, logo, watermark, realistic photo, 3d render, noisy, blurry, asymmetry`

## 3) Unity 연결 대상
- 프리팹: `Assets/Resources/UI/Components/WorldMap/UIStageNode.prefab`
- 연결 필드:
  - `selectedHighlight` -> `SelectedHighlight` 오브젝트
  - `lockIcon` -> `LockIcon` 오브젝트
  - `notificationDot` -> `NotificationDot` 오브젝트
  - `starImages` -> `StarContainer/Star1~3` Image 배열

