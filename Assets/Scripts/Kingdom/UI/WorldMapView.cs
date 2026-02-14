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

namespace Kingdom.App
{
    /// <summary>
    /// World map main view. Binds world-map visuals and audio assets from Resources.
    /// </summary>
    public class WorldMapView : BaseView
    {
        private const string WorldMapBackgroundResourcePath = "UI/Sprites/WorldMap/WorldMap_Background";
        private const string LegacyWorldMapBackgroundResourcePath = "UI/Sprites/WorldMap_BG";
        private const string WorldMapBgmResourcePath = "Audio/WorldMap/WorldMap_BGM";
        private const string WorldMapClickResourcePath = "Audio/WorldMap/WorldMap_Click";
        private const string WorldMapButtonCatalogResourcePath = "Data/UI/WorldMapButtonCatalog";
        private const string WorldMapStageConfigResourcePath = "Data/StageConfigs/World1_StageConfig";
        private const string HeroRoomActionId = "hero_room";
        private const string UpgradesActionId = "upgrades";
        private const string HeroRoomLabel = "\uC601\uC6C5 \uAD00\uB9AC\uC18C";
        private const string UpgradeLabel = "\uC5C5\uADF8\uB808\uC774\uB4DC";

        [Header("WorldMap UI")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button btnBack;

        [Header("Containers")]
        [SerializeField] private Transform bottomBar;
        [SerializeField] private RectTransform safeAreaTarget;
        [SerializeField] private bool useSafeArea = true;
        [SerializeField, Range(0f, 64f)] private float bottomBarLift = 10f;
        [SerializeField] private ViewportSafeAreaLayout viewportLayout;

        [SerializeField] UIActionButtonItem btnHeroRoom;
        [SerializeField] UIActionButtonItem btnUpgrades;
        [SerializeField] private List<UIStageNode> stageNodes = new List<UIStageNode>();

        [Header("WorldMap Audio")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float clickVolumeScale = 0.9f;
        private Rect lastAppliedSafeArea = Rect.zero;
        private bool hasBottomBarBasePosition;
        private Vector2 bottomBarBaseAnchoredPosition;
        private readonly Dictionary<string, WorldMapButtonCatalogEntry> _buttonCatalogEntries = new Dictionary<string, WorldMapButtonCatalogEntry>(StringComparer.Ordinal);
        private WorldMapPresenter _stagePresenter;

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
                backgroundImage = transform.Find("imgBackground").GetComponent<Image>();

            if (bottomBar == null) bottomBar = transform.Find("BottomBar");

            if (btnBack == null && transform.Find("btnBack")) btnBack = transform.Find("btnBack").GetComponent<Button>();
            if (safeAreaTarget == null) safeAreaTarget = transform as RectTransform;
            if (viewportLayout == null) viewportLayout = GetComponent<ViewportSafeAreaLayout>();

            EnsureBackgroundImage();
            CacheBottomBarBasePosition();
            ApplyViewportLayout();
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
        }

        private void OnRectTransformDimensionsChange()
        {
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

        private void OnClickHeroRoom()
        {
            PlayClickSfx();
            Debug.Log("[WorldMapView] Hero Room clicked (Not implemented).");
            UIHelper.ShowToast("\uC601\uC6C5 \uAD00\uB9AC\uC18C\uB294 \uC900\uBE44 \uC911\uC785\uB2C8\uB2E4!");
        }

        private void OnClickUpgrades()
        {
            PlayClickSfx();
            Debug.Log("[WorldMapView] Upgrades clicked (Not implemented).");
            UIHelper.ShowToast("\uC5C5\uADF8\uB808\uC774\uB4DC \uAE30\uB2A5\uC740 \uC900\uBE44 \uC911\uC785\uB2C8\uB2E4!");
        }

        private void OnClickBack()
        {
            PlayClickSfx();
            Debug.Log("[WorldMapView] Back clicked. Returning to Title.");
            KingdomAppManager.Instance.ChangeScene(SCENES.TitleScene);
        }

        public override bool OnBackKey()
        {
            OnClickBack();
            return true;
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
                Debug.LogWarning("[WorldMapView] World map background sprite not found in Resources.");
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
                Debug.LogWarning($"[WorldMapView] BGM clip not found at Resources/{WorldMapBgmResourcePath}");
            }

            if (clickClip == null)
            {
                Debug.LogWarning($"[WorldMapView] Click clip not found at Resources/{WorldMapClickResourcePath}");
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
                new UserSaveStageProgressRepository(),
                new DefaultStageUnlockPolicy());

            BindPresenterEvents();
            BindStageNodeEvents();
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

            Debug.LogWarning($"[WorldMap] WorldMapManager missing. Fallback loading GameScene for stage {stageId}.");
            KingdomAppManager.Instance.ChangeScene(SCENES.GameScene);
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
            // Hero Room
            if (btnHeroRoom.IsNotNull() && bottomBar.IsNotNull())
            {
                UIActionButtonItemConfig config = BuildButtonConfig(HeroRoomActionId, HeroRoomLabel, "UI/Sprites/WorldMap/Icon_Hero");
                btnHeroRoom.Init(config, OnClickHeroRoom);
            }

            // Upgrades
            if (btnUpgrades.IsNotNull() && bottomBar.IsNotNull())
            {
                UIActionButtonItemConfig config = BuildButtonConfig(UpgradesActionId, UpgradeLabel, "UI/Sprites/WorldMap/Icon_Upgrade");
                btnUpgrades.Init(config, OnClickUpgrades);
            }
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

