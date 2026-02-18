# WorldMap 이미지 생성 프롬프트 (NotebookLM 연동)

- 작성일: 2026-02-14
- 기준 노트북: `킹덤러쉬` (`778e7a86-d054-4560-95de-b7378c2a11d3`)
- 대상 자산(현재 코드 기준):
  - `UI/Sprites/WorldMap/WorldMap_Background`
  - `UI/Sprites/WorldMap/Icon_Back`
  - `UI/Sprites/WorldMap/Icon_Hero`
  - `UI/Sprites/WorldMap/Icon_Stage`
  - `UI/Sprites/WorldMap/Icon_Upgrade`

## 공통 스타일 토큰
아래 키워드는 모든 자산 프롬프트에 공통으로 포함:

```text
Kingdom Rush art style, 2D vector art, flat cartoon style, thick bold outlines,
vibrant saturated colors, hand-drawn game asset, Flash game aesthetic, chunky shapes
```

- 권장: 동일 seed 고정(예: `123456`)으로 시리즈 일관성 유지

---

## 1) 월드맵 배경 (`WorldMap_Background`)

### Positive Prompt
```text
game level select world map, top-down isometric view, winding dirt paths connecting distinct zones,
fantasy kingdom landscape, mix of green forests and rocky mountains, medieval fantasy setting,
distinct biomes, detailed terrain textures, readable path flow,
Kingdom Rush art style, 2D vector art, bold outlines, vibrant colors, no UI elements, clean composition
```

### Negative Prompt
```text
UI buttons, text, icons, grid lines, 3D render, photorealistic, blurry, low resolution,
gradients, noise, cluttered elements, modern buildings, cars
```

### 권장 규격
- 16:9 권장: `1920x1080` 또는 `2560x1440`
- 이미지 사이즈(기본): `1920x1080`

---

## 2) 버튼 아이콘 (`Icon_Back`, `Icon_Hero`, `Icon_Stage`, `Icon_Upgrade`)

### A. Back (`Icon_Back`)
#### 이미지 사이즈
- `512x512` (기본)
- 고해상도 필요 시 `1024x1024`

#### Positive Prompt
```text
UI button icon for "Back", red curved arrow pointing left, wooden button frame with metal rim,
game interface asset, game UI icon, isolated on white background, chunky vector style, wood and stone texture,
Kingdom Rush art style, 2D vector art, flat cartoon style, thick bold outlines, vibrant saturated colors,
hand-drawn game asset, Flash game aesthetic, cel-shaded, high contrast, 1:1 composition, 512x512
```
#### Negative Prompt
```text
text, realistic, 3D render, complex details, thin lines, watermark
```

### B. Hero (`Icon_Hero`)
#### 이미지 사이즈
- `512x512` (기본)
- 고해상도 필요 시 `1024x1024`

#### Positive Prompt
```text
UI button icon for "Hero", knight helmet symbol or crossed swords, stone button texture with gold border,
medieval fantasy style, heroic glowing effect, game interface asset, game UI icon, isolated on white background,
chunky vector style, wood and stone texture, Kingdom Rush art style, 2D vector art, flat cartoon style,
thick bold outlines, vibrant saturated colors, hand-drawn game asset, Flash game aesthetic,
1:1 composition, 512x512
```
#### Negative Prompt
```text
face photo, realistic armor, text, complex background, scary
```

### C. Stage (`Icon_Stage`)
#### 이미지 사이즈
- `512x512` (기본)
- 고해상도 필요 시 `1024x1024`

#### Positive Prompt
```text
UI button icon for "Stage Select", medieval battle flag or stone tower symbol, wooden sign texture,
bright colors to indicate action, game interface asset, game UI icon, isolated on white background,
chunky vector style, wood and stone texture, Kingdom Rush art style, 2D vector art, flat cartoon style,
thick bold outlines, vibrant saturated colors, hand-drawn game asset, Flash game aesthetic,
chunky design, 1:1 composition, 512x512
```
#### Negative Prompt
```text
modern building, map lines, text, realistic fabric, noise
```

### D. Upgrade (`Icon_Upgrade`)
#### 이미지 사이즈
- `512x512` (기본)
- 고해상도 필요 시 `1024x1024`

#### Positive Prompt
```text
UI button icon for "Upgrade", golden upward arrow or blacksmith hammer,
metallic button texture with magical glow, game interface asset, game UI icon, isolated on white background,
chunky vector style, wood and stone texture, Kingdom Rush art style, 2D vector art, flat cartoon style,
thick bold outlines, vibrant saturated colors, hand-drawn game asset, Flash game aesthetic,
symbol of power, 1:1 composition, 512x512
```
#### Negative Prompt
```text
text, realistic tools, rust, dirty texture, complex machinery
```

---

## 실무 적용 메모
1. 배경과 버튼은 반드시 같은 스타일 토큰 + 동일 seed 계열로 생성
2. 아이콘은 배경 제거 후 스프라이트로 임포트
3. Unity 적용 경로는 `Assets/Resources/UI/Sprites/WorldMap/` 기준
