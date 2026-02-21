# 스프라이트 생성 프롬프트 옵션 1 (Ronanobanana Pro 최적화버전)

> 작성일: 2026-02-21  
> 대상 모델: **Ronanobanana Pro (SDXL 기반 픽셀아트 모델)**
> 목적: 텍스트 없는 4행 스프라이트시트 고정 출력 (`idle / walk / attack / die`)  
> 권장 해상도: SDXL 권장인 `1024x1024` (정사각)  
> 필수 배경: 단색 마젠타 `#FF00FF`

---

아래는 **Ronanobanana Pro** 모델의 특성에 맞춘 프롬프트입니다.
SDXL 기반 픽셀 아트 모델은 태그(단어)와 자연어의 혼합을 잘 이해합니다. `pixel art` 키워드를 선두에 배치하고, 그리드 형태와 캐릭터 외형을 명확하게 태그 형태로 분리하여 묘사하는 것이 이 모델에서 가장 결과물이 잘 나옵니다.

유저 요청에 따라 모든 프롬프트에 **치비(Chibi)** 스타일을 적용하여, 머리가 크고 귀여운(cute, big head, small body) 비율의 픽셀 아트가 나오도록 키워드를 보강했습니다.

## 💡 Ronanobanana Pro 입력 팁
1. **그리드 설정 (4x4 구조 권장):** AI 모델 특성상 프레임이 너무 많아지면 화질이 뭉개지거나 캐릭터가 변형되기 쉽습니다. (예: 24프레임 이상 생성 시 디자인 일관성 붕괴). 따라서 가로세로 **`4x4 grid, 16 frames`**로 지정하여 각 동작(idle/walk/attack/die)당 4프레임씩 안정적으로 뽑는 것을 최우선으로 권장합니다. 
2. **가중치 활용:** 만약 워크플로우 상에서 배경색이 잘 안 먹히거나 특정 요소가 무시된다면, 해당 키워드에 괄호를 씌워 `(solid magenta background:1.2)` 처럼 가중치를 줄 수 있습니다.
3. **네거티브 프롬프트 분리:** 텍스트나 UI가 섞여 나오는 것을 막기 위해 네거티브 프롬프트(Negative Prompt)를 별도의 입력 노드/칸에 정확히 입력해주세요.

---

## 1) 기본 보병 (Default Footman)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, medieval footman, simple iron armor, blue tabard, holding short broadsword and wooden shield, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row walk cycle animation, third row sword attack animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 2) 중장갑 기사 (Armored Knight)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, heavily armored knight, full plate armor, closed silver helmet, tower shield, steel mace, defensive stance, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row walk cycle animation, third row mace attack animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 3) 고블린 전사 (Goblin Warrior)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, ugly green goblin warrior, ragged brown leather armor, crude wooden club, fierce expression, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row walk cycle animation, third row club attack animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 4) 오크 브루트 (Orc Brawler)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, massive brutal orc brawler, muscular grey skin, spiked shoulder pads, heavy dual-bladed axe, aggressive stance, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row walk cycle animation, third row axe attack animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 5) 좀비 (Zombie)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, rotting zombie, pale green skin, torn peasant clothes, bare hands reaching forward, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row slow walk cycle animation, third row attack bite animation, bottom row death collapsing animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 6) 정찰병 (Scout / Bandit)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, agile scout rogue, light leather armor, green hooded cloak, holding dual daggers, fast posture, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row fast run cycle animation, third row dual dagger attack animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 7) 샤먼 (Shaman)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, tribal goblin shaman, wearing animal skulls and feathers, holding a wooden magic staff, casting posture, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row walk cycle animation, third row magic casting animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 8) 오우거 (Ogre)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, massive fat ogre, pale yellow skin, wearing simple loincloth, holding a giant wooden club, lumbering posture, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row slow walk cycle animation, third row club smash attack animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 9) 스켈레톤 (Skeleton)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, undead skeleton warrior, bare bones, holding a rusty sword and cracked wooden shield, shaky posture, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row walk cycle animation, third row sword attack animation, bottom row death collapsing animation into bones, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, flesh, skin, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

---

## 10) 메이지 헌터 (Mage Hunter)

**Positive Prompt:**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, elite berserker mage hunter, anti-mage warrior, dark glowing tattoos, heavy fur hooded cloak, holding a glowing runic greatsword, aggressive posture, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row fast run cycle animation, third row greatsword attack animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

**Negative Prompt:**
```text
low quality, worst quality, blurry, deformed, 3d, realistic, painterly, realistic proportions, tall character, text, fonts, letters, watermark, signature, ui, words, shadow, cast shadow, drop shadow, multiple different characters, inconsistent outfit, changing weapons, unaligned grid
```

---

## 11) 특수 기믹 유닛 프롬프트 대처 가이드 (Special Units)
걷고, 때리고, 쓰러지는 일반적인 4x4 그리드(16프레임) 구조로는 처리하기 애매한 몬스터들(원거리 공격, 비행, 자폭 등)을 위한 패턴별 프롬프트 가이드입니다.

### A. 일반 중무장 보병/엘리트 (다크 나이트, 머로더, 다크 슬레이어 등)
이들은 체력과 방어력이 높을 뿐 액션 자체는 기본 보병/오크와 완전히 동일합니다.
**대처법:** 기존 '오크 브루트'나 '중장갑 기사'의 프롬프트에서 `massive brutal orc`를 `dark knight clad in black armor` 등으로 **외형 키워드 1~2개만 교체**하고 그대로 출력합니다.

### B. 무기 투척/원거리 유닛 (트롤 챔피언 등)
근접 무기를 휘두르는 대신 창이나 도끼를 던지는 등 다른 형태의 공격 애니메이션이 필요합니다.
**대처법:** 3번째 줄(row 3)의 Attack 액션 설명을 '투척'으로 변경합니다.

**원거리 유닛용 프롬프트 (예: 트롤 챔피언)**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, muscular jungle troll champion, green skin, holding a large throwing spear, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row walk cycle animation, third row spear throwing ranged attack animation, bottom row death falling animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

### C. 비행 유닛 (가고일/윙 슈트/제트팩)
지상을 걷지 않기 때문에 'Walk' 대신 'Fly' 모션이 필요하며, 'Idle' 상태에서도 날개나 로켓을 퍼덕이는 Hovering 동작이 들어가야 어색하지 않습니다.
**대처법:** 1번째, 2번째 줄의 액션 묘사를 비행(Hover/Fly)에 맞게 커스텀합니다.

**비행 유닛용 프롬프트 (예: 윙 슈트/가고일 대체재)**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, steampunk goblin wearing mechanical glider wing suit, floating in air, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row hovering idle animation in air, second row flying forward animation, third row dive attack animation, bottom row crashing to ground death animation, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

### D. 자폭 / 폭발 유닛 (데몬 스폰, 데몬 스토커)
이 유닛들은 평범하게 픽 쓰러져 죽는(death collapsing) 모션 대신, 산산조각 폭발하는(exploding) 특별한 Death 모션이 필요합니다.
**대처법:** 4번째 줄(row 4)의 Death 액션 설명을 '폭발'로 변경합니다.

**자폭 유닛용 프롬프트 (예: 데몬 스폰)**
```text
pixel art, masterpiece, high quality, sprite sheet, 4x4 grid, 16 frames, isometric view, chibi style, cute, big head, small body, small fiery red demon spawn, glowing yellow eyes, little horns, facing right, 3/4 view, full body, solid magenta background, clear pixel edges, equal cell size, top row idle animation, second row fast run cycle animation, third row claw swipe attack animation, bottom row violent exploding fiery death animation leaving ashes, completely identical character design in all frames, no ui, no text, no shadow, flat shading
```

### E. 규격 외 초대형 보스 (저거너트, 베즈난, J.T.)
화면에 꽉 차는 거대한 크기, 혹은 2페이즈 변신 기믹이 있는 보스들은 1024x1024 한 장에 16프레임을 욱여넣으면 픽셀이 완전히 뭉개집니다. 
**대처법 (별개 관리):**
*   가로세로 `4x4 grid`를 풀고, **`2x2 grid` 나 `1 row 4 frames`** 수준으로 프레임 수를 대폭 줄여서 해상도를 확보해야 합니다.
*   예를 들어, 보스의 '걷기 4프레임'만 단독 프롬프트로 한 번 생성하고, '공격 4프레임'만 따로 생성한 뒤 합치는 등 **액션 종류별로 분할 생성 작업**을 권장합니다.
