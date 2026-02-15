# Hero Sprite Prompt Guide (ComfyUI / 나노바나나용)

이 문서는 영웅 `Portrait`와 `InGame Sprite`를 ComfyUI로 생성하고 Unity에 연동하기 위한 실전 가이드다.  
현재 프로젝트 연동 경로 규칙:

- 포트레이트(UI): `Assets/Resources/UI/Sprites/Heroes/Portraits/{HeroId}.png`
- 인게임(월드): `Assets/Resources/UI/Sprites/Heroes/InGame/{HeroId}.png`

`HeroId` 예시: `DefaultHero`

---

## 1) 산출물 규격

### A. Portrait (UI)
- 용도: `HeroPortraitWidget` HUD 슬롯
- 권장 해상도: `512x512` 또는 `1024x1024`
- 배경: 투명 PNG
- 구도: 흉상 중심, 얼굴/실루엣 명확

### B. InGame Sprite (월드)
- 용도: `HeroController`의 `SpriteRenderer`
- 권장 해상도: `256x256` 또는 `512x512`
- 배경: 투명 PNG
- 구도: 전신, 상단 45도 느낌(쿼터뷰), 게임 내 가독성 우선

---

## 2) ComfyUI 공통 프롬프트 (복붙용)

### Positive Prompt (공통)
```text
kingdom rush style, stylized 2d game character, clean silhouette, bold outline, flat colors, readable shape language, fantasy hero, high contrast, transparent background, no text, no watermark
```

### Negative Prompt (공통)
```text
photorealistic, realistic skin pores, 3d render, low contrast, blurry, noisy background, logo, watermark, text, cropped head, deformed hands, extra limbs
```

---

## 3) Portrait 전용 프롬프트

```text
hero portrait icon, bust shot, centered composition, expressive face, shoulders visible, kingdom rush style, stylized 2d vector-like shading, bold black outline, clean flat colors, transparent background
```

추가 키워드 예시:
- 기사형: `armored knight, blue tabard, sword emblem`
- 궁수형: `ranger hood, leather armor, emerald accent`
- 마법사형: `arcane robe, glowing rune ornament`

---

## 4) InGame 전용 프롬프트

```text
top-down 3/4 view fantasy hero sprite, full body, idle combat stance, kingdom rush style, stylized 2d game sprite, bold outline, flat colors, clear readability at small size, transparent background
```

추가 키워드 예시:
- 기사형: `shield-forward stance, heavy armor`
- 궁수형: `bow ready pose, light armor`
- 마법사형: `staff ready pose, robe with simple ornaments`

---

## 5) 파일명/저장 규칙

동일한 `HeroId`를 Portrait/InGame 모두 동일하게 맞춘다.

예시 (`HeroId = DefaultHero`)
- `Assets/Resources/UI/Sprites/Heroes/Portraits/DefaultHero.png`
- `Assets/Resources/UI/Sprites/Heroes/InGame/DefaultHero.png`

코드는 위 경로를 자동 로드한다.  
파일이 없으면 기존 fallback(임시 흰색 스프라이트/빈 포트레이트)로 동작한다.

---

## 6) Unity 임포트 설정 체크리스트

- Texture Type: `Sprite (2D and UI)`
- Alpha Is Transparency: `On`
- Compression: `High Quality`(또는 프로젝트 정책)
- Filter Mode:
  - 픽셀풍이면 `Point`
  - 일반 2D 일러스트풍이면 `Bilinear`
- Max Size: 원본 해상도에 맞춰 충분히 크게

---

## 7) 빠른 테스트 순서

1. ComfyUI에서 Portrait/InGame 각각 1장 생성
2. 경로 규칙대로 파일 저장
3. Unity `Refresh` 후 Play
4. 확인:
   - HUD 영웅 포트레이트 반영
   - 월드 영웅 스프라이트 반영
   - 파일 제거 시 fallback 정상 동작

