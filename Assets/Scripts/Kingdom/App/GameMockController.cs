using UnityEngine;
using UnityEngine.UI;
using Common.App;
using Kingdom.Save;
using Kingdom.WorldMap;

namespace Kingdom.App
{
#if DEV_MOCK
    /// <summary>
    /// GameScene에서 임시로 승리/패배를 시뮬레이션하기 위한 Mock 컨트롤러.
    /// 실제 게임 로직이 구현되기 전까지 월드맵 연동 테스트용으로 사용됩니다.
    /// </summary>
    public class GameMockController : MonoBehaviour
    {
        [Header("Mock UI")]
        [SerializeField] private Button btnWin;
        [SerializeField] private Button btnLose;
        [SerializeField] private Button btnWinStar1;
        [SerializeField] private Button btnWinStar2;
        [SerializeField] private Button btnWinStar3;

        private void Start()
        {
            // 씬에 버튼이 없으면 자동 생성 (편의성)
            if (btnWin == null && btnLose == null)
            {
                CreateMockUI();
            }

            if (btnWin != null) btnWin.onClick.AddListener(() => SimulateGameEnd(true, 3));
            if (btnLose != null) btnLose.onClick.AddListener(() => SimulateGameEnd(false, 0));
            
            if (btnWinStar1 != null) btnWinStar1.onClick.AddListener(() => SimulateGameEnd(true, 1));
            if (btnWinStar2 != null) btnWinStar2.onClick.AddListener(() => SimulateGameEnd(true, 2));
            if (btnWinStar3 != null) btnWinStar3.onClick.AddListener(() => SimulateGameEnd(true, 3));
        }

        private void CreateMockUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("MockCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            btnWin = CreateButton(canvas.transform, "BtnWin", "Win (3 Star)", new Vector2(-200, 0), Color.green);
            btnLose = CreateButton(canvas.transform, "BtnLose", "Lose", new Vector2(200, 0), Color.red);
            
            btnWinStar1 = CreateButton(canvas.transform, "BtnWin1", "Win (1 Star)", new Vector2(-200, -100), Color.yellow);
            btnWinStar2 = CreateButton(canvas.transform, "BtnWin2", "Win (2 Star)", new Vector2(-200, -200), Color.yellow);
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPos, Color color)
        {
            GameObject go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 60);
            rect.anchoredPosition = anchoredPos;

            Image img = go.GetComponent<Image>();
            img.color = color;

            GameObject txtGo = new GameObject("Text", typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            Text txt = txtGo.GetComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.resizeTextForBestFit = true;
            
            RectTransform txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private void SimulateGameEnd(bool isWin, int stars)
        {
            Debug.Log($"[GameMockController] Game End Simulation. Win: {isWin}, Stars: {stars}");

            if (isWin)
            {
                int currentStageId = WorldMapScene.SelectedStageId;
                if (currentStageId < 0) currentStageId = 1; // 기본값

                // 데이터 저장
                UserSaveData saveData = SaveManager.Instance.SaveData;
                saveData.SetStageCleared(currentStageId, stars, Random.Range(60f, 180f), WorldMapScene.SelectedDifficulty);

                // 월드맵 복귀 연출 설정
                WorldMapReturnAnimator.SetPendingReturnData(currentStageId, true, 120f, WorldMapScene.SelectedDifficulty);
            }

            // 월드맵 씬으로 복귀
            KingdomAppManager.Instance.ChangeScene(SCENES.WorldMapScene);
        }
    }
#endif
}
