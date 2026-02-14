# StageInfoPopup 이미지 프롬프트 (나노바나나용)
아래 프롬프트를 그대로 복사해서 사용하면 됩니다.

## 1) 패널 배경 (`StageInfoPopup_Panel.png`)
- 용도: 팝업 본문 배경 (`9-slice` 전제)
- 권장 해상도: `1024 x 768` (PNG, 불투명)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Panel.png`
- 참고: 테두리 두께 48px 이상 확보 (슬라이스 안전 영역)

**Positive Prompt**
```text
fantasy mobile game ui popup panel background, kingdom rush inspired 2d hand-painted style, dark parchment stone mix, ornate but readable frame, centered rectangular panel with thick decorative border, subtle top highlight and inner shadow, clean silhouette, high contrast edge, no text, no icon, no characters, flat front view, game-ready ui asset
```

**Negative Prompt**
```text
text, logo, watermark, blurry, photorealistic, 3d render, perspective distortion, noisy texture, character, weapon, cluttered ornaments
```

---

## 2) 기본 버튼 (`StageInfoPopup_Button.png`)
- 용도: 난이도/Back 공용 버튼 베이스 (`9-slice`)
- 권장 해상도: `512 x 192` (PNG, 불투명)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button.png`
- 참고: 중앙은 비교적 단순, 좌우 끝 장식 분리 가능하게

**Positive Prompt**
```text
fantasy game ui button plate, hand-painted 2d, carved wood and bronze trim, horizontally stretchable button body, centered front view, clear border for 9-slice, clean shading, no text, no symbols, no icon, game-ready mobile ui
```

**Negative Prompt**
```text
text, letters, logo, watermark, blurry, realistic photo, 3d perspective, heavy grunge noise, uneven silhouette
```

---

## 3) 강조 버튼 (`StageInfoPopup_Button_Start.png`)
- 용도: Start 전용 강조 버튼 베이스
- 권장 해상도: `512 x 192` (PNG, 불투명)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button_Start.png`

**Positive Prompt**
```text
fantasy game ui button for primary action, vivid emerald green glow accents, hand-painted 2d style, thick readable border, centered front view, no text, no icon, no logo, mobile game quality, clean high contrast
```

**Negative Prompt**
```text
text, watermark, photorealistic, 3d render, blur, muddy colors, low contrast, visual clutter
```

---

## 4) 닫기 버튼 (`StageInfoPopup_Button_Close.png`)
- 용도: 우상단 X 버튼 배경
- 권장 해상도: `256 x 256` (PNG, 불투명)
- 저장 경로: `Assets/Resources/UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button_Close.png`

**Positive Prompt**
```text
fantasy ui close button base, red lacquered square with beveled edge, medieval gold corner trim, front view, centered, no text, no X symbol, hand-painted 2d icon style, crisp edge, game-ready ui
```

**Negative Prompt**
```text
letters, text, logo, watermark, blur, photorealistic, 3d perspective, noisy background
```

---

## Unity 적용 가이드
1. 위 파일을 `Assets/Resources/UI/Sprites/WorldMap/StageInfoPopup/`에 배치
2. 임포트 설정:
   - Texture Type: `Sprite (2D and UI)`
   - Mesh Type: `Full Rect`
   - Compression: UI 용도에 맞게 낮춤
3. `9-slice`:
   - `StageInfoPopup_Panel`: 테두리 48px 기준으로 Slice
   - `StageInfoPopup_Button`: 테두리 24px 기준으로 Slice
4. 메뉴 실행:
   - `Tools/Kingdom/Build StageInfoPopup Prefab`
5. 확인:
   - 월드맵에서 스테이지 클릭 -> 팝업 패널/버튼 스킨 적용 확인
