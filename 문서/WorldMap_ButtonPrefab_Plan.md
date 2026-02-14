# 공용 UI 액션 버튼 아이템 구현 계획 (UIActionButtonItem)

## 1. 개요
*   **목적**: 월드맵 등 다양한 화면에서 사용 가능한 "아이콘 버튼 + 상단 라벨" 형태의 재사용 가능한 공용 UI 컴포넌트 구현
*   **변경 사항**: 기존 `WorldMapView` 내 개별 오브젝트(`btnHeroRoom`, `lblHero` 등)를 제거하고, 런타임에 공용 프리팹을 로드하여 배치하는 방식으로 전환
*   **기대 효과**: UI 일관성 유지, 유지보수 용이성 확보, 중복 코드 제거

## 2. 기존 계획 대비 개선점 (Codex 반영)
*   **범용성 강화**: `Kingdom` 네임스페이스 대신 `Common` 네임스페이스 사용
*   **명확한 명명**: `IconButtonWidget` -> `UIActionButtonItem`
*   **설정 객체 도입**: `SetData(string, sprite, action)` 대신 `UIActionButtonItemConfig` 구조체를 사용하여 확장성 확보
*   **표준 경로 준수**: `Assets/Resources/UI/Components/Common/` 경로 사용

## 3. 구현 상세

### 3.1. 스크립트 (Common 영역)

#### [NEW] `Assets/Scripts/Common/UI/Components/UIActionButtonItem.cs`
```csharp
namespace Common.UI.Components
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;

    public class UIActionButtonItem : BaseWidget
    {
        [Header("Components")]
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image buttonBg; // 버튼 배경 (필요시)

        public void Init(UIActionButtonItemConfig config, UnityAction onClick)
        {
            if (label != null) label.text = config.LabelText;
            if (icon != null) icon.sprite = config.IconSprite;
            
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                if (onClick != null) button.onClick.AddListener(onClick);
            }
        }
        
        // 레이아웃/스타일 오버라이드 기능 (옵션)
        public void SetLayout(float width, float height)
        {
            var rect = GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(width, height);
        }
    }
}
```

#### [NEW] `Assets/Scripts/Common/UI/Components/UIActionButtonItemConfig.cs`
```csharp
namespace Common.UI.Components
{
    using UnityEngine;

    [System.Serializable]
    public struct UIActionButtonItemConfig
    {
        public string LabelText;
        public Sprite IconSprite;
        // 추후 폰트, 색상 등 스타일 옵션 추가 가능
        
        public UIActionButtonItemConfig(string label, Sprite icon)
        {
            LabelText = label;
            IconSprite = icon;
        }
    }
}
```

### 3.2. 프리팹 (Resources 영역)

*   **경로**: `Assets/Resources/UI/Components/Common/UIActionButtonItem.prefab`
*   **생성 방식**: 일회성 에디터 스크립트(`Disposable Tool`) 사용
*   **구조**:
    ```
    UIActionButtonItem (RectTransform: 240x240)
    ├── Label (TMP: Top Anchor, h:44, "Maplestory Bold SDF")
    └── ButtonRoot (Button+Image: 180x180, Center)
        └── Icon (Image: AspectRatioFitter)
    ```

### 3.3. 적용 (WorldMapView 수정)

*   **파일**: `Assets/Scripts/Kingdom/UI/WorldMapView.cs`
*   **수정 내용**:
    *   기존 `SerializedField` 제거 (`btnHeroRoom`, `btnUpgrades`, `lblHero`, `lblUpgrade`)
    *   `BottomBar` 컨테이너 하위에 `UIActionButtonItem` 런타임 생성
    *   `UIActionButtonItemConfig`를 사용하여 데이터 주입

```csharp
// WorldMapView.cs 예시
[SerializeField] private Transform bottomBarContainer; // 기존 BottomBar 참조

private void CreateBottomButtons()
{
    var prefab = Resources.Load<UIActionButtonItem>("UI/Components/Common/UIActionButtonItem");
    
    // 영웅 관리소 버튼
    var heroBtn = Instantiate(prefab, bottomBarContainer);
    var heroConfig = new UIActionButtonItemConfig("영웅 관리소", Resources.Load<Sprite>("UI/Sprites/WorldMap/Icon_Hero"));
    heroBtn.Init(heroConfig, OnClickHeroRoom);

    // 업그레이드 버튼
    var upgradeBtn = Instantiate(prefab, bottomBarContainer);
    var upgradeConfig = new UIActionButtonItemConfig("업그레이드", Resources.Load<Sprite>("UI/Sprites/WorldMap/Icon_Upgrade"));
    upgradeBtn.Init(upgradeConfig, OnClickUpgrades);
}
```

## 4. 작업 단계 (Step-by-Step)

1.  **스크립트 작성**: `UIActionButtonItem.cs`, `UIActionButtonItemConfig.cs`
2.  **프리팹 생성 툴 작성**: `Assets/Scripts/Common/Editor/UIActionButtonItemMaker.cs`
3.  **프리팹 생성 실행**: 메뉴를 통해 프리팹 생성 후 툴 삭제
4.  **WorldMapView 리팩토링**: 기존 버튼 참조 제거 및 런타임 생성 로직 구현
5.  **기존 씬/프리팹 정리**: WorldMapView 프리팹에서 구버전 객체(`btnHeroRoom` 등) 제거
6.  **검증**: 게임 실행 후 버튼 표시 및 클릭 동작 확인

## 5. 검증 계획
*   [ ] `Assets/Scripts/Common/UI/Components/` 경로에 스크립트 생성 확인
*   [ ] `Assets/Resources/UI/Components/Common/` 경로에 프리팹 생성 확인
*   [ ] WorldMap 씬 진입 시 하단 버튼 2개(영웅, 업그레이드)가 정상 배치되는지 확인
*   [ ] 버튼 클릭 시 Toast 메시지("준비 중입니다!") 출력 확인
