using Common.Extensions;
using Common.UI;
using Common.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Common.UI.Components;

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

        [Header("WorldMap UI")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button btnStage1;
        [SerializeField] private Button btnBack;

        [Header("Containers")]
        [SerializeField] private Transform bottomBar;

        [SerializeField] UIActionButtonItem btnHeroRoom;
        [SerializeField] UIActionButtonItem btnUpgrades;

        [Header("WorldMap Audio")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float clickVolumeScale = 0.9f;

        private void Awake()
        {
            if (backgroundImage == null && transform.Find("imgBackground"))
                backgroundImage = transform.Find("imgBackground").GetComponent<Image>();

            if (bottomBar == null) bottomBar = transform.Find("BottomBar");

            if (btnStage1 == null && transform.Find("btnStage1")) btnStage1 = transform.Find("btnStage1").GetComponent<Button>();
            if (btnBack == null && transform.Find("btnBack")) btnBack = transform.Find("btnBack").GetComponent<Button>();

            EnsureBackgroundImage();
        }

        protected override void OnInit()
        {
            TryApplyDefaultBackground();
            TryLoadDefaultAudio();
            PlayWorldMapBgm();
            
            ApplyWorldMapButtonSkinAndLayout();
            CreateBottomButtons();

            if (btnStage1.IsNotNull())
                btnStage1.SetOnClickWithCooldown(() => OnClickStage(1));



            if (btnBack.IsNotNull())
                btnBack.SetOnClickWithCooldown(OnClickBack);
        }

        private void OnClickStage(int stageId)
        {
            PlayClickSfx();
            Debug.Log($"[WorldMapView] Stage {stageId} selected. Loading GameScene...");
            KingdomAppManager.Instance.ChangeScene(SCENES.GameScene);
        }

        private void OnClickHeroRoom()
        {
            PlayClickSfx();
            Debug.Log("[WorldMapView] Hero Room clicked (Not implemented).");
            UIHelper.ShowToast("영웅 관리소는 준비 중입니다!");
        }

        private void OnClickUpgrades()
        {
            PlayClickSfx();
            Debug.Log("[WorldMapView] Upgrades clicked (Not implemented).");
            UIHelper.ShowToast("업그레이드 기능은 준비 중입니다!");
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
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = true;
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

        private void ApplyWorldMapButtonSkinAndLayout()
        {
            SetButtonVisual(btnBack, "UI/Sprites/WorldMap/Icon_Back");
            SetButtonVisual(btnStage1, "UI/Sprites/WorldMap/Icon_Stage");
        }

        private void CreateBottomButtons()
        {
            // Hero Room
            if (btnHeroRoom.IsNotNull() && bottomBar.IsNotNull())
            {
                var config = new UIActionButtonItemConfig("영웅 관리소", Resources.Load<Sprite>("UI/Sprites/WorldMap/Icon_Hero"));
                btnHeroRoom.Init(config, OnClickHeroRoom);
            }

            // Upgrades
            if (btnUpgrades.IsNotNull() && bottomBar.IsNotNull())
            {
                var config = new UIActionButtonItemConfig("업그레이드", Resources.Load<Sprite>("UI/Sprites/WorldMap/Icon_Upgrade"));
                btnUpgrades.Init(config, OnClickUpgrades);
            }
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
