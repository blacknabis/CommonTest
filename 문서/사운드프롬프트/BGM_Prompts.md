# AI 배경음악 생성 프롬프트 (Kingdom Rush Style)

이 문서는 NotebookLM의 `킹덤러쉬` 분석 데이터를 바탕으로, 각 씬(Scene)에 적합한 배경음악을 AI에게 요청하기 위한 프롬프트 모음입니다.

> **중요 (Important)**: 배경음악(BGM)이므로 반드시 **보컬 없는 연주곡(Instrumental)**으로 생성해야 합니다.
> *   **Suno/Udio**: `Instrumental` 모드를 켜거나, 프롬프트에 `[Instrumental]` 태그를 포함하세요.
> *   **Negative Prompt**: `vocals, lyrics, voice, singing, speech`

## 1. 공통 스타일 (Common Style)
*   **장르**: Classic Medieval Fantasy Orchestral (고전 중세 판타지 오케스트라)
*   **분위기**: Bright, Heroic, Cartoonish, Lighthearted (밝고, 영웅적이며, 만화 같은 가벼움)
*   **참고**: 너무 어둡거나 심각한(Dark, Somber) 분위기는 피하고, 선명한 멜로디와 리듬감을 강조하세요.

---

## 2. 씬별 프롬프트 (Scene Prompts)

### A. 타이틀 화면 (TitleScene)
모험의 시작을 알리는 웅장하고 활기찬 테마입니다.

*   **키워드**: `Instrumental`, `Epic`, `Heroic`, `Adventurous`, `Fanfare`, `Marching`, `Upbeat`, `No Vocals`
*   **추천 악기**: Trumpets (트럼펫), French Horns (호른), Snare Drums (스네어 드럼), Staccato Strings (스타카토 현악기)
*   **AI 프롬프트 (English)**:
    > "[Instrumental] Compose a main theme for a medieval tower defense game. The style should be orchestral fantasy with a heroic and adventurous tone. Use bright brass fanfares (trumpets) and marching snare drums. Keep the melody catchy and upbeat. High energy intro. No vocals."

### B. 월드맵 (WorldMapScene)
전투를 준비하며 왕국을 조망하는 전략적이고 평화로운 분위기입니다.

*   **키워드**: `Instrumental`, `Strategic`, `Preparation`, `Medieval Folk`, `Restful`, `Planning`, `No Vocals`
*   **추천 악기**: Lute (류트), Acoustic Guitar (어쿠스틱 기타), Flute/Piccolo (플루트/피콜로), Light Percussion (가벼운 타악기)
*   **AI 프롬프트 (English)**:
    > "[Instrumental] Create background music for a fantasy world map strategy screen. It should feel strategic, peaceful but with a hint of tension. Use medieval folk instruments like lutes, acoustic guitars, and woodwinds (flutes). The tempo should be moderate and steady. Not too busy, suitable for looping. No vocals."

### C. 인게임 전투 (GameScene)
끊임없이 몰려오는 적들을 막아내는 긴박하고 리듬감 있는 전투 곡입니다.

*   **키워드**: `Instrumental`, `Urgent`, `Rhythmic`, `Action`, `Battle`, `Loopable`, `Driving`, `No Vocals`
*   **추천 악기**: Orchestral Percussion (오케스트라 타악기), Cymbals (심벌즈), Driving Strings (질주하는 현악기), Brass Hits (브라스 타격음)
*   **AI 프롬프트 (English)**:
    > "[Instrumental] Generate looping battle music for a tower defense stage. The track should be energetic, rhythmic, and driving. Use orchestral percussion, fast-paced strings, and dramatic brass hits. The music must be loopable and maintain high energy without being overwhelming. Ensure clear separation of instruments. No vocals."

---

## 3. 활용 팁 (Tips)
*   **Suno / Udio**: 
    *   **Custom Mode**를 켜고 **Instrumental** 체크박스를 반드시 체크하세요.
    *   `Lyrics` 필드가 비활성화되거나 무시되더라도, `Style of Music`에 `Instrumental, Orchestral`을 명시하는 것이 좋습니다.
*   **ComfyUI (AudioLDM/Magnet)**: 
    *   Positive Prompt: `instrumental only, no vocals`
    *   Negative Prompt: `voice, singing, lyrics, speech, vocals`
