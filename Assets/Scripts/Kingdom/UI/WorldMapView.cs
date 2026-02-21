using System;
using System.Collections.Generic;
using Common.Extensions;
using Common.UI;
using Common.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Common.UI.Components;
using Kingdom.WorldMap;
using Kingdom.Save;

namespace Kingdom.App
{
    /// <summary>
    /// 월드맵 뷰. Resources의 UI/오디오 리소스를 바인딩하고 노드 상태를 반영한다.
    /// </summary>
    public class WorldMapView : BaseView
    {
        private const string WorldMapBackgroundResourcePath = "UI/Sprites/WorldMap/WorldMap_Background";
        private const string LegacyWorldMapBackgroundResourcePath = "UI/Sprites/WorldMap_BG";
        private const string WorldMapBgmResourcePath = "Audio/BGM/BGM_WolrdMapScene";
        private const string WorldMapClickResourcePath = "Audio/SFX/UI_Common_Click";
        private const string WorldMapButtonCatalogResourcePath = "Data/UI/WorldMapButtonCatalog";
        private const string WorldMapStageConfigResourcePath = "Kingdom/Configs/Stages/World1_StageConfig";
        private const string UpgradesPopupResourcePath = "UI/WorldMap/UpgradesPopup";
        private const string HeroRoomPopupResourcePath = "UI/WorldMap/HeroRoomPopup";
        // 오디오 옵션 팝업 프리팹 경로 (없으면 런타임 폴백 생성)
        private const string AudioOptionsPopupResourcePath = "UI/WorldMap/AudioOptionsPopup";
        private const string HeroRoomActionId = "hero_room";
        private const string UpgradesActionId = "upgrades";
        private const string AudioOptionsActionId = "audio_options";
        private const string HeroRoomLabel = "\uC601\uC6C5 \uAD00\uB9AC\uC18C";
        private const string UpgradeLabel = "\uC5C5\uADF8\uB808\uC774\uB4DC";
        private const string AudioOptionsLabel = "설정";
        private static readonly string[] AudioOptionsButtonCandidateNames =
        {
            "btnAudioOptions",
            "btn_audio_options",
            "btn_settings",
            "btn_option",
            "btn_option_audio",
        };

        [Header("WorldMap UI")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button btnBack;

        [Header("Containers")]
        [SerializeField] private Transform bottomBar;
        [SerializeField] private RectTransform safeAreaTarget;
        [SerializeField] private RectTransform safeAreaContent;
        [SerializeField] private bool useSafeArea = true;
        [SerializeField, Range(0f, 64f)] private float bottomBarLift = 10f;
        [SerializeField] private ViewportSafeAreaLayout viewportLayout;

        [Header("Side Border")]
        [SerializeField] private RectTransform sideBordersRoot;
        [SerializeField] private float sideBorderWidth = 280f;
        [SerializeField] private float minSideBorderWidth = 220f;
        [SerializeField] private float maxSideBorderWidth = 340f;
        [SerializeField] private bool autoSideBorderWidthFromBackground = true;

        [SerializeField] UIActionButtonItem btnHeroRoom;
        [SerializeField] UIActionButtonItem btnUpgrades;
        [SerializeField] UIActionButtonItem btnAudioOptions;
        [SerializeField] private List<UIStageNode> stageNodes = new List<UIStageNode>();

        [Header("Overlay / Popup Routing")]
        [SerializeField] private GameObject overlayContainer;
        [SerializeField] private Button overlayDimButton;
        [SerializeField] private GameObject upgradesPopupRoot;
        [SerializeField] private GameObject heroRoomPopupRoot;
        [SerializeField] private GameObject audioOptionsPopupRoot;

        [Header("WorldMap Audio")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float clickVolumeScale = 0.9f;
        private Rect lastAppliedSafeArea = Rect.zero;
        private bool hasBottomBarBasePosition;
        private Vector2 bottomBarBaseAnchoredPosition;
        private readonly Dictionary<string, WorldMapButtonCatalogEntry> _buttonCatalogEntries = new Dictionary<string, WorldMapButtonCatalogEntry>(StringComparer.Ordinal);
        private WorldMapPresenter _stagePresenter;
        private GameObject _currentOverlayPopup;
        private readonly List<Button> _overlayCloseButtons = new List<Button>();

        [Serializable]
        private struct WorldMapButtonCatalogEntry
        {
            public string actionId;
            public string labelText;
            public string iconResourcePath;
        }

        [Serializable]
        private sealed class WorldMapButtonCatalogData
        {
            public WorldMapButtonCatalogEntry[] entries;
        }

        private void Awake()
        {
            if (backgroundImage == null && transform.Find("imgBackground"))
            {
                backgroundImage = transform.Find("imgBackground").GetComponent<Image>();
            }

            TryApplyWorldMapStructureFixup();

            if (safeAreaContent == null)
            {
                safeAreaContent = transform.Find("SafeAreaContent") as RectTransform;
            }

            if (bottomBar == null)
            {
                bottomBar = transform.Find("BottomBar");
            }

            if (btnBack == null && transform.Find("btnBack"))
            {
                btnBack = transform.Find("btnBack").GetComponent<Button>();
            }

            if (safeAreaTarget == null)
            {
                safeAreaTarget = transform as RectTransform;
            }

            if (viewportLayout == null)
            {
                viewportLayout = GetComponent<ViewportSafeAreaLayout>();
            }

            if (viewportLayout != null)
            {
                viewportLayout.SetSafeAreaTarget(safeAreaContent.IsNotNull() ? safeAreaContent : safeAreaTarget);
            }

            EnsureBackgroundImage();
            ApplySideBorderWidth();
            CacheBottomBarBasePosition();
            ApplyViewportLayout();
            EnsureOverlayRoutingBindings();
        }

        // 레거시 프리팹 구조에서도 런타임 시작 시 안전하게 월드맵 레이아웃을 보정한다.
        // 배경 이미지와 사이드보더는 safeAreaTarget에서 제외한다.
        private void TryApplyWorldMapStructureFixup()
        {
            EnsureSideBorders();
            ApplySideBorderWidth();
            EnsureSafeAreaContent();
        }

        private void EnsureSideBorders()
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            if (sideBordersRoot == null)
            {
                sideBordersRoot = transform.Find("SideBorders") as RectTransform;
            }

            if (sideBordersRoot == null)
            {
                var sideRoot = new GameObject("SideBorders", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                sideBordersRoot = sideRoot.GetComponent<RectTransform>();
                sideBordersRoot.SetParent(root, false);
                sideBordersRoot.SetSiblingIndex(1);
            }

            var sideImage = sideBordersRoot.GetComponent<Image>();
            if (sideImage == null)
            {
                sideImage = sideBordersRoot.gameObject.AddComponent<Image>();
            }

            sideImage.raycastTarget = false;
            sideImage.color = Color.clear;
            sideBordersRoot.anchorMin = Vector2.zero;
            sideBordersRoot.anchorMax = Vector2.one;
            sideBordersRoot.offsetMin = Vector2.zero;
            sideBordersRoot.offsetMax = Vector2.zero;

            EnsureSideBorderChild("LeftBorder", true);
            EnsureSideBorderChild("RightBorder", false);
        }

        private void EnsureSideBorderChild(string name, bool isLeft)
        {
            if (sideBordersRoot == null)
            {
                return;
            }

            var child = sideBordersRoot.Find(name);
            if (child == null)
            {
                var border = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child = border.transform;
                child.SetParent(sideBordersRoot, false);
            }

            var borderRect = child.GetComponent<RectTransform>();
            var borderImage = child.GetComponent<Image>();
            borderImage.raycastTarget = false;
            borderImage.color = borderImage.sprite != null ? Color.white : Color.clear;

            borderRect.anchorMin = isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            borderRect.anchorMax = isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            borderRect.pivot = isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            borderRect.sizeDelta = new Vector2(Mathf.Max(0f, sideBorderWidth), 0f);
            borderRect.anchoredPosition = Vector2.zero;
        }

        private void ApplySideBorderWidth()
        {
            if (sideBordersRoot == null)
            {
                return;
            }

            if (minSideBorderWidth < 0f)
            {
                minSideBorderWidth = 0f;
            }

            if (maxSideBorderWidth < minSideBorderWidth)
            {
                maxSideBorderWidth = minSideBorderWidth;
            }

            float width = sideBorderWidth;
            if (autoSideBorderWidthFromBackground)
            {
                width = EstimateSideBorderWidth();
            }

            float clampedWidth = Mathf.Clamp(width, minSideBorderWidth, maxSideBorderWidth);
            SetSideBorderWidth(clampedWidth);
            sideBorderWidth = clampedWidth;
        }

        private float EstimateSideBorderWidth()
        {
            if (backgroundImage == null || backgroundImage.sprite == null)
            {
                return sideBorderWidth;
            }

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            float screenAspect = screenWidth / screenHeight;

            float bgWidth = backgroundImage.sprite.rect.width;
            float bgHeight = Mathf.Max(1f, backgroundImage.sprite.rect.height);
            float backgroundAspect = bgWidth / bgHeight;

            if (backgroundAspect <= 0f || screenAspect <= backgroundAspect)
            {
                return sideBorderWidth;
            }

            float sideAreaPixel = screenWidth - (screenHeight * backgroundAspect);
            return sideAreaPixel * 0.5f;
        }

        private void SetSideBorderWidth(float width)
        {
            Transform leftBorder = sideBordersRoot.Find("LeftBorder");
            if (leftBorder != null)
            {
                RectTransform leftRect = leftBorder.GetComponent<RectTransform>();
                if (leftRect != null)
                {
                    Vector2 leftSize = leftRect.sizeDelta;
                    leftRect.sizeDelta = new Vector2(width, leftSize.y);
                }
            }

            Transform rightBorder = sideBordersRoot.Find("RightBorder");
            if (rightBorder != null)
            {
                RectTransform rightRect = rightBorder.GetComponent<RectTransform>();
                if (rightRect != null)
                {
                    Vector2 rightSize = rightRect.sizeDelta;
                    rightRect.sizeDelta = new Vector2(width, rightSize.y);
                }
            }
        }

        private void EnsureSafeAreaContent()
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            if (safeAreaContent == null)
            {
                safeAreaContent = transform.Find("SafeAreaContent") as RectTransform;
            }

            if (safeAreaContent == null)
            {
                var content = new GameObject("SafeAreaContent", typeof(RectTransform));
                safeAreaContent = content.GetComponent<RectTransform>();
                safeAreaContent.SetParent(root, false);
                safeAreaContent.SetSiblingIndex(root.childCount > 1 ? root.childCount - 1 : 0);
            }

            safeAreaContent.anchorMin = Vector2.zero;
            safeAreaContent.anchorMax = Vector2.one;
            safeAreaContent.offsetMin = Vector2.zero;
            safeAreaContent.offsetMax = Vector2.zero;

            if (sideBordersRoot == null)
            {
                sideBordersRoot = transform.Find("SideBorders") as RectTransform;
            }

            Transform background = transform.Find("imgBackground");
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);

                if (child == null || child == safeAreaContent)
                {
                    continue;
                }

                if (sideBordersRoot != null && child == sideBordersRoot)
                {
                    continue;
                }

                if (background != null && child == background)
                {
                    continue;
                }

                child.SetParent(safeAreaContent, false);
            }
        }
        private void OnEnable()
        {
            ApplyViewportLayout();
            BindPresenterEvents();
            BindStageNodeEvents();
        }

        private void OnDisable()
        {
            UnbindStageNodeEvents();
            UnbindPresenterEvents();
            ReleaseOverlayRoutingBindings();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplySideBorderWidth();
            ApplyViewportLayout();
        }

        protected override void OnInit()
        {
            TryApplyDefaultBackground();
            TryLoadDefaultAudio();
            TryLoadButtonCatalog();
            PlayWorldMapBgm();
            
            ApplyWorldMapButtonSkinAndLayout();
            CreateBottomButtons();
            ApplyViewportLayout();
            InitializeStageNodeFlow();

            if (btnBack.IsNotNull())
                btnBack.SetOnClickWithCooldown(OnClickBack);
        }

        protected override void OnEnter(object[] data)
        {
            base.OnEnter(data);
            RefreshStageNodeProgress();
        }

        private void OnClickHeroRoom()
        {
            PlayClickSfx();
            OpenHeroRoomPopup();
        }

        private void OnClickUpgrades()
        {
            PlayClickSfx();
            OpenUpgradesPopup();
        }

        // 월드맵 하단 설정 버튼 클릭: 오디오 옵션 팝업 오픈
        private void OnClickAudioOptions()
        {
            PlayClickSfx();
            OpenAudioOptionsPopup();
        }

        private void OnClickBack()
        {
            PlayClickSfx();
            Debug.Log("[WorldMapView] Back clicked. Returning to Title.");
            KingdomAppManager.Instance.ChangeScene(SCENES.TitleScene);
        }

        public override bool OnBackKey()
        {
            if (CloseOverlay())
            {
                return true;
            }

            OnClickBack();
            return true;
        }

        public void OpenUpgradesPopup()
        {
            EnsureUpgradesPopupRoot();
            OpenOverlayPopup(upgradesPopupRoot, "Upgrades");
        }

        public void OpenHeroRoomPopup()
        {
            EnsureHeroRoomPopupRoot();
            OpenOverlayPopup(heroRoomPopupRoot, "HeroRoom");
        }

        public void OpenAudioOptionsPopup()
        {
            EnsureAudioOptionsPopupRoot();
            OpenOverlayPopup(audioOptionsPopupRoot, "AudioOptions");
        }

        private void EnsureUpgradesPopupRoot()
        {
            if (upgradesPopupRoot != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(UpgradesPopupResourcePath);
            if (prefab != null)
            {
                EnsureOverlayContainer();
                Transform parent = overlayContainer != null ? overlayContainer.transform : transform;
                upgradesPopupRoot = Instantiate(prefab, parent, false);
                upgradesPopupRoot.name = "UpgradesPopup";
                return;
            }

            upgradesPopupRoot = CreateFallbackUpgradesPopup();
        }

        public bool CloseOverlay()
        {
            if (_currentOverlayPopup == null)
            {
                return false;
            }

            _currentOverlayPopup.SetActive(false);
            _currentOverlayPopup = null;

            if (overlayContainer != null)
            {
                overlayContainer.SetActive(false);
            }

            return true;
        }

        private void OpenOverlayPopup(GameObject popupRoot, string popupName)
        {
            EnsureOverlayRoutingBindings();

            if (popupRoot == null)
            {
                Debug.LogWarning($"[WorldMapView] {popupName} popup root is not assigned.");
                UIHelper.ShowToast($"{popupName} popup is not ready.");
                return;
            }

            if (overlayContainer != null)
            {
                overlayContainer.SetActive(true);
            }

            if (_currentOverlayPopup != null && _currentOverlayPopup != popupRoot)
            {
                _currentOverlayPopup.SetActive(false);
            }

            popupRoot.SetActive(true);
            _currentOverlayPopup = popupRoot;
        }

        private void EnsureOverlayRoutingBindings()
        {
            EnsureOverlayContainer();

            if (overlayContainer != null && overlayDimButton == null)
            {
                Transform dim = overlayContainer.transform.Find("OverlayDimButton");
                if (dim != null)
                {
                    overlayDimButton = dim.GetComponent<Button>();
                }
            }

            if (overlayContainer != null)
            {
                overlayContainer.SetActive(false);
            }

            if (overlayDimButton != null)
            {
                overlayDimButton.onClick.RemoveListener(OnOverlayDimClicked);
                overlayDimButton.onClick.AddListener(OnOverlayDimClicked);
            }

            if (upgradesPopupRoot != null)
            {
                upgradesPopupRoot.SetActive(false);
                BindOverlayCloseButtons(upgradesPopupRoot);
            }

            if (heroRoomPopupRoot != null)
            {
                heroRoomPopupRoot.SetActive(false);
                BindOverlayCloseButtons(heroRoomPopupRoot);
            }

            if (audioOptionsPopupRoot != null)
            {
                audioOptionsPopupRoot.SetActive(false);
                BindOverlayCloseButtons(audioOptionsPopupRoot);
            }
        }

        private void ReleaseOverlayRoutingBindings()
        {
            for (int i = 0; i < _overlayCloseButtons.Count; i++)
            {
                Button closeButton = _overlayCloseButtons[i];
                if (closeButton == null)
                {
                    continue;
                }

                closeButton.onClick.RemoveListener(OnOverlayCloseButtonClicked);
            }

            _overlayCloseButtons.Clear();

            if (overlayDimButton != null)
            {
                overlayDimButton.onClick.RemoveListener(OnOverlayDimClicked);
            }
        }

        private void OnOverlayDimClicked()
        {
            CloseOverlay();
        }

        private void OnOverlayCloseButtonClicked()
        {
            CloseOverlay();
        }

        private void EnsureOverlayContainer()
        {
            if (overlayContainer != null)
            {
                ReparentOverlayContainerToPopupLayerIfNeeded();
                return;
            }

            Transform container = transform.Find("OverlayContainer");
            if (container == null && safeAreaContent != null)
            {
                container = safeAreaContent.Find("OverlayContainer");
            }

            if (container != null)
            {
                overlayContainer = container.gameObject;
                ReparentOverlayContainerToPopupLayerIfNeeded();
                return;
            }

            Transform parent = ResolveOverlayParent();
            GameObject containerObject = new GameObject("OverlayContainer", typeof(RectTransform));
            RectTransform containerRect = containerObject.GetComponent<RectTransform>();
            containerRect.SetParent(parent, false);
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            containerRect.SetAsLastSibling();
            overlayContainer = containerObject;

            GameObject dimObject = new GameObject("OverlayDimButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform dimRect = dimObject.GetComponent<RectTransform>();
            dimRect.SetParent(containerRect, false);
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image dimImage = dimObject.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.45f);
            dimImage.raycastTarget = true;

            overlayDimButton = dimObject.GetComponent<Button>();
            overlayContainer.SetActive(false);
        }

        private Transform ResolveOverlayParent()
        {
            // 팝업은 Screen 레이어가 아닌 Popup 레이어 캔버스에 배치한다.
            if (UIManager.Instance != null)
            {
                Canvas popupCanvas = UIManager.Instance.GetLayerCanvas(UILayer.Popup);
                if (popupCanvas != null)
                {
                    return popupCanvas.transform;
                }
            }

            return safeAreaContent != null ? safeAreaContent : transform;
        }

        private void ReparentOverlayContainerToPopupLayerIfNeeded()
        {
            if (overlayContainer == null)
            {
                return;
            }

            Transform targetParent = ResolveOverlayParent();
            RectTransform overlayRect = overlayContainer.transform as RectTransform;
            if (targetParent == null || overlayRect == null)
            {
                return;
            }

            if (overlayRect.parent == targetParent)
            {
                return;
            }

            overlayRect.SetParent(targetParent, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();
        }

        private void BindOverlayCloseButtons(GameObject popupRoot)
        {
            if (popupRoot == null)
            {
                return;
            }

            Button[] buttons = popupRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                string buttonName = button.name;
                if (!buttonName.Contains("Close", StringComparison.OrdinalIgnoreCase) &&
                    !buttonName.Contains("Back", StringComparison.OrdinalIgnoreCase) &&
                    !buttonName.Contains("Exit", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                button.onClick.RemoveListener(OnOverlayCloseButtonClicked);
                button.onClick.AddListener(OnOverlayCloseButtonClicked);

                if (!_overlayCloseButtons.Contains(button))
                {
                    _overlayCloseButtons.Add(button);
                }
            }
        }

        private GameObject CreateFallbackUpgradesPopup()
        {
            EnsureOverlayContainer();
            Transform parent = overlayContainer != null ? overlayContainer.transform : transform;

            GameObject popupRoot = new GameObject("UpgradesPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform popupRect = popupRoot.GetComponent<RectTransform>();
            popupRect.SetParent(parent, false);
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(980f, 720f);
            popupRect.anchoredPosition = Vector2.zero;
            popupRect.SetAsLastSibling();

            Image bgImage = popupRoot.GetComponent<Image>();
            bgImage.color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
            bgImage.raycastTarget = true;

            GameObject titleObject = new GameObject("txtTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.SetParent(popupRect, false);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(420f, 64f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);

            TextMeshProUGUI titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.text = "업그레이드";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 42f;
            titleText.color = Color.white;

            GameObject closeButtonObject = new GameObject("btnClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform closeRect = closeButtonObject.GetComponent<RectTransform>();
            closeRect.SetParent(popupRect, false);
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(88f, 56f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);

            Image closeImage = closeButtonObject.GetComponent<Image>();
            closeImage.color = new Color(0.6f, 0.2f, 0.2f, 0.95f);

            GameObject closeLabelObject = new GameObject("txtLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.SetParent(closeRect, false);
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.fontSize = 30f;
            closeLabel.color = Color.white;

            SkillTreeUI skillTree = popupRoot.AddComponent<SkillTreeUI>();
            skillTree.enabled = true;

            popupRoot.SetActive(false);
            BindOverlayCloseButtons(popupRoot);
            return popupRoot;
        }

        private void EnsureHeroRoomPopupRoot()
        {
            if (heroRoomPopupRoot != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(HeroRoomPopupResourcePath);
            if (prefab != null)
            {
                EnsureOverlayContainer();
                Transform parent = overlayContainer != null ? overlayContainer.transform : transform;
                heroRoomPopupRoot = Instantiate(prefab, parent, false);
                heroRoomPopupRoot.name = "HeroRoomPopup";
                return;
            }

            heroRoomPopupRoot = CreateFallbackHeroRoomPopup();
        }

        private GameObject CreateFallbackHeroRoomPopup()
        {
            EnsureOverlayContainer();
            Transform parent = overlayContainer != null ? overlayContainer.transform : transform;

            GameObject popupRoot = new GameObject("HeroRoomPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform popupRect = popupRoot.GetComponent<RectTransform>();
            popupRect.SetParent(parent, false);
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(980f, 720f);
            popupRect.anchoredPosition = Vector2.zero;
            popupRect.SetAsLastSibling();

            Image bgImage = popupRoot.GetComponent<Image>();
            bgImage.color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
            bgImage.raycastTarget = true;

            GameObject titleObject = new GameObject("txtTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.SetParent(popupRect, false);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(420f, 64f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);

            TextMeshProUGUI titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.text = "영웅 관리소";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 42f;
            titleText.color = Color.white;

            GameObject closeButtonObject = new GameObject("btnClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform closeRect = closeButtonObject.GetComponent<RectTransform>();
            closeRect.SetParent(popupRect, false);
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(88f, 56f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);

            Image closeImage = closeButtonObject.GetComponent<Image>();
            closeImage.color = new Color(0.6f, 0.2f, 0.2f, 0.95f);

            GameObject closeLabelObject = new GameObject("txtLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.SetParent(closeRect, false);
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.fontSize = 30f;
            closeLabel.color = Color.white;

            HeroSelectionUI heroSelection = popupRoot.AddComponent<HeroSelectionUI>();
            heroSelection.enabled = true;

            popupRoot.SetActive(false);
            BindOverlayCloseButtons(popupRoot);
            return popupRoot;
        }

        private void EnsureAudioOptionsPopupRoot()
        {
            if (audioOptionsPopupRoot != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(AudioOptionsPopupResourcePath);
            if (prefab != null)
            {
                EnsureOverlayContainer();
                Transform parent = overlayContainer != null ? overlayContainer.transform : transform;
                audioOptionsPopupRoot = Instantiate(prefab, parent, false);
                audioOptionsPopupRoot.name = "AudioOptionsPopup";
                return;
            }

            audioOptionsPopupRoot = CreateFallbackAudioOptionsPopup();
        }

        // 프리팹 미존재 환경에서도 테스트 가능한 최소 오디오 옵션 팝업 생성
        private GameObject CreateFallbackAudioOptionsPopup()
        {
            EnsureOverlayContainer();
            Transform parent = overlayContainer != null ? overlayContainer.transform : transform;

            GameObject popupRoot = new GameObject("AudioOptionsPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform popupRect = popupRoot.GetComponent<RectTransform>();
            popupRect.SetParent(parent, false);
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(760f, 520f);
            popupRect.anchoredPosition = Vector2.zero;
            popupRect.SetAsLastSibling();

            Image bgImage = popupRoot.GetComponent<Image>();
            bgImage.color = new Color(0.09f, 0.1f, 0.12f, 0.96f);
            bgImage.raycastTarget = true;

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(popupRect, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 450f);
            panel.GetComponent<Image>().color = new Color(0.16f, 0.17f, 0.2f, 0.98f);

            TextMeshProUGUI titleText = CreatePopupText(panelRect, "Title", "오디오 설정", 36f, TextAlignmentOptions.Center, new Vector2(0f, 176f), new Vector2(400f, 64f));
            titleText.color = Color.white;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.SetParent(panelRect, false);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(620f, 260f);

            CreateAudioSliderRow(contentRect, "BgmRow", "BGM", 56f);
            CreateAudioSliderRow(contentRect, "SfxRow", "SFX", -56f);

            GameObject closeButtonObject = new GameObject("BtnClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform closeRect = closeButtonObject.GetComponent<RectTransform>();
            closeRect.SetParent(panelRect, false);
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(220f, 64f);
            closeRect.anchoredPosition = new Vector2(0f, 20f);
            closeButtonObject.GetComponent<Image>().color = new Color(0.76f, 0.76f, 0.82f, 1f);
            CreatePopupText(closeRect, "Label", "닫기", 28f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero).color = Color.black;

            // 팝업 런타임 스크립트 연결: 슬라이더/토글/텍스트를 이름 경로로 바인딩함
            popupRoot.AddComponent<AudioOptionsPopup>();
            popupRoot.SetActive(false);
            BindOverlayCloseButtons(popupRoot);
            return popupRoot;
        }

        private void CreateAudioSliderRow(RectTransform parent, string rowName, string label, float y)
        {
            GameObject row = new GameObject(rowName, typeof(RectTransform));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.SetParent(parent, false);
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(620f, 72f);
            rowRect.anchoredPosition = new Vector2(0f, y);

            TextMeshProUGUI labelText = CreatePopupText(rowRect, "Label", label, 24f, TextAlignmentOptions.Left, new Vector2(-260f, 0f), new Vector2(120f, 48f));
            labelText.color = Color.white;

            // Slider 기본 구조 (Background/Fill/Handle) 생성
            Slider slider = CreateBasicSlider(rowRect, "Slider", new Vector2(-20f, 0f), new Vector2(280f, 28f));
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            TextMeshProUGUI valueText = CreatePopupText(rowRect, "Value", "100%", 20f, TextAlignmentOptions.Center, new Vector2(190f, 0f), new Vector2(88f, 48f));
            valueText.color = Color.white;

            Toggle toggle = CreateBasicToggle(rowRect, "MuteToggle", "Mute", new Vector2(272f, 0f), new Vector2(110f, 40f));
            toggle.isOn = false;
        }

        private Slider CreateBasicSlider(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.SetParent(parent, false);
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = anchoredPos;
            sliderRect.sizeDelta = size;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.SetParent(sliderRect, false);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.SetParent(sliderRect, false);
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(6f, 6f);
            fillAreaRect.offsetMax = new Vector2(-20f, -6f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.SetParent(fillAreaRect, false);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.45f, 0.7f, 0.98f, 1f);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.SetParent(sliderRect, false);
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.SetParent(handleAreaRect, false);
            handleRect.sizeDelta = new Vector2(18f, 34f);
            handle.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 1f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private Toggle CreateBasicToggle(RectTransform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
        {
            GameObject toggleObject = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            toggleRect.SetParent(parent, false);
            toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
            toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);
            toggleRect.anchoredPosition = anchoredPos;
            toggleRect.sizeDelta = size;

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.SetParent(toggleRect, false);
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.sizeDelta = new Vector2(26f, 26f);
            bgRect.anchoredPosition = new Vector2(0f, 0f);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.SetParent(bgRect, false);
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(16f, 16f);
            checkmark.GetComponent<Image>().color = new Color(0.45f, 0.9f, 0.45f, 1f);

            TextMeshProUGUI labelText = CreatePopupText(toggleRect, "Label", label, 18f, TextAlignmentOptions.Left, new Vector2(38f, 0f), new Vector2(72f, 30f));
            labelText.color = Color.white;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            return toggle;
        }

        private TextMeshProUGUI CreatePopupText(RectTransform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 anchoredPos, Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = anchoredPos;
            textRect.sizeDelta = size;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = align;
            return label;
        }

        public void SetBackgroundImage(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            EnsureBackgroundImage();
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.sprite = sprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = true;

            ApplyBackgroundCoverMode(sprite);
        }

        private void TryApplyDefaultBackground()
        {
            Sprite sprite = Resources.Load<Sprite>(WorldMapBackgroundResourcePath);
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>(LegacyWorldMapBackgroundResourcePath);
            }

            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(WorldMapBackgroundResourcePath);
                if (texture == null)
                {
                    texture = Resources.Load<Texture2D>(LegacyWorldMapBackgroundResourcePath);
                }

                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            if (sprite != null)
            {
                SetBackgroundImage(sprite);
                Debug.Log($"[WorldMapView] Background applied: {sprite.name}");
            }
            else
            {
                Debug.Log("[WorldMapView] World map background sprite not found in Resources. Using existing background.");
            }
        }

        private void EnsureBackgroundImage()
        {
            if (backgroundImage != null)
            {
                return;
            }

            Transform parent = transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Transform existing = canvas.transform.Find("imgBackground");
                if (existing != null)
                {
                    backgroundImage = existing.GetComponent<Image>();
                    if (backgroundImage != null)
                    {
                        return;
                    }
                }

                parent = canvas.transform;
            }

            GameObject go = new GameObject("imgBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            backgroundImage = go.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = true;
        }

        private void ApplyBackgroundCoverMode(Sprite sprite)
        {
            if (backgroundImage == null || sprite == null)
            {
                return;
            }

            var fitter = backgroundImage.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            }

            var width = Mathf.Max(1f, sprite.rect.width);
            var height = Mathf.Max(1f, sprite.rect.height);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = width / height;
        }

        private void CacheBottomBarBasePosition()
        {
            if (hasBottomBarBasePosition)
            {
                return;
            }

            if (bottomBar is RectTransform bottomBarRect)
            {
                bottomBarBaseAnchoredPosition = bottomBarRect.anchoredPosition;
                hasBottomBarBasePosition = true;
            }
        }

        private void ApplyViewportLayout()
        {
            if (viewportLayout != null)
            {
                viewportLayout.ApplyLayout();
                return;
            }

            ApplySafeArea();
            ApplyBottomBarLift();
        }

        private void TryLoadButtonCatalog()
        {
            if (_buttonCatalogEntries.Count > 0)
            {
                return;
            }

            TextAsset textAsset = Resources.Load<TextAsset>(WorldMapButtonCatalogResourcePath);
            if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
            {
                return;
            }

            WorldMapButtonCatalogData data;
            try
            {
                data = JsonUtility.FromJson<WorldMapButtonCatalogData>(textAsset.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WorldMapView] Failed to parse button catalog JSON: {ex.Message}");
                return;
            }

            if (data?.entries == null || data.entries.Length == 0)
            {
                return;
            }

            _buttonCatalogEntries.Clear();
            for (int i = 0; i < data.entries.Length; i++)
            {
                WorldMapButtonCatalogEntry entry = data.entries[i];
                if (string.IsNullOrWhiteSpace(entry.actionId))
                {
                    continue;
                }

                _buttonCatalogEntries[entry.actionId] = entry;
            }
        }

        private void ApplySafeArea()
        {
            if (!useSafeArea)
            {
                return;
            }

            RectTransform target = safeAreaTarget != null ? safeAreaTarget : transform as RectTransform;
            if (target == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (safeArea == lastAppliedSafeArea)
            {
                return;
            }

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            min.x /= screenWidth;
            min.y /= screenHeight;
            max.x /= screenWidth;
            max.y /= screenHeight;

            target.anchorMin = min;
            target.anchorMax = max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;

            lastAppliedSafeArea = safeArea;
        }

        private void ApplyBottomBarLift()
        {
            CacheBottomBarBasePosition();
            if (!hasBottomBarBasePosition)
            {
                return;
            }

            if (bottomBar is RectTransform bottomBarRect)
            {
                bottomBarRect.anchoredPosition = bottomBarBaseAnchoredPosition + new Vector2(0f, bottomBarLift);
            }
        }

        private void TryLoadDefaultAudio()
        {
            if (bgmClip == null)
            {
                bgmClip = Resources.Load<AudioClip>(WorldMapBgmResourcePath);
            }

            if (clickClip == null)
            {
                clickClip = Resources.Load<AudioClip>(WorldMapClickResourcePath);
            }

            if (bgmClip == null)
            {
                Debug.Log($"[WorldMapView] BGM clip not found at Resources/{WorldMapBgmResourcePath}. Audio fallback: silent.");
            }

            if (clickClip == null)
            {
                Debug.Log($"[WorldMapView] Click clip not found at Resources/{WorldMapClickResourcePath}. Audio fallback: silent.");
            }
        }

        private void PlayWorldMapBgm()
        {
            if (bgmClip == null)
            {
                return;
            }

            AudioHelper.Instance?.PlayBGM(bgmClip, 0.15f);
        }

        private void PlayClickSfx()
        {
            if (clickClip == null)
            {
                return;
            }

            AudioHelper.Instance?.PlaySFX(clickClip, clickVolumeScale);
        }

        private void InitializeStageNodeFlow()
        {
            CollectStageNodes();
            if (stageNodes.Count == 0)
            {
                Debug.LogWarning("[WorldMap] No UIStageNode found. Stage selection flow is disabled.");
                return;
            }

            if (!ValidateStageNodeIds())
            {
                return;
            }

            List<StageData> stageDataList = LoadStageData();
            if (stageDataList.Count == 0)
            {
                Debug.LogWarning("[WorldMap] Stage data is empty. Stage selection flow is disabled.");
                return;
            }

            _stagePresenter = new WorldMapPresenter(
                stageDataList,
                BuildProgressRepository(),
                new DefaultStageUnlockPolicy());

            BindPresenterEvents();
            BindStageNodeEvents();
            RebindAllStageNodes();
        }

        public void RefreshStageNodeProgress()
        {
            if (stageNodes == null || stageNodes.Count == 0)
            {
                CollectStageNodes();
            }

            if (stageNodes == null || stageNodes.Count == 0)
            {
                return;
            }

            List<StageData> stageDataList = LoadStageData();
            if (stageDataList.Count == 0)
            {
                return;
            }

            UnbindPresenterEvents();
            _stagePresenter = new WorldMapPresenter(
                stageDataList,
                BuildProgressRepository(),
                new DefaultStageUnlockPolicy());

            BindPresenterEvents();
            RebindAllStageNodes();
        }

        private void CollectStageNodes()
        {
            stageNodes.Clear();
            UIStageNode[] foundNodes = GetComponentsInChildren<UIStageNode>(true);
            if (foundNodes == null || foundNodes.Length == 0)
            {
                return;
            }

            stageNodes.AddRange(foundNodes);
            stageNodes.Sort((a, b) => a.StageId.CompareTo(b.StageId));
        }

        private bool ValidateStageNodeIds()
        {
            var idSet = new HashSet<int>();
            for (int i = 0; i < stageNodes.Count; i++)
            {
                UIStageNode node = stageNodes[i];
                if (node == null)
                {
                    continue;
                }

                if (node.StageId <= 0)
                {
                    Debug.LogError($"[WorldMap] Invalid StageId on node: {node.name}", node);
                    return false;
                }

                if (!idSet.Add(node.StageId))
                {
                    Debug.LogError($"[WorldMap] Duplicate StageId detected: {node.StageId}", node);
                    return false;
                }
            }

            return true;
        }

        private List<StageData> LoadStageData()
        {
            StageConfig config = null;

            if (WorldMapManager.Instance != null)
            {
                config = WorldMapManager.Instance.CurrentStageConfig;
            }

            if (config == null)
            {
                config = Resources.Load<StageConfig>(WorldMapStageConfigResourcePath);
                if (config == null)
                {
                    config = Kingdom.Game.ConfigResourcePaths.LoadStageConfigByWorldId(1);
                }
            }

            if (config != null && config.Stages != null && config.Stages.Count > 0)
            {
                return new List<StageData>(config.Stages);
            }

            var fallback = new List<StageData>();
            for (int i = 0; i < stageNodes.Count; i++)
            {
                UIStageNode node = stageNodes[i];
                if (node == null)
                {
                    continue;
                }

                fallback.Add(new StageData
                {
                    StageId = node.StageId,
                    StageName = $"STAGE {node.StageId}",
                    Difficulty = StageDifficulty.Normal,
                    Position = Vector2.zero,
                    NextStageIds = new List<int>(),
                    StarRequirements = new List<float>(),
                    IsBoss = false,
                    IsUnlocked = node.StageId == 1,
                    BestTime = 0f
                });
            }

            fallback.Sort((a, b) => a.StageId.CompareTo(b.StageId));
            return fallback;
        }

        private void BindStageNodeEvents()
        {
            if (stageNodes == null || stageNodes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < stageNodes.Count; i++)
            {
                UIStageNode node = stageNodes[i];
                if (node == null)
                {
                    continue;
                }

                node.OnNodeClicked -= OnStageNodeClicked;
                node.OnNodeClicked += OnStageNodeClicked;
            }
        }

        private void UnbindStageNodeEvents()
        {
            if (stageNodes == null || stageNodes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < stageNodes.Count; i++)
            {
                UIStageNode node = stageNodes[i];
                if (node == null)
                {
                    continue;
                }

                node.OnNodeClicked -= OnStageNodeClicked;
            }
        }

        private void BindPresenterEvents()
        {
            if (_stagePresenter == null)
            {
                return;
            }

            _stagePresenter.StageLockedClicked -= OnLockedStageClicked;
            _stagePresenter.StageLockedClicked += OnLockedStageClicked;

            _stagePresenter.StageSelected -= OnUnlockedStageSelected;
            _stagePresenter.StageSelected += OnUnlockedStageSelected;
        }

        private void UnbindPresenterEvents()
        {
            if (_stagePresenter == null)
            {
                return;
            }

            _stagePresenter.StageLockedClicked -= OnLockedStageClicked;
            _stagePresenter.StageSelected -= OnUnlockedStageSelected;
        }

        private void OnStageNodeClicked(int stageId)
        {
            if (_stagePresenter == null)
            {
                return;
            }

            bool changed = _stagePresenter.HandleNodeClicked(stageId);
            if (changed)
            {
                RebindAllStageNodes();
            }
        }

        private void OnLockedStageClicked(int stageId)
        {
            UIHelper.ShowToast($"Stage {stageId} is locked.");
        }

        private void OnUnlockedStageSelected(int stageId)
        {
            PlayClickSfx();

            if (WorldMapManager.Instance != null)
            {
                WorldMapManager.Instance.OnNodeClicked(stageId);
                return;
            }

            WorldMapScene.SetSelectedStageContext(stageId, StageDifficulty.Normal);
            Debug.LogWarning($"[WorldMap] WorldMapManager missing. Fallback loading GameScene for stage {stageId} with default difficulty.");
            KingdomAppManager.Instance.ChangeScene(SCENES.GameScene);
        }

        private static IStageProgressRepository BuildProgressRepository()
        {
            UserSaveData saveData = null;
            if (SaveManager.Instance != null)
            {
                saveData = SaveManager.Instance.SaveData;
            }

            return new UserSaveStageProgressRepository(saveData);
        }

        private void RebindAllStageNodes()
        {
            if (_stagePresenter == null || stageNodes == null || stageNodes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < stageNodes.Count; i++)
            {
                UIStageNode node = stageNodes[i];
                if (node == null)
                {
                    continue;
                }

                if (!_stagePresenter.TryBuildViewModel(node.StageId, out StageNodeViewModel vm))
                {
                    node.SetInteractable(false);
                    continue;
                }

                node.Bind(vm);
            }
        }

        private void ApplyWorldMapButtonSkinAndLayout()
        {
            SetButtonVisual(btnBack, "UI/Sprites/WorldMap/Icon_Back");
        }

        private void CreateBottomButtons()
        {
            EnsureAudioOptionsButtonBinding();

            // 영웅 방
            if (btnHeroRoom.IsNotNull() && bottomBar.IsNotNull())
            {
                UIActionButtonItemConfig config = BuildButtonConfig(HeroRoomActionId, HeroRoomLabel, "UI/Sprites/WorldMap/Icon_Hero");
                btnHeroRoom.Init(config, OnClickHeroRoom);
            }

            // 업그레이드
            if (btnUpgrades.IsNotNull() && bottomBar.IsNotNull())
            {
                UIActionButtonItemConfig config = BuildButtonConfig(UpgradesActionId, UpgradeLabel, "UI/Sprites/WorldMap/Icon_Upgrade");
                btnUpgrades.Init(config, OnClickUpgrades);
            }

            // 오디오 옵션 (프리팹에 버튼 슬롯이 있을 때 연결됨)
            if (btnAudioOptions.IsNotNull() && bottomBar.IsNotNull())
            {
                UIActionButtonItemConfig config = BuildButtonConfig(AudioOptionsActionId, AudioOptionsLabel, "UI/Sprites/WorldMap/Icon_Upgrade");
                btnAudioOptions.Init(config, OnClickAudioOptions);
            }
        }

        // 레거시 월드맵 프리팹(btnAudioOptions 미배선) 대응:
        // 후보 이름 검색 -> 미존재 시 업그레이드 버튼을 복제해 오디오 버튼 슬롯으로 사용.
        private void EnsureAudioOptionsButtonBinding()
        {
            if (btnAudioOptions.IsNotNull() || bottomBar.IsNull())
            {
                return;
            }

            for (int i = 0; i < AudioOptionsButtonCandidateNames.Length; i++)
            {
                Transform candidate = bottomBar.Find(AudioOptionsButtonCandidateNames[i]);
                if (candidate == null)
                {
                    continue;
                }

                btnAudioOptions = candidate.GetComponent<UIActionButtonItem>();
                if (btnAudioOptions.IsNotNull())
                {
                    return;
                }
            }

            UIActionButtonItem[] items = bottomBar.GetComponentsInChildren<UIActionButtonItem>(true);
            for (int i = 0; i < items.Length; i++)
            {
                UIActionButtonItem item = items[i];
                if (item.IsNull() || item == btnHeroRoom || item == btnUpgrades)
                {
                    continue;
                }

                btnAudioOptions = item;
                return;
            }

            if (btnUpgrades.IsNull() || btnUpgrades.gameObject.IsNull())
            {
                Debug.LogWarning("[WorldMapView] Audio options button is missing and fallback clone source is not available.");
                return;
            }

            GameObject clone = Instantiate(btnUpgrades.gameObject, bottomBar, false);
            clone.name = "btn_audio_options";
            clone.SetActive(true);

            btnAudioOptions = clone.GetComponent<UIActionButtonItem>();
            if (btnAudioOptions.IsNull())
            {
                btnAudioOptions = clone.GetComponentInChildren<UIActionButtonItem>(true);
            }

            if (btnAudioOptions.IsNull())
            {
                Debug.LogWarning("[WorldMapView] Failed to bind runtime audio options button.");
                return;
            }

            Debug.Log("[WorldMapView] Runtime audio options button created from upgrades button template.");
        }

        private UIActionButtonItemConfig BuildButtonConfig(string actionId, string fallbackLabel, string fallbackIconResourcePath)
        {
            if (_buttonCatalogEntries.TryGetValue(actionId, out WorldMapButtonCatalogEntry catalogEntry))
            {
                string label = string.IsNullOrWhiteSpace(catalogEntry.labelText) ? fallbackLabel : catalogEntry.labelText;
                string iconPath = string.IsNullOrWhiteSpace(catalogEntry.iconResourcePath) ? fallbackIconResourcePath : catalogEntry.iconResourcePath;
                Sprite icon = Resources.Load<Sprite>(iconPath);
                if (icon == null)
                {
                    icon = Resources.Load<Sprite>(fallbackIconResourcePath);
                }

                return new UIActionButtonItemConfig(label, icon);
            }

            Sprite fallbackSprite = Resources.Load<Sprite>(fallbackIconResourcePath);
            return new UIActionButtonItemConfig(fallbackLabel, fallbackSprite);
        }

        private void SetButtonVisual(Button button, string resourcePath)
        {
            if (button == null)
            {
                return;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null && button.image != null)
            {
                button.image.sprite = sprite;
                button.image.type = Image.Type.Simple;
                button.image.preserveAspect = true;
                button.image.color = Color.white;
            }
        }
    }
}



