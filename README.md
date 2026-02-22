# CommonTest

Unity 기반 2D 전략/디펜스 프로젝트입니다.  
공통 프레임워크(`Assets/Scripts/Common`)와 게임 로직(`Assets/Scripts/Kingdom`)을 분리해 운영합니다.

## 실행 환경
- Unity: `6000.3.3f1` (프로젝트 기준)
- 주요 패키지: `UniTask`, `TextMeshPro`, `MCPForUnity`

## 빠른 실행
1. Unity로 프로젝트를 엽니다.
2. 메뉴 `Kingdom/Setup/Register Scenes in Build Settings` 실행
3. `Assets/Scenes/InitScene.unity`를 열고 Play

## 문서 운영
### 현재 진행 트랙
- 메인 작업 보드: `문서/진행/task.md`
- 그 외 문서는 완료 처리되어 `문서/완료/2026_02_22`로 이관됨

### 최근 완료 문서 (2026-02-22)
- 타워 본체 2액션 적용 계획: `문서/완료/2026_02_22/타워본체_애니메이션_2액션_적용계획_2026_02_22.md`
- 타워 본체 스프라이트 프롬프트: `문서/완료/2026_02_22/타워본체_스프라이트_프롬프트_2026_02_22.md`

### 문서 보관 규칙
- 진행 중인 문서는 `문서/진행`에 유지합니다.
- 완료된 산출물은 날짜 폴더(`문서/완료/YYYY_MM_DD`)로 이관합니다.
- `task.md`는 진행 상태의 단일 기준 문서로 사용합니다.

## 주요 코드 경로
- `Assets/Scripts/Kingdom/App/`: 씬/게임 흐름(`GameScene`, `WorldMapScene` 등)
- `Assets/Scripts/Kingdom/Game/`: 전투/타워/적/영웅 런타임
- `Assets/Scripts/Kingdom/Editor/`: 회귀/검증/마이그레이션 에디터 메뉴
- `Assets/Scripts/Common/Editor/`: `AISpriteProcessor` 등 공통 툴

## 회귀 점검 메뉴
1. `Tools/Common/AI Sprite Processor/Run Preset Load Regression`
2. `Tools/Kingdom/Sprites/Validate Runtime Sprite Bindings`
3. `Tools/Kingdom/Sprites/Run Missing Hint Regression`
4. `Tools/Kingdom/Sprites/Migrate Barracks Soldier Config References` (필요 시)

## 참고
- 공통 모듈 문서: `Assets/Scripts/Common/README.md`
- MCP for Unity 문서: `Assets/MCPForUnity/README.md`

## Config 경로 규칙 (2026-02-21)
- 표준(Resources) 경로는 `Assets/Resources/Kingdom/Configs/<Category>/`를 사용한다.
- 카테고리:
  - `Heroes`
  - `Enemies`
  - `Towers`
  - `Waves`
  - `Stages`
  - `BarracksSoldiers`
- 런타임 로딩은 표준 경로를 우선 사용한다.
- 레거시 경로(`Assets/Resources/Data/...`, `Assets/Resources/Kingdom/Enemies/Config`)는 마이그레이션 호환을 위해 fallback으로만 유지한다.
- 신규/수정 에셋은 레거시 경로에 생성하지 않는다.

상세 마이그레이션 내역: `문서/완료/2026_02_21/리소스_컨피그_경로_통일_마이그레이션_2026_02_21.md`
## AISpriteProcessor로 몬스터 Animator 연동
아래 순서대로 진행하면 EnemyConfig에 Animator가 자동 연결되고, 게임 런타임에서 해당 Animator가 우선 사용됩니다.

1. 입력 준비
- `AI Sprite Processor` 창에서 `BaseUnit` 탭 선택
- `Target Type = Enemy Config`, 대상 EnemyConfig 지정
- 소스 텍스처(스프라이트 시트) 지정

2. 슬라이싱 설정
- 4행 시트(idle/walk/attack/die)면 `Mode = Smart Slice` + `4행 액션 분리 사용` 활성화
- 이 모드에서는 `Action Group` 수동값이 무시됨

3. 일괄 실행
- 권장: `Run All (Process + Apply + Generate)` 버튼 1회 실행
- 수동 분리 실행 시 순서:
- `Process & Slice`
- `Apply Binding`
- `Generate Animator + Clips`

4. 생성/바인딩 결과
- 액션별 Sprite, AnimationClip, AnimatorController가 생성됨
- 대상 `EnemyConfig.RuntimeAnimatorControllerPath`가 자동 갱신됨

5. 런타임 확인
- 플레이 중 콘솔 로그 확인:
- `[SpawnManager] Enemy animator resolve. enemyId=..., configuredPath=..., loaded=...`
- `loaded` 값이 출력되면 Animator 로딩 성공

6. 주의사항
- `Generate Animator + Clips`는 소스 경로가 필요하므로 보통 `Process + Apply` 이후 실행
- ComfyUI 전처리(rembg)를 쓰는 경우 ComfyUI에 `Image Remove Background (rembg)` 커스텀 노드가 설치되어 있어야 함
