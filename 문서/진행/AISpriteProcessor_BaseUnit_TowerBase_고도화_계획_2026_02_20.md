# AISpriteProcessor BaseUnit/TowerBase 고도화 계획
> 작성일: 2026-02-20  
> 마지막 수정: 2026-02-20  
> 대상 프로젝트: Kingdom  
> 대상 파일: `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`

## 0. 확정 결정 사항
1. 배럭 병사 런타임 객체(`BarracksSoldierRuntime`)는 `BaseUnit` 범주로 본다.
2. `TowerConfig`는 배럭 병사 이미지 경로를 직접 소유하지 않는다.
3. 배럭 병사 비주얼 경로는 신규 `BarracksSoldierConfig`가 소유한다.
4. 툴 책임 분리:
- `BaseUnit` 탭: Hero/Enemy/BarracksSoldier 바인딩
- `TowerBase` 탭: 타워 본체(레벨 스프라이트) 바인딩
5. 누락 데이터는 런타임 strict 정책으로 즉시 실패(중단)한다.

## 1. 목표
- `AISpriteProcessor`를 단일 텍스처 처리 도구에서, “처리 + 슬라이싱 + 에셋 바인딩 + 검증”까지 가능한 워크플로우 툴로 확장한다.
- 사용자가 에셋 하나씩 지정해 순차 작업할 수 있게 한다.
- 바인딩 누락이 숨겨지지 않도록 툴 단계와 런타임 단계 모두에서 검증한다.

## 2. 범위
### 2.1 In Scope
1. 탭 구조 추가: `Common`, `BaseUnit`, `TowerBase`
2. `BaseUnit` 탭 대상:
- `HeroConfig`
- `EnemyConfig`
- `BarracksSoldierConfig`(신규)
3. `TowerBase` 탭 대상:
- `TowerConfig` 레벨 스프라이트
4. `BarracksSoldierConfig` 신규 SO 도입
5. `TowerConfig -> BarracksSoldierConfig` 참조 구조로 마이그레이션
6. 에디터 사전 검증 + 런타임 strict 실패 정책 정렬

### 2.2 Out of Scope
1. `HeroController`를 즉시 `BaseUnit`으로 상속 전환하는 대규모 전투 리팩토링 (현재 `MonoBehaviour, IDamageable` 직접 구현)
2. 전투 밸런스 수치 튜닝
3. 이펙트/프로젝타일/사운드 자동 바인딩

## 3. 데이터 소유권 원칙
1. 비주얼 경로는 “그 비주얼을 직접 대표하는 Config”가 소유한다.
2. 타워는 병사 이미지를 직접 모르고, 병사 Config 참조만 유지한다.
3. 경로 중복 소유를 금지해 충돌/동기화 문제를 제거한다.

## 4. 데이터 모델 변경안
### 4.1 신규 ScriptableObject
`Assets/Scripts/Kingdom/Game/Data/BarracksSoldierConfig.cs` (신규)

```csharp
using UnityEngine;

namespace Kingdom.Game
{
    [CreateAssetMenu(fileName = "BarracksSoldierConfig", menuName = "Kingdom/Game/Barracks Soldier Config")]
    public class BarracksSoldierConfig : ScriptableObject
    {
        [Header("Identity")]
        public string SoldierId = "BarracksSoldier_Default";
        public string DisplayName = "Barracks Soldier";

        [Header("Visual")]
        [Tooltip("Resources path without extension, e.g. Sprites/Barracks/Soldiers/BarracksSoldier_Default")]
        public string RuntimeSpriteResourcePath;
    }
}
```

### 4.2 TowerConfig 변경
`Assets/Scripts/Kingdom/Game/Data/TowerConfig.cs`

변경 포인트:
1. `BarracksData` 내부 `SoldierSpriteResourcePath`는 제거 대상
2. `TowerConfig`에 `BarracksSoldierConfig` 참조 필드 추가
3. 과도기에는 legacy fallback 필드를 읽기 전용으로 유지

권장 형태:

```csharp
[Serializable]
public struct BarracksData
{
    public int SquadSize;
    public float RallyRange;
    public float SoldierMaxHp;
    public float SoldierDamage;
    public float SoldierAttackCooldown;
    public float SoldierRespawnSec;

    [HideInInspector] public string LegacySoldierSpriteResourcePath; // migration only
}

public class TowerConfig : ScriptableObject
{
    public BarracksSoldierConfig BarracksSoldierConfig; // 신규 추가
    public BarracksData BarracksData;
}
```

> **Candidate 필드 매핑** (M1 단계에서 자동화):
> - 현재 필드: `BarracksData.SoldierSpriteResourcePath` → 이관 후 `LegacySoldierSpriteResourcePath`로 리네임
> - 현재는 `TowerConfig`에 `BarracksSoldierConfig` 필드가 **없음** → Phase D/F에서 추가

### 4.3 런타임 로딩 우선순위
배럭 병사 스프라이트 로딩 우선순위:
1. `TowerConfig.BarracksSoldierConfig.RuntimeSpriteResourcePath`
2. `TowerConfig.BarracksData.LegacySoldierSpriteResourcePath` (마이그레이션 기간)
3. 기존 관용 경로 후보
4. fallback sprite (strict 모드에서는 실패 처리)

## 5. AISpriteProcessor 탭 설계
### 5.1 Common 탭
- 기존 처리/슬라이싱 기능 유지
- 기존 preset/preview/smart slice 로직 유지

### 5.2 BaseUnit 탭
대상 타입:
1. `HeroConfig`
2. `EnemyConfig`
3. `BarracksSoldierConfig`

필수 입력:
1. Target Type
2. Target Asset
3. Source Texture
4. Action Group(상태형 에셋일 때)

버튼:
1. `Process & Slice`
2. `Apply Binding`
3. `Process + Apply`
4. `Next Target`
5. `Validate Current`

### 5.3 TowerBase 탭
대상:
1. `TowerConfig` (타워 본체만)

필수 입력:
1. Target Asset
2. Bind Mode: `Tower Level`
3. Level Index
4. Source Texture

버튼:
1. `Process & Slice`
2. `Apply Binding`
3. `Process + Apply`
4. `Validate Tower`

### 5.4 에디터 UX 및 상태 유지 (UX & State Persistence)
- **상태 보존**: 탭 전환 시 작업 컨텍스트가 날아가지 않도록, 지정된 `Target Asset` 슬롯과 `Action Group` 설정은 `AISpriteProcessor` 인스턴스 뷰 내에서 탭 객체 단위로 상태를 유지해야 한다.
- **텍스처 임포트 세팅**: `Source Texture` 할당 시 `isReadable` 옵션이 켜져 있는지 확인하고, 꺼져있다면 UI에 경고 표시 및 자동 Fix 버튼을 제공하여 전처리 예외를 최소화한다.
- **덮어쓰기 경고**: `Apply Binding` 시 이미 타겟 에셋 필드에 유효한 경로가 할당되어 있다면, 덮어쓸 것인지 확인하는 다이얼로그(`EditorUtility.DisplayDialog`)를 띄워 실수를 방지한다.
- **Dirty State 시각화**: 에셋 바인딩이 아직 저장(`SaveAssets`)되지 않은 상태인 경우, 해당 탭 상단에 "[Unsaved Changes]" 등 시각적 힌트를 제공한다.


## 6. 바인딩 규칙
### 6.1 HeroConfig
1. 키: `HeroConfig.HeroId`
2. 상태: `idle`, `walk`, `attack`, `die` (추후 `cast`, `special` 등 확장 가능하도록 Enum 분리 설계)
3. 기본 경로: `UI/Sprites/Heroes/InGame/{HeroId}/`
4. 보조: `Sprites/Heroes/manifest` 사용
5. 규칙: 출력 파일명 또는 manifest 메타에 `HeroId` 식별자가 포함되어야 한다.

**중요: 히어로 스프라이트 로딩 구조** (코드 기준):
- `HeroConfig`에는 `RuntimeSpriteResourcePath` 필드가 **없다** (단일 필드 바인딩 없음).
- `HeroController`가 내부 상수로 경로를 조합하여 프레임 시퀀스 로딩: `{prefix}{HeroId}/{action}_{00..31}`
- 1차 로딩 실패 시 `Sprites/Heroes/manifest` JSON에서 `actionGroup+HeroId` 매칭으로 폴백.
- 따라서 본 툴의 Hero 바인딩은 **파일 배치 + manifest 갱신**이 핵심이지, SO 필드에 경로를 쓰는 것이 아니다.

### 6.2 EnemyConfig
1. 키: `EnemyConfig.EnemyId`
2. 필드: `EnemyConfig.RuntimeSpriteResourcePath`
3. 권장 경로: `Sprites/Enemies/{EnemyId}`

### 6.3 BarracksSoldierConfig
1. 키: `BarracksSoldierConfig.SoldierId`
2. 필드: `BarracksSoldierConfig.RuntimeSpriteResourcePath`
3. 권장 경로: `Sprites/Barracks/Soldiers/{SoldierId}`

### 6.4 TowerConfig
1. 필드: `TowerConfig.Levels[level].SpriteResourcePath`
2. 옵션: `TowerConfig.RuntimeSpriteResourcePath` 템플릿 (`{tower}`, `{level}`)
3. 레벨 매핑: UI에서 Level 1~3 모드를 선택하면 `Levels` 배열 인덱스 0~2에 매핑하여 적용한다.
   - 현재 `TowerConfig.Levels` 기본 크기는 **3** (코드: `new TowerLevelData[3]`).
   - 4티어 확장 시 배열 크기를 동적으로 늘리는 마이그레이션 또는 검증 로직이 필요하다.
4. 배럭 병사 경로는 여기서 수정하지 않는다.

## 7. 검증 및 실패 기준
### 7.1 에디터 검증
- 탭별 `Validate` 버튼에서 다음을 검사한다.
1. Target Asset null 여부
2. 필수 식별자(`HeroId`, `EnemyId`, `SoldierId`) 공백 여부
3. 바인딩된 Resources 경로 체크: 에디터 환경이므로 파일 I/O나 `AssetDatabase` 기반 역추적을 사용하여 실제 에셋 파일 확장자(`.png` 등) 유무를 파일 시스템 수준에서 사전 검출한다.
4. Hero 상태 최소 4종(idle/walk/attack/die) 모두 해상 가능 여부
- **시각적 피드백**: 검증 통과 시 GUI 요소를 녹색(Green) 마크업으로 처리하고, 누락 발생 시 붉은색(Red) 경고 메시지와 누락된 세부 경로 트리를 `HelpBox` 형태로 렌더링한다.

### 7.2 런타임 검증
- `GameScene` strict 검증 기준과 동일하게 유지한다.
- 누락 시 동작:
1. 누락 목록 전체 로그 출력
2. 씬 시작 중단
3. 에디터 플레이 강제 종료

## 8. 마이그레이션 계획
### 8.0 사전 백업 절차 (필수)
1. 실행 전 백업 루트: `Assets/Resources/_Backup/Migration_{yyyyMMdd_HHmmss}/`
2. 백업 대상:
- `Assets/Resources/Data/TowerConfigs/*.asset`
- `Assets/Resources/Kingdom/**/Config/*.asset`
- `Assets/Resources/Sprites/**`
3. 자동 백업 실패 시 마이그레이션 실행 금지(즉시 중단).
4. 복구 절차:
- 자동 복구: 백업 경로에서 원본 위치로 복사 후 `AssetDatabase.Refresh()`
- 수동 복구: Git 기준으로 해당 경로만 restore 후 재실행

### M0. 준비
1. `BarracksSoldierConfig` 클래스 추가
2. 런타임 로더에 신규 참조 우선순위 지원

### M1. 데이터 이관
1. 기존 `TowerConfig.BarracksData.SoldierSpriteResourcePath` 값을 읽어 `BarracksSoldierConfig` 생성
2. 생성된 SO를 `TowerConfig.BarracksSoldierConfig`에 연결
3. 이관 리포트 로그 출력

### M2. 과도기 운영
1. legacy 필드는 읽기 전용 fallback으로만 사용
2. 신규 입력은 모두 `BarracksSoldierConfig`로만 받음

### M3. 제거
1. legacy 필드 제거
2. 참조 누락 TowerConfig 검증 실패 처리

### M4. 롤백 게이트
1. 누락 참조가 1건 이상 발생하면 legacy 제거 단계 중단.
2. 복구는 8.0 백업본을 우선 사용하고, 실패 시 Git restore 경로로 전환.
3. 롤백 후 원인 로그(`failed asset path`, `missing reference`)를 리포트에 남긴다.

## 9. 구현 단계
1. Phase A: 탭 골격 분리 (`Common/BaseUnit/TowerBase`)
2. Phase B: BaseUnit + HeroConfig 바인딩
3. Phase C: BaseUnit + EnemyConfig 바인딩
4. Phase D: `BarracksSoldierConfig` 생성 및 BaseUnit 바인딩
5. Phase E: TowerBase + TowerLevel 바인딩
6. Phase F: 마이그레이션 툴/자동 이관 구현
7. Phase G: 에디터 Validate/로그 UX 보강
8. Phase H: 런타임 strict 연동 검증 및 회귀 테스트

## 10. 완료 기준 (DoD)
1. `BaseUnit` 탭에서 Hero/Enemy/BarracksSoldier 각각 `Process + Apply`가 동작한다.
2. `TowerBase` 탭에서 타워 레벨 스프라이트 바인딩이 동작한다.
3. `TowerConfig`가 배럭 병사 이미지 경로를 직접 소유하지 않는다.
4. 마이그레이션 후 기존 데이터가 손실 없이 신규 구조로 전환된다.
5. 검증 실패 시 에디터/런타임 모두 누락이 즉시 드러난다.

## 11. 리스크 및 대응
1. Hero 식별 충돌
- 대응: 파일명/manifest 모두 `HeroId` 강제 포함

2. 경로 규칙 혼재
- 대응: 권장 경로 템플릿 고정 + Apply 전 최종 경로 프리뷰 표시

3. 마이그레이션 누락
- 대응: 이관 결과 리포트(성공/실패 수치) 필수 출력

4. 회귀 리스크
- 대응: `Common` 탭 기존 로직 비변경, 신규 로직은 탭 분기 내부로 격리

## 12. 즉시 다음 작업
1. `Phase A`: `AISpriteProcessor` 탭 분리 코드 추가
2. `Phase B`: HeroConfig `Process + Apply` 구현
3. `Phase C`: EnemyConfig `Process + Apply` 구현

## 13. 경로 컨벤션 (권장)
1. Hero
- `UI/Sprites/Heroes/InGame/{HeroId}/{action}_00` (연속 프레임)
- 예시: `UI/Sprites/Heroes/InGame/MageHero/attack_00`

2. Enemy
- `Sprites/Enemies/{EnemyId}`
- 예시: `Sprites/Enemies/Goblin`

3. Barracks Soldier
- `Sprites/Barracks/Soldiers/{SoldierId}`
- 예시: `Sprites/Barracks/Soldiers/BarracksSoldier_Default`

4. Tower Level
- `Sprites/Towers/{TowerType}/L{level}`
- 예시: `Sprites/Towers/Archer/L1`

## 13.1 경로/파일명 충돌 정책 (필수)
1. 동일 `HeroId/EnemyId/SoldierId`가 동일 출력 경로를 공유하면 기본 정책은 `Reject`(덮어쓰기 금지)다.
2. 충돌 시 `Apply`를 중단하고, 사용자에게 `Overwrite` 확인 다이얼로그를 표시한다.
3. `Process + Apply` 자동 모드에서는 충돌 시 항상 중단하고 수동 확인을 요구한다.
4. `Overwrite`를 수행한 경우 기존 경로와 신규 경로를 로그로 남긴다.

## 14. 탭별 샘플 워크플로우
### 14.1 BaseUnit - HeroConfig
1. `Target Type = HeroConfig` 선택
2. `Target Asset = MageHero.asset` 지정
3. `Action Group = idle` 지정 후 `Process + Apply`
4. `walk`, `attack`, `die` 순서로 반복
5. `Validate Current`로 4개 상태 해상 여부 확인

### 14.2 BaseUnit - EnemyConfig
1. `Target Type = EnemyConfig` 선택
2. `Target Asset` 지정
3. `Process + Apply`
4. `RuntimeSpriteResourcePath` 자동 반영 확인

### 14.3 BaseUnit - BarracksSoldierConfig
1. `Target Type = BarracksSoldierConfig` 선택
2. `Target Asset` 지정
3. `Process + Apply`
4. `RuntimeSpriteResourcePath` 자동 반영 확인

### 14.4 TowerBase - TowerConfig
1. `Target Asset = Archer.asset` 지정
2. `Level Index = 1` 지정 후 `Process + Apply`
3. L2, L3 반복 적용
4. `Validate Tower`로 레벨별 경로 확인

## 15. 구현 체크리스트 (개발용)
1. Phase A
- [ ] `TabMode` enum 추가
- [ ] `DrawCommonTab/DrawBaseUnitTab/DrawTowerBaseTab` 분리
- [ ] 탭별 컨텍스트 객체 분리

2. Phase B
- [ ] Hero 바인딩 함수 구현
- [ ] ActionGroup 필수 검증 구현
- [ ] Hero 4상태 Validate 구현

3. Phase C
- [ ] Enemy 바인딩 함수 구현
- [ ] 경로 자동 생성 규칙 적용

4. Phase D
- [ ] `BarracksSoldierConfig` SO 생성
- [ ] Barracks soldier 바인딩 함수 구현

5. Phase E
- [ ] Tower level 바인딩 함수 구현
- [ ] `Level Index` UI/배열 매핑 검증

6. Phase F
- [ ] 마이그레이션 메뉴 구현
- [ ] legacy -> 신규 참조 자동 이관
- [ ] 이관 결과 로그(성공/실패/스킵) 출력

7. Phase G/H
- [ ] Validate 결과 패널 정리
- [ ] 런타임 strict 회귀 테스트
- [ ] 샘플 에셋 스모크 테스트

## 16. 롤백 기준
1. 런타임에서 타워/병사 스프라이트 누락이 증가하면 즉시 legacy fallback 경로를 임시 재활성화한다.
2. 마이그레이션 후 참조 유실이 발견되면 이관 스크립트 재실행 전 자동 백업 에셋으로 복원한다.
3. `Common` 탭 기존 기능 회귀가 발생하면 탭 분리 커밋을 우선 롤백하고 바인딩 기능은 별도 브랜치에서 재검증한다.

---

## Appendix A. 현재 코드베이스 상태 요약 (Code-Verified)

> 구현 전 주의사항을 정리한다. 코드 확인 시점: 2026-02-20.

### A.1 HeroConfig (`HeroConfig.cs`)
| 항목 | 상태 |
|---|---|
| `RuntimeSpriteResourcePath` 필드 | **없음** |
| 스프라이트 로딩 방식 | `HeroController` 내부 상수 경로 기반 프레임 시퀀스 로딩 |
| 1차 로딩 | `UI/Sprites/Heroes/InGame/{HeroId}/{action}_{00..31}` |
| fallback 로딩 | `Sprites/Heroes/manifest` JSON 매칭 |
| 바인딩 전략 | SO 필드 업데이트 없음 → 파일 배치 + manifest 갱신이 핵심 |

### A.2 EnemyConfig (`EnemyConfig.cs`)
| 항목 | 상태 |
|---|---|
| `RuntimeSpriteResourcePath` | **있음** (string 필드) |
| `EnemyId` | **있음** (string) |
| 바인딩 전략 | SO 필드 직접 업데이트 가능 |

### A.3 BarracksSoldierConfig
| 항목 | 상태 |
|---|---|
| SO 클래스 | **미존재** → Phase D에서 신규 생성 |
| 현재 병사 데이터 위치 | `TowerConfig.BarracksData` 내부 인라인 |

### A.4 TowerConfig (`TowerConfig.cs`)
| 항목 | 상태 |
|---|---|
| `Levels` 배열 기본 크기 | **3** (`new TowerLevelData[3]`) |
| `Levels[n].SpriteResourcePath` | **있음** (string) |
| `Levels[n].SpriteOverride` | **있음** (Sprite 직접 참조) |
| `RuntimeSpriteResourcePath` | **있음** (템플릿 지원 `{tower}`,`{level}`) |
| `BarracksData.SoldierSpriteResourcePath` | **있음** → legacy 제거 대상 |
| `BarracksSoldierConfig` 참조 필드 | **없음** → Phase D/F에서 추가 |

### A.5 BarracksSoldierRuntime (`BarracksSoldierRuntime.cs`)
| 항목 | 상태 |
|---|---|
| 상속 | `BaseUnit` |
| 상태 enum | `Idle/Moving/Blocking/Dead/Respawning` |
| 스프라이트 자체 로딩 | **없음** — `Initialize()`에서 `SpriteRenderer` 외부 주입 |
| 스프라이트 설정 주체 | `TowerManager`가 `BarracksData.SoldierSpriteResourcePath`로 로드 후 전달 |

### A.6 HeroController (`HeroController.cs`)
| 항목 | 상태 |
|---|---|
| 상속 | `MonoBehaviour, IDamageable` (비-BaseUnit) |
| 애니메이션 지원 | 4상태 프레임 시퀀스 (idle/walk/attack/die) + manifest fallback |
| 최대 프레임 | 32프레임 |
| 기본 FPS | 10 |

---

## Appendix B. 변경 파일 매트릭스 (Planned)

| 구분 | 파일 | 작업 | 비고 |
|---|---|---|---|
| 신규 | `Assets/Scripts/Kingdom/Game/Data/BarracksSoldierConfig.cs` | SO 타입 추가 | 배럭 병사 비주얼 소유 |
| 수정 | `Assets/Scripts/Kingdom/Game/Data/TowerConfig.cs` | `BarracksSoldierConfig` 참조 필드 추가 | legacy 경로는 과도기 유지 |
| 수정 | `Assets/Scripts/Kingdom/Game/TowerManager.cs` | 병사 스프라이트 로딩 우선순위 변경 | 신규 참조 우선 |
| 수정 | `Assets/Scripts/Common/Editor/AISpriteProcessor.cs` | 탭 분리 + 바인딩 레이어 + Validate | 핵심 대상 |
| 신규(선택) | `Assets/Scripts/Kingdom/Editor/KingdomSpriteBindingTools.cs` | 마이그레이션/검증 헬퍼 확장 | 기존 툴 재사용 가능 |
| 수정(필요 시) | `Assets/Scripts/Kingdom/App/GameScene.cs` | strict 검증 항목 확장 | BarracksSoldierConfig 검증 추가 |

## Appendix C. Phase별 게이트(Go/No-Go)

### C.1 Phase A 종료 조건
1. 탭 전환 후 기존 Common 처리 기능이 동일 동작한다.
2. 탭별 컨텍스트 오염(값 섞임)이 발생하지 않는다.
3. 컴파일 에러 0.
4. 검증 명령:
- Unity 컴파일: `refresh_unity(compile=request)`
- 스크립트 검사: `validate_script` 대상 `Assets/Scripts/Common/Editor/AISpriteProcessor.cs`

### C.2 Phase B/C 종료 조건
1. Hero/Enemy 각각 `Process + Apply` 1회로 경로 반영 가능.
2. 잘못된 Target Type 선택 시 Apply 버튼 비활성화.
3. Undo/Redo로 바인딩 변경 취소/복구 가능.
4. 검증 명령:
- 에디터 Validate 결과 `missing=0`
- 런타임 strict 실행 시 씬 중단 없음

### C.3 Phase D/F 종료 조건
1. 최소 1개 TowerConfig가 신규 `BarracksSoldierConfig` 참조로 이관됨.
2. 이관 로그에 성공/실패/스킵 건수가 출력됨.
3. legacy 경로 없이도 병사 스프라이트가 정상 로딩됨.
4. 검증 명령:
- 마이그레이션 로그: `MigrationSummary created/linked/skipped/failed`
- 샘플 플레이 1회에서 병사 렌더 정상 확인

### C.4 최종 배포 게이트
1. 런타임 strict 검증에서 누락 0.
2. 스모크 테스트(영웅/적/배럭병사/타워) 통과.
3. 회귀 항목(Common 탭, 기존 슬라이싱 결과)이 기준과 동일.
4. 검증 명령:
- `Tools/Kingdom/Sprites/Validate Runtime Sprite Bindings` 실행 결과 `missing=0`
- 런타임 로그에서 `Required runtime data is missing` 미출력

## Appendix D. 검증 시나리오 확장

1. Hero 상태 누락 테스트
- `attack`만 제거 후 `Validate Current` 실행 시 누락 경고가 정확히 `action=attack`으로 표시되어야 한다.

2. Enemy 경로 오타 테스트
- `RuntimeSpriteResourcePath`를 존재하지 않는 경로로 설정 후 Validate 시 해당 경로가 그대로 표시되어야 한다.

3. Barracks 참조 누락 테스트
- `TowerConfig.BarracksSoldierConfig = null` 상태에서 strict 실행 시 씬 시작 중단이 발생해야 한다.

4. Tower 레벨 인덱스 초과 테스트
- `Levels.Length = 3`일 때 UI에서 `Level 4`를 선택할 수 없어야 하며, 강제 입력 시 Validate 실패가 나와야 한다.

5. 마이그레이션 재실행 테스트
- 이미 이관된 자산에 대해 재실행 시 중복 SO가 생성되지 않고 `Skip`으로 집계되어야 한다.

## Appendix E. 로그 포맷 기준

1. 바인딩 성공
- `[AISpriteProcessor] BindSuccess type={type} asset={assetPath} field={field} path={resourcePath}`

2. 바인딩 실패
- `[AISpriteProcessor] BindFailed type={type} asset={assetPath} reason={reason}`

3. 검증 요약
- `[AISpriteProcessor] ValidateSummary scope={scope} checked={n} resolved={n} missing={n}`

4. 마이그레이션 요약
- `[AISpriteProcessor] MigrationSummary tower={n} created={n} linked={n} skipped={n} failed={n}`
