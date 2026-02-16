# Hero Animation ComfyUI 프롬프트 (복붙용)

대상 HeroId: `DefaultHero`  
목표: `idle / walk / attack / die` 프레임을 생성해 Unity `Resources` 경로에 투입

---

## 1) 공통 설정

- 스타일 키워드:
```text
kingdom rush style, stylized 2d game character, clear silhouette, bold outline, flat colors, fantasy hero, readable at small size, transparent background
```

- 네거티브:
```text
photorealistic, 3d render, blurry, text, watermark, logo, background scene, extra limbs, distorted anatomy
```

- 권장 출력:
  - PNG
  - 투명 배경
  - 512x512 (또는 384x384)
  - 캐릭터 중심, 여백 적절히 확보

---

## 2) 액션별 프롬프트

### A. Idle
```text
top-down 3/4 fantasy hero idle stance, sword and shield ready, kingdom rush style, stylized 2d game sprite, bold outline, flat colors, transparent background
```

### B. Walk
```text
top-down 3/4 fantasy hero walk cycle pose, forward movement feeling, kingdom rush style, stylized 2d game sprite, bold outline, flat colors, transparent background
```

### C. Attack
```text
top-down 3/4 fantasy hero attack swing pose, sword slash motion, strong readable action, kingdom rush style, stylized 2d game sprite, bold outline, flat colors, transparent background
```

### D. Die
```text
top-down 3/4 fantasy hero death/fall pose, dramatic but clean silhouette, kingdom rush style, stylized 2d game sprite, bold outline, flat colors, transparent background
```

---

## 3) 시퀀스 생성 규칙

각 액션을 프레임 단위로 생성/추출해서 다음 파일명으로 저장:

- `idle_00.png`, `idle_01.png`, ...
- `walk_00.png`, `walk_01.png`, ...
- `attack_00.png`, `attack_01.png`, ...
- `die_00.png`, `die_01.png`, ...

프레임 수 권장:
- idle: 6~10
- walk: 8~12
- attack: 4~8
- die: 6~10

---

## 4) Unity 저장 경로

### 인게임 애니메이션 프레임
- `Assets/Resources/UI/Sprites/Heroes/InGame/DefaultHero/idle_00.png`
- `Assets/Resources/UI/Sprites/Heroes/InGame/DefaultHero/walk_00.png`
- `Assets/Resources/UI/Sprites/Heroes/InGame/DefaultHero/attack_00.png`
- `Assets/Resources/UI/Sprites/Heroes/InGame/DefaultHero/die_00.png`

### HUD 포트레이트(단일)
- `Assets/Resources/UI/Sprites/Heroes/Portraits/DefaultHero.png`

---

## 5) 코드 연동 상태

현재 코드가 자동으로 아래를 로드한다.

- HUD Portrait:
  - `UI/Sprites/Heroes/Portraits/{HeroId}`
- InGame 단일:
  - `UI/Sprites/Heroes/InGame/{HeroId}`
- InGame 시퀀스(우선):
  - `UI/Sprites/Heroes/InGame/{HeroId}/idle_00`부터 연속 프레임
  - `walk_00`, `attack_00`, `die_00` 동일 규칙

리소스가 없으면 fallback 스프라이트를 사용한다.

