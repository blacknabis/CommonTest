using System;
using Common.Extensions;
using Common.UI;
using Kingdom.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.App
{
    /// <summary>
    /// 인게임 HUD 뷰.
    /// KR 스타일 최소 슬롯(Lives/Gold/Wave/NextWave/Hero/Spell/Result)을 제공한다.
    /// </summary>
    public class GameView : BaseView
    {
        private const float HudMargin = 24f;

        [Header("Debug Buttons")]
        [SerializeField] private Button btnVictory;
        [SerializeField] private Button btnDefeat;

        [Header("Top HUD")]
        [SerializeField] private Button btnPause;
        [SerializeField] private Button btnNextWave;
        [SerializeField] private TextMeshProUGUI txtWaveInfo;
        [SerializeField] private TextMeshProUGUI txtStateInfo;
        [SerializeField] private TextMeshProUGUI txtLives;
        [SerializeField] private TextMeshProUGUI txtGold;

        [Header("Hero / Spell")]
        [SerializeField] private Image imgHeroPortrait;
        [SerializeField] private Button btnSpellReinforce;
        [SerializeField] private Button btnSpellRain;
        [SerializeField] private Image imgSpellReinforceCooldown;
        [SerializeField] private Image imgSpellRainCooldown;

        [Header("Result UI")]
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TextMeshProUGUI txtResultTitle;
        [SerializeField] private TextMeshProUGUI txtResultMessage;
        [SerializeField] private Button btnRetry;
        [SerializeField] private Button btnExit;

        private GameStateController _stateController;
        private RectTransform _hudRoot;
        private bool _isPausedVisual;

        public event Action NextWaveRequested;
        public event Action SpellReinforceRequested;
        public event Action SpellRainRequested;

        private void Awake()
        {
            BindLegacyRefs();
            EnsureHudRoot();
            CleanupLegacyRootChildren();
            EnsureCoreSlots();
            NormalizeLayout();
            EnsureResultSlot();
        }

        protected override void OnInit()
        {
            BindButton(btnVictory, OnVictory);
            BindButton(btnDefeat, OnDefeat);
            BindPauseButton(btnPause, OnPause);
            BindButton(btnNextWave, OnNextWave);
            BindButton(btnSpellReinforce, OnSpellReinforce);
            BindButton(btnSpellRain, OnSpellRain);
            BindButton(btnRetry, OnRetry);
            BindButton(btnExit, OnExitToMap);

            HideResult();
            SetPauseVisual(false);
            UpdateWaveInfo(1, 1);
            UpdateResourceInfo(20, 100);
            SetHeroPortrait(null);
            SetSpellCooldown("reinforce", 0f);
            SetSpellCooldown("rain", 0f);
        }

        protected override void OnEnter(object[] data)
        {
            base.OnEnter(data);
            SetStateText("Prepare");
            HideResult();
        }

        protected override void OnExit()
        {
            if (_stateController != null)
            {
                _stateController.WaveChanged -= OnWaveChanged;
                _stateController.StateChanged -= OnStateChanged;
            }

            Time.timeScale = 1f;
            base.OnExit();
        }

        public void Bind(GameStateController controller)
        {
            if (_stateController != null)
            {
                _stateController.WaveChanged -= OnWaveChanged;
                _stateController.StateChanged -= OnStateChanged;
            }

            _stateController = controller;
            if (_stateController == null)
            {
                return;
            }

            _stateController.WaveChanged += OnWaveChanged;
            _stateController.StateChanged += OnStateChanged;
            int currentWave = _stateController.CurrentWave <= 0 ? 1 : _stateController.CurrentWave;
            UpdateWaveInfo(currentWave, _stateController.TotalWaves);
            OnStateChanged(_stateController.CurrentState);
        }

        public void UpdateWaveInfo(int current, int total)
        {
            if (txtWaveInfo != null)
            {
                txtWaveInfo.text = $"WAVE {Mathf.Max(1, current)} / {Mathf.Max(1, total)}";
            }
        }

        public void UpdateResourceInfo(int lives, int gold)
        {
            if (txtLives != null) txtLives.text = $"LIVES {Mathf.Max(0, lives)}";
            if (txtGold != null) txtGold.text = $"GOLD {Mathf.Max(0, gold)}";
        }

        public void SetHeroPortrait(Sprite portrait)
        {
            if (imgHeroPortrait == null)
            {
                return;
            }

            imgHeroPortrait.sprite = portrait;
            imgHeroPortrait.preserveAspect = true;
            imgHeroPortrait.color = portrait != null
                ? Color.white
                : new Color(1f, 1f, 1f, 0.18f);
        }

        public void SetNextWaveInteractable(bool interactable)
        {
            if (btnNextWave != null)
            {
                btnNextWave.interactable = interactable;
            }
        }

        public void SetSpellCooldown(string spellId, float normalized01)
        {
            float clamped = Mathf.Clamp01(normalized01);
            if (string.Equals(spellId, "reinforce", StringComparison.OrdinalIgnoreCase))
            {
                ApplyCooldownVisual(imgSpellReinforceCooldown, clamped);
                return;
            }

            ApplyCooldownVisual(imgSpellRainCooldown, clamped);
        }

        public void ShowResult(bool isVictory, string message = null)
        {
            if (resultRoot != null) resultRoot.SetActive(true);
            if (txtResultTitle != null) txtResultTitle.text = isVictory ? "VICTORY" : "DEFEAT";
            if (txtResultMessage != null)
            {
                txtResultMessage.text = string.IsNullOrWhiteSpace(message)
                    ? (isVictory ? "스테이지 클리어!" : "방어선이 돌파되었습니다.")
                    : message;
            }
        }

        public void HideResult()
        {
            if (resultRoot != null) resultRoot.SetActive(false);
        }

        public void SetPauseVisual(bool isPaused)
        {
            _isPausedVisual = isPaused;
            if (txtStateInfo != null && isPaused)
            {
                txtStateInfo.text = "Pause";
            }

            SetPauseButtonLabel(isPaused ? "Resume" : "Pause");
        }

        private void OnVictory()
        {
            ShowResult(true);
        }

        private void OnDefeat()
        {
            ShowResult(false);
        }

        private void OnPause()
        {
            if (_stateController == null)
            {
                Debug.Log("[GameView] Pause clicked, but state controller is not bound.");
                return;
            }

            _stateController.TogglePause();
        }

        private void OnNextWave()
        {
            NextWaveRequested?.Invoke();
        }

        private void OnSpellReinforce()
        {
            SpellReinforceRequested?.Invoke();
        }

        private void OnSpellRain()
        {
            SpellRainRequested?.Invoke();
        }

        private void OnRetry()
        {
            KingdomAppManager.Instance.ChangeScene(SCENES.GameScene);
        }

        private void OnExitToMap()
        {
            KingdomAppManager.Instance.ChangeScene(SCENES.WorldMapScene);
        }

        private void OnWaveChanged(int current, int total)
        {
            UpdateWaveInfo(current, total);
        }

        private void OnStateChanged(GameFlowState state)
        {
            SetPauseVisual(state == GameFlowState.Pause);
            SetStateText(state.ToString());

            if (state == GameFlowState.Result)
            {
                ShowResult(true, "결과 처리 연결 대기 중");
            }
        }

        private void SetStateText(string state)
        {
            if (txtStateInfo != null && !_isPausedVisual)
            {
                txtStateInfo.text = state;
            }
            else if (txtStateInfo != null && state != "Pause")
            {
                txtStateInfo.text = state;
            }
        }

        private void ApplyCooldownVisual(Image image, float amount01)
        {
            if (image == null) return;
            image.fillAmount = amount01;
            image.gameObject.SetActive(amount01 > 0f);
        }

        private void BindButton(Button button, Action callback)
        {
            if (button != null)
            {
                button.SetOnClickWithCooldown(callback);
            }
        }

        private static void BindPauseButton(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            // Pause/Resume 토글은 timescale과 무관하게 즉시 입력 가능해야 한다.
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }

        private void SetPauseButtonLabel(string label)
        {
            if (btnPause == null)
            {
                return;
            }

            var text = btnPause.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private void BindLegacyRefs()
        {
            btnVictory = btnVictory != null ? btnVictory : FindButton("btnVictory");
            btnDefeat = btnDefeat != null ? btnDefeat : FindButton("btnDefeat");
            btnPause = btnPause != null ? btnPause : FindButton("btnPause");
            btnNextWave = btnNextWave != null ? btnNextWave : FindButton("btnNextWave");
            btnSpellReinforce = btnSpellReinforce != null ? btnSpellReinforce : FindButton("btnSpellReinforce");
            btnSpellRain = btnSpellRain != null ? btnSpellRain : FindButton("btnSpellRain");
            btnRetry = btnRetry != null ? btnRetry : FindButton("ResultRoot/btnRetry");
            btnExit = btnExit != null ? btnExit : FindButton("ResultRoot/btnExit");

            txtWaveInfo = txtWaveInfo != null ? txtWaveInfo : FindText("txtWaveInfo");
            txtStateInfo = txtStateInfo != null ? txtStateInfo : FindText("txtStateInfo");
            txtLives = txtLives != null ? txtLives : FindText("txtLives");
            txtGold = txtGold != null ? txtGold : FindText("txtGold");
            txtResultTitle = txtResultTitle != null ? txtResultTitle : FindText("ResultRoot/txtResultTitle");
            txtResultMessage = txtResultMessage != null ? txtResultMessage : FindText("ResultRoot/txtResultMessage");

            imgHeroPortrait = imgHeroPortrait != null ? imgHeroPortrait : FindImage("imgHeroPortrait");
            imgSpellReinforceCooldown = imgSpellReinforceCooldown != null
                ? imgSpellReinforceCooldown
                : FindImage("btnSpellReinforce/CooldownFill");
            imgSpellRainCooldown = imgSpellRainCooldown != null
                ? imgSpellRainCooldown
                : FindImage("btnSpellRain/CooldownFill");

            if (resultRoot == null)
            {
                var tr = transform.Find("ResultRoot");
                if (tr != null) resultRoot = tr.gameObject;
            }
        }

        private void EnsureHudRoot()
        {
            var found = transform.Find("HUDRoot");
            if (found != null)
            {
                _hudRoot = found.GetComponent<RectTransform>();
            }

            if (_hudRoot == null)
            {
                _hudRoot = new GameObject("HUDRoot", typeof(RectTransform)).GetComponent<RectTransform>();
                _hudRoot.SetParent(transform, false);
            }

            Stretch(_hudRoot);
        }

        private void EnsureCoreSlots()
        {
            txtWaveInfo ??= CreateText("txtWaveInfo");
            txtStateInfo ??= CreateText("txtStateInfo");
            txtLives ??= CreateText("txtLives");
            txtGold ??= CreateText("txtGold");

            btnPause ??= CreateButton("btnPause", "Pause");
            btnNextWave ??= CreateButton("btnNextWave", "Next Wave");
            btnVictory ??= CreateButton("btnVictory", "Victory");
            btnDefeat ??= CreateButton("btnDefeat", "Defeat");
            btnSpellReinforce ??= CreateButton("btnSpellReinforce", "Reinforce");
            btnSpellRain ??= CreateButton("btnSpellRain", "Rain");

            if (imgHeroPortrait == null)
            {
                var go = new GameObject("imgHeroPortrait", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_hudRoot, false);
                imgHeroPortrait = go.GetComponent<Image>();
                imgHeroPortrait.color = new Color(1f, 1f, 1f, 0.18f);
            }

            EnsureCooldownOverlay(btnSpellReinforce, ref imgSpellReinforceCooldown);
            EnsureCooldownOverlay(btnSpellRain, ref imgSpellRainCooldown);
        }

        private void NormalizeLayout()
        {
            Reparent(btnVictory);
            Reparent(btnDefeat);
            Reparent(btnPause);
            Reparent(btnNextWave);
            Reparent(btnSpellReinforce);
            Reparent(btnSpellRain);
            Reparent(txtWaveInfo);
            Reparent(txtStateInfo);
            Reparent(txtLives);
            Reparent(txtGold);
            Reparent(imgHeroPortrait);

            PlaceTextTopLeft(txtWaveInfo, new Vector2(HudMargin, -HudMargin), 40);
            PlaceTextTopLeft(txtStateInfo, new Vector2(HudMargin, -HudMargin - 50f), 30);
            PlaceTextTopLeft(txtLives, new Vector2(HudMargin, -HudMargin - 95f), 28);
            PlaceTextTopLeft(txtGold, new Vector2(HudMargin, -HudMargin - 135f), 28);

            PlaceButton(btnPause, new Vector2(1f, 1f), new Vector2(-HudMargin, -HudMargin), new Vector2(120f, 56f), new Color(0.18f, 0.2f, 0.25f, 0.9f), "Pause");
            PlaceButton(btnNextWave, new Vector2(1f, 1f), new Vector2(-HudMargin, -HudMargin - 66f), new Vector2(180f, 56f), new Color(0.45f, 0.2f, 0.2f, 0.9f), "Next Wave");
            PlaceButton(btnVictory, new Vector2(1f, 0f), new Vector2(-HudMargin, HudMargin + 70f), new Vector2(150f, 56f), new Color(0.2f, 0.55f, 0.2f, 0.9f), "Victory");
            PlaceButton(btnDefeat, new Vector2(1f, 0f), new Vector2(-HudMargin, HudMargin), new Vector2(150f, 56f), new Color(0.65f, 0.2f, 0.2f, 0.9f), "Defeat");
            PlaceButton(btnSpellReinforce, new Vector2(0f, 0f), new Vector2(HudMargin, HudMargin), new Vector2(160f, 56f), new Color(0.2f, 0.35f, 0.6f, 0.9f), "Reinforce");
            PlaceButton(btnSpellRain, new Vector2(0f, 0f), new Vector2(HudMargin + 170f, HudMargin), new Vector2(140f, 56f), new Color(0.55f, 0.35f, 0.2f, 0.9f), "Rain");

            if (imgHeroPortrait != null)
            {
                var rect = imgHeroPortrait.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(HudMargin, HudMargin + 72f);
                rect.sizeDelta = new Vector2(84f, 84f);
            }
        }

        private void EnsureResultSlot()
        {
            if (resultRoot != null)
            {
                resultRoot.transform.SetParent(_hudRoot, false);
                SetupResultRootRect(resultRoot.GetComponent<RectTransform>());
                return;
            }

            var root = new GameObject("ResultRoot", typeof(RectTransform));
            root.transform.SetParent(_hudRoot, false);
            resultRoot = root;
            SetupResultRootRect(root.GetComponent<RectTransform>());

            txtResultTitle = txtResultTitle != null ? txtResultTitle : CreateResultText(root.transform, "txtResultTitle", 42f, new Vector2(0f, -36f), new Vector2(600f, 80f), true);
            txtResultMessage = txtResultMessage != null ? txtResultMessage : CreateResultText(root.transform, "txtResultMessage", 28f, new Vector2(0f, 20f), new Vector2(640f, 120f), false);
            btnRetry = btnRetry != null ? btnRetry : CreateResultButton(root.transform, "btnRetry", "Retry", new Vector2(-120f, 36f));
            btnExit = btnExit != null ? btnExit : CreateResultButton(root.transform, "btnExit", "Exit", new Vector2(120f, 36f));
        }

        private TextMeshProUGUI CreateText(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_hudRoot, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.fontSize = 28;
            return text;
        }

        private Button CreateButton(string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_hudRoot, false);
            var button = go.GetComponent<Button>();

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var text = txtGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 28;
            text.color = Color.black;

            var txtRect = txtGo.GetComponent<RectTransform>();
            Stretch(txtRect);
            return button;
        }

        private static TextMeshProUGUI CreateResultText(Transform parent, string name, float size, Vector2 anchoredPos, Vector2 box, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = size;

            var rect = text.GetComponent<RectTransform>();
            if (top)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = box;
            return text;
        }

        private static Button CreateResultButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(180f, 70f);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var text = txtGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 30;
            text.color = Color.black;
            Stretch(txtGo.GetComponent<RectTransform>());

            return go.GetComponent<Button>();
        }

        private void Reparent(Component component)
        {
            if (component == null) return;
            component.transform.SetParent(_hudRoot, false);
        }

        private static void PlaceTextTopLeft(TextMeshProUGUI text, Vector2 pos, float fontSize)
        {
            if (text == null) return;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(560f, 46f);
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Left;
        }

        private static void PlaceButton(Button button, Vector2 anchor, Vector2 pos, Vector2 size, Color color, string label)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var image = button.GetComponent<Image>();
            if (image != null) image.color = color;

            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 26f;
            }
        }

        private static void EnsureCooldownOverlay(Button owner, ref Image cooldownImage)
        {
            if (owner == null) return;
            var ownerRect = owner.GetComponent<RectTransform>();
            Transform existing = ownerRect.Find("CooldownFill");
            if (existing != null)
            {
                cooldownImage = existing.GetComponent<Image>();
            }

            if (cooldownImage == null)
            {
                var go = new GameObject("CooldownFill", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(ownerRect, false);
                cooldownImage = go.GetComponent<Image>();
                cooldownImage.color = new Color(0f, 0f, 0f, 0.45f);
                cooldownImage.type = Image.Type.Filled;
                cooldownImage.fillMethod = Image.FillMethod.Vertical;
                cooldownImage.fillOrigin = (int)Image.OriginVertical.Top;
            }

            Stretch(cooldownImage.GetComponent<RectTransform>());
            cooldownImage.fillAmount = 0f;
            cooldownImage.gameObject.SetActive(false);
        }

        private static void SetupResultRootRect(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 360f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private Button FindButton(string path)
        {
            var tr = transform.Find(path);
            return tr != null ? tr.GetComponent<Button>() : null;
        }

        private TextMeshProUGUI FindText(string path)
        {
            var tr = transform.Find(path);
            return tr != null ? tr.GetComponent<TextMeshProUGUI>() : null;
        }

        private Image FindImage(string path)
        {
            var tr = transform.Find(path);
            return tr != null ? tr.GetComponent<Image>() : null;
        }

        private void CleanupLegacyRootChildren()
        {
            string[] legacyNames =
            {
                "btnVictory",
                "btnDefeat",
                "btnPause",
                "btnNextWave",
                "txtWaveInfo",
                "txtStateInfo",
                "txtLives",
                "txtGold",
                "imgHeroPortrait",
                "btnSpellReinforce",
                "btnSpellRain"
            };

            for (int i = 0; i < legacyNames.Length; i++)
            {
                var tr = transform.Find(legacyNames[i]);
                if (tr == null)
                {
                    continue;
                }

                if (IsBoundTransform(tr))
                {
                    // 현재 바인딩된 오브젝트면 유지
                    continue;
                }

                Destroy(tr.gameObject);
            }
        }

        private bool IsBoundTransform(Transform tr)
        {
            if (tr == null)
            {
                return false;
            }

            return tr == GetTransform(btnVictory)
                || tr == GetTransform(btnDefeat)
                || tr == GetTransform(btnPause)
                || tr == GetTransform(btnNextWave)
                || tr == GetTransform(txtWaveInfo)
                || tr == GetTransform(txtStateInfo)
                || tr == GetTransform(txtLives)
                || tr == GetTransform(txtGold)
                || tr == GetTransform(imgHeroPortrait)
                || tr == GetTransform(btnSpellReinforce)
                || tr == GetTransform(btnSpellRain);
        }

        private static Transform GetTransform(Component c)
        {
            return c != null ? c.transform : null;
        }
    }
}
