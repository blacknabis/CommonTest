# NotebookLM MCP 연결 상태 및 작업 기록

## 연결 상태 ✓

- **서비스**: notebooklm-mcp.exe
- **전송 프로토콜**: HTTP
- **호스트**: 127.0.0.1
- **포트**: 8011
- **경로**: /mcp
- **디버그 모드**: 활성화

## MCP 구성 파일 위치

`C:\Users\black\.gemini\antigravity\mcp_config.json`

## 관련 문서

1. [`문서/Design_WorldMap.md`](문서/Design_WorldMap.md) - 원본 디자인 문서
2. [`문서/Design_WorldMap_codex.md`](문서/Design_WorldMap_codex.md) - 종합 계획서
3. [`문서/Design_WorldMap_Final.md`](문서/Design_WorldMap_Final.md) - 버전 4.0 최종 문서

## 월드맵 시스템 4단계 로드맵

### Phase 1: 핵심 시스템 (Foundation)
- StageConfig.cs - ScriptableObject 기반 스테이지 설정
- WorldMapManager.cs - 월드맵 전체 상태 관리
- StageNode.cs - 개별 노드 컴포넌트
- CameraClamp.cs - 카메라 범위 제한
- ParallaxBackground.cs - 패럴랙스 배경

### Phase 2: UI 및 인터랙션 (Interaction)
- StageInfoPopup.cs - 스테이지 정보 팝업
- WorldMapScene.cs - 씬 로드/언로드
- UserSaveData.cs - 유저 진행 데이터 저장

### Phase 3: 게임플레이 통합 (Gameplay Integration)
- WorldMapReturnAnimator.cs - 클리어 복귀 연출
- StagePathRenderer.cs - 경로 렌더링
- StarUIManager.cs - 별 시스템

### Phase 4: 확장 콘텐츠 (Expansion)
- SkillTreeUI.cs - 스킬 트리 UI
- HeroSelectionUI.cs - 영웅 선택 UI
- AchievementConfig.cs - 업적 설정
- AchievementSystem.cs - 업적 시스템
- BossEventSystem.cs - 빌런 이벤트 시스템

## 기록일

2026-02-13
