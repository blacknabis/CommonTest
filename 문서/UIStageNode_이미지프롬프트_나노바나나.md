# UIStageNode 이미지 프롬프트 (나노바나나용)

각 프롬프트는 독립적으로 사용 가능합니다. 공통 스타일/금지 키워드가 각 프롬프트에 포함되어 있습니다.

---

## A. 선택 하이라이트 (`UIStageNode_SelectedHighlight`)
- 용도: 선택된 스테이지 노드 강조 FX 링
- 권장 크기: `512 x 512` (투명 배경 PNG)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/UIStageNode_SelectedHighlight.png`

**Positive Prompt:**
```
fantasy game ui selection ring, warm golden magical glow, subtle blue inner light, circular badge aura, centered, clean outline, hand painted 2d, kingdom rush style 2d fantasy ui, game-ready icon, centered composition, crisp edge, isolated on solid black background
```

**Negative Prompt:**
```
text, letters, words, logo, watermark, blurry, photorealistic, 3d render, character, scenery, noisy
```

---

## B. 잠금 아이콘 (`UIStageNode_LockIcon`)
- 용도: 잠금 상태 표시
- 권장 크기: `384 x 384` (투명 배경 PNG)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/UIStageNode_LockIcon.png`

**Positive Prompt:**
```
fantasy ui lock icon, medieval metal padlock with rivets, slight gold trim, front view, centered, high contrast, hand painted 2d icon, kingdom rush style 2d fantasy ui, game-ready icon, centered composition, crisp edge, isolated on solid black background
```

**Negative Prompt:**
```
text, letters, words, logo, watermark, blurry, photorealistic, 3d render, key, chain clutter, noisy
```

---

## C. 알림 점 (`UIStageNode_NotificationDot`)
- 용도: 새 이벤트/보상 알림
- 권장 크기: `256 x 256` (투명 배경 PNG)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/UIStageNode_NotificationDot.png`

**Positive Prompt:**
```
fantasy ui notification badge, red gem dot with tiny gold rim, glossy highlight, centered, simple silhouette, hand painted 2d icon, kingdom rush style 2d fantasy ui, game-ready icon, centered composition, crisp edge, isolated on solid black background
```

**Negative Prompt:**
```
text, letters, words, logo, watermark, blurry, photorealistic, 3d render, realistic plastic, complex background, noisy
```

---

## D. 별 아이콘 (`UIStageNode_Star`)
- 용도: 클리어 별 개수 표시(1~3개)
- 권장 크기: `256 x 256` (투명 배경 PNG)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/UIStageNode_Star.png`

**Positive Prompt:**
```
fantasy game ui golden star icon, five-point star, polished metal with soft glow, centered, clear outline, hand painted 2d icon, kingdom rush style 2d fantasy ui, game-ready icon, centered composition, crisp edge, isolated on solid black background
```

**Negative Prompt:**
```
text, letters, words, logo, watermark, blurry, photorealistic, 3d render, noisy, asymmetry
```

---

## Unity 연결 대상
- 프리팹: `Assets/Resources/UI/Components/WorldMap/UIStageNode.prefab`
- 연결 필드:
  - `selectedHighlight` -> `SelectedHighlight` 오브젝트
  - `lockIcon` -> `LockIcon` 오브젝트
  - `notificationDot` -> `NotificationDot` 오브젝트
  - `starImages` -> `StarContainer/Star1~3` Image 배열
