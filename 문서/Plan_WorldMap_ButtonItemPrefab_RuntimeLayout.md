# 공용 아이콘 버튼 위젯 프리팹 + 런타임 배치 계획 (개선안)

- 작성일: 2026-02-14
- 목적: 월드맵 전용 구현을 제거하고, 어떤 화면에서도 재사용 가능한 `텍스트 + 아이콘 버튼` 위젯을 표준화

---

## 1) 배경 및 문제
현재 월드맵(`WorldMapView`)에서는 버튼(`btnHeroRoom`, `btnUpgrades`)과 라벨(`lblHero`, `lblUpgrade`)이 분리되어 있어 다음 문제가 있음.

1. 버튼과 라벨이 다른 오브젝트로 관리되어 위치/정렬 동기화가 어렵다.
2. 화면별로 유사 UI를 다시 만들어 중복 코드가 발생한다.
3. 새 버튼 추가 시 오브젝트 생성 + 바인딩 + 이벤트 연결이 반복된다.

핵심 개선 방향은 `버튼+텍스트`를 하나의 위젯 프리팹으로 통합하고, 코드에서 데이터만 주입해 어디서든 사용하게 만드는 것이다.

---

## 2) 목표 (Scope)

### In Scope
1. 공용 위젯 프리팹/스크립트 제작
2. 월드맵에 1차 적용 (Hero/Upgrade)
3. 월드맵 외 1개 화면 이상에 재사용 적용
4. 런타임 코드 기반 배치/설정/이벤트 연결 구조 확정

### Out of Scope
1. 모든 기존 버튼 즉시 교체
2. 월드맵 전체 레이아웃 재디자인
3. 아이콘 아트 리소스 재생성

---

## 3) 공용 컴포넌트 설계

### 3.1 이름/경로
- 컴포넌트명: `IconButtonWidget`
- 스크립트: `Assets/Scripts/Common/UI/Widgets/IconButtonWidget.cs`
- 프리팹: `Assets/Resources/UI/Widgets/IconButtonWidget.prefab`
- 네임스페이스: `Common.UI.Widgets` (전역 재사용 목적)

### 3.2 프리팹 구조

```text
IconButtonWidget (RectTransform + IconButtonWidget)
├── lblTitle   (TextMeshProUGUI)
└── btnIcon    (Button + Image + AspectRatioFitter)
```

### 3.3 기본 레이아웃 권장값
- 루트: `200 x 250`
- 라벨 영역: 높이 `40`
- 버튼 영역: `180 x 180`, 1:1 고정
- 아이콘: `preserveAspect = true`

### 3.4 API 설계

```csharp
public struct IconButtonWidgetData
{
    public string Title;
    public Sprite Icon;
    public TMP_FontAsset Font;
    public bool Interactable;
}

public struct IconButtonWidgetLayout
{
    public Vector2 AnchorMin;
    public Vector2 AnchorMax;
    public Vector2 AnchoredPosition;
    public Vector2 SizeDelta;
}

public sealed class IconButtonWidget : MonoBehaviour
{
    public void SetData(in IconButtonWidgetData data);
    public void SetOnClick(UnityAction onClick);
    public void SetLayout(in IconButtonWidgetLayout layout);
}
```

원칙:
- 위젯은 UI 표현과 클릭 전달만 담당
- 클릭 이후 비즈니스 로직은 화면(`WorldMapView`, `TitleView`, `ShopView`)이 담당

---

## 4) 생성/배치 전략

### 4.1 로드
- `Resources.Load<IconButtonWidget>("UI/Widgets/IconButtonWidget")`

### 4.2 배치
- 화면에서 부모 컨테이너(`Transform parent`)를 넘겨서 인스턴스 생성
- 각 화면에서 레이아웃 값만 다르게 전달

### 4.3 중복 생성 방지
- 런타임 루트(`RuntimeButtons`)를 두고 존재 시 재생성 금지
- 씬 재진입 시 기존 런타임 인스턴스 정리/재사용 정책 명확화

---

## 5) 마이그레이션 단계

### Phase 1: 공용 위젯 제작
1. `IconButtonWidget.cs` 작성
2. `IconButtonWidget.prefab` 생성 및 연결
3. 기본 스타일(폰트/색/Transition) 세팅

### Phase 2: 월드맵 적용
1. `WorldMapView`에 `CreateBottomButtons()` 추가
2. Hero/Upgrade를 `IconButtonWidget`으로 교체
3. 기존 `lblHero/lblUpgrade/btnHeroRoom/btnUpgrades`는 전환 기간 비활성 유지

### Phase 3: 재사용 검증
1. 월드맵 외 1개 화면(예: `TitleView` 또는 `ShopUI`)에 동일 위젯 적용
2. 화면별 배치/문구/아이콘/콜백 주입 방식 검증

### Phase 4: 정리
1. 구식 필드/오브젝트 참조 제거
2. 중복 코드 삭제
3. 문서/주석 업데이트

---

## 6) WorldMapView 적용 가이드 (초안)

```csharp
private IconButtonWidget heroWidget;
private IconButtonWidget upgradeWidget;

private void CreateBottomButtons()
{
    var prefab = Resources.Load<IconButtonWidget>("UI/Widgets/IconButtonWidget");
    if (prefab == null)
    {
        Debug.LogError("[WorldMapView] IconButtonWidget prefab not found.");
        return;
    }

    heroWidget = Instantiate(prefab, bottomBarTransform);
    heroWidget.SetData(new IconButtonWidgetData
    {
        Title = "HERO",
        Icon = heroIcon,
        Font = defaultFont,
        Interactable = true
    });
    heroWidget.SetOnClick(OnClickHeroRoom);

    upgradeWidget = Instantiate(prefab, bottomBarTransform);
    upgradeWidget.SetData(new IconButtonWidgetData
    {
        Title = "UPGRADE",
        Icon = upgradeIcon,
        Font = defaultFont,
        Interactable = true
    });
    upgradeWidget.SetOnClick(OnClickUpgrades);
}
```

---

## 7) 검증 계획

### 자동 검증
1. 컴파일 에러/경고 확인
2. Missing Reference/NullReference 로그 확인

### 수동 검증
1. `1920x1080`, `1600x900`, `1280x720`에서 위치 정상 확인
2. 버튼 클릭 콜백 동작 확인
3. 씬 재진입 시 중복 생성/유실 없는지 확인
4. 월드맵 외 적용 화면에서 동일 위젯 정상 동작 확인

### 스크린샷
- Scene 뷰 1장
- Game 뷰 1장
- 재사용 화면 Game 뷰 1장

---

## 8) 리스크 및 대응

1. 리스크: 화면별 요구사항 차이(텍스트 위치/버튼 크기)
- 대응: `IconButtonWidgetLayout` 분리, 화면별 레이아웃 주입

2. 리스크: 기존 구조와 병행 시 중복 표시
- 대응: 전환 기간 비활성 정책 + 런타임 생성 가드

3. 리스크: Resources 경로 오타
- 대응: 로드 실패 시 즉시 `Debug.LogError` 및 안전 폴백

4. 리스크: 폰트/아이콘 누락
- 대응: 기본 폰트/기본 아이콘 폴백 규칙 정의

---

## 9) 완료 기준 (DoD)
1. `IconButtonWidget` 공용 프리팹/스크립트가 생성되어 있다.
2. 월드맵 Hero/Upgrade가 공용 위젯으로 교체되어 있다.
3. 월드맵 외 최소 1개 화면에서 동일 위젯을 재사용한다.
4. 콘솔 에러(Null/Missing) 없이 동작한다.
5. 검증 스크린샷이 문서화되어 있다.