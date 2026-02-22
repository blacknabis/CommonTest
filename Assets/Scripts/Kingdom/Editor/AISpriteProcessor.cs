using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Common.Extensions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Newtonsoft.Json;
using CatSudoku.Editor;

namespace Kingdom.Editor
{
    public enum SlicingMode
    {
        AutoDivide, // Width / Cols
        CustomGrid, // Manual Offset/Size/Spacing
        SmartSlice  // Alpha Island Detection
    }

    public enum ChannelComparison
    {
        Ignore,
        GreaterOrEqual,
        LessOrEqual
    }

    public enum SpriteActionGroup
    {
        Unknown,
        Idle,
        Walk,
        Attack,
        Die
    }

    public enum AISpriteProcessorTab
    {
        Common,
        AIPreprocess,
        BaseUnit,
        TowerBase
    }

    public enum BaseUnitTargetType
    {
        HeroConfig,
        EnemyConfig,
        BarracksSoldierConfig
    }

    /// <summary>
    /// 생성형 이미지(스프라이트 시트)를 전처리하고 자르는 도구 (개선 버전)
    /// - 실시간 프리뷰 및 격자 시각화 지원
    /// - 배경색 제거(크로마키)
    /// - 격자 기반(자동/커스텀) 슬라이싱 및 패딩 처리
    /// </summary>
    public class AISpriteProcessor : EditorWindow
    {
        [Serializable]
        private sealed class BaseUnitBindingContext
        {
            public BaseUnitTargetType targetType = BaseUnitTargetType.HeroConfig;
            public UnityEngine.Object targetAsset;
            public bool useManualActionGroup = true;
            public SpriteActionGroup actionGroup = SpriteActionGroup.Unknown;
            public bool autoApplyAfterProcess;
        }

        [Serializable]
        private sealed class TowerBindingContext
        {
            public UnityEngine.Object targetAsset;
            public int levelIndex = 1;
            public bool autoApplyAfterProcess;
        }

        [System.Serializable]
        private sealed class AISpriteProcessConfig
        {
            public int schemaVersion = 1;
            public bool hasBindingContext;
            public bool removeBackground = true;
            public Color keyColor = new Color(1f, 0f, 1f, 1f); // 기본 키 컬러: 마젠타 #FF00FF
            public float tolerance = 0.1f;
            public bool useRgbRangeFilter = false;
            public int thresholdR = 150;
            public int thresholdG = 50;
            public int thresholdB = 150;
            public ChannelComparison compareR = ChannelComparison.GreaterOrEqual;
            public ChannelComparison compareG = ChannelComparison.LessOrEqual;
            public ChannelComparison compareB = ChannelComparison.GreaterOrEqual;
            public SlicingMode slicingMode = SlicingMode.AutoDivide;
            public int rows = 2;
            public int cols = 4;
            public int innerPadding = 4;
            public Vector2 offset = Vector2.zero;
            public Vector2 cellSize = new Vector2(100f, 100f);
            public Vector2 spacing = Vector2.zero;
            public float smartSliceAlphaThreshold = 0.1f;
            public int smartSliceMinPixels = 64;
            public int smartSliceOuterPadding = 2;
            public bool smartSliceSplitActionsByRows = false;
            public int smartSliceActionRowCount = 4;
            public bool normalizeFrames = true;
            public bool emitManifest = true;
            public bool useManualActionGroup = true;
            public SpriteActionGroup singleSourceActionGroup = SpriteActionGroup.Unknown;
            public AISpriteProcessorTab activeTab = AISpriteProcessorTab.Common;
            public BaseUnitTargetType baseUnitTargetType = BaseUnitTargetType.HeroConfig;
            public bool baseUnitUseManualActionGroup = true;
            public SpriteActionGroup baseUnitActionGroup = SpriteActionGroup.Unknown;
            public bool baseUnitAutoApplyAfterProcess;
            public int towerLevelIndex = 1;
            public bool towerAutoApplyAfterProcess;
        }

        [Serializable]
        private sealed class AISpriteManifest
        {
            public int version = 1;
            public string updatedAtUtc = string.Empty;
            public List<string> sourceFiles = new List<string>();
            public List<ManifestActionRecord> actions = new List<ManifestActionRecord>();
        }

        [Serializable]
        private sealed class ManifestActionRecord
        {
            public string actionGroup = "unknown";
            public string sourceFile = string.Empty;
            public string outputTexture = string.Empty;
            public int frameCount;
            public int maxFrameWidth;
            public int maxFrameHeight;
            public float pivotX = DefaultPivotX;
            public float pivotY = DefaultPivotY;
            public bool normalizedFrames;
            public string slicingMode = string.Empty;
            public ManifestOptions options = new ManifestOptions();
            public List<string> warnings = new List<string>();
        }

        [Serializable]
        private sealed class ManifestOptions
        {
            public bool removeBackground;
            public bool useRgbRangeFilter;
            public float tolerance;
            public int thresholdR;
            public int thresholdG;
            public int thresholdB;
            public ChannelComparison compareR;
            public ChannelComparison compareG;
            public ChannelComparison compareB;
            public bool cropToSelection;
            public bool normalizeFrames;
            public SlicingMode slicingMode;
            public int rows;
            public int cols;
            public int innerPadding;
            public float smartSliceAlphaThreshold;
            public int smartSliceMinPixels;
            public int smartSliceOuterPadding;
            public bool smartSliceSplitActionsByRows;
            public int smartSliceActionRowCount;
        }

        private const string PresetFolderPath = "Assets/Editor/AISpritePresets";
        private const string ManifestFileName = "manifest.json";
        private const float DefaultPivotX = 0.5f;
        private const float DefaultPivotY = 0f;

        private Texture2D _sourceTexture;
        private bool _useManualActionGroup = true;
        private SpriteActionGroup _singleSourceActionGroup = SpriteActionGroup.Unknown;
        private SlicingMode _slicingMode = SlicingMode.AutoDivide;

        // 공통 설정
        private int _rows = 2;
        private int _cols = 4;
        private int _innerPadding = 4; // 픽셀 단위 (실제 스프라이트 영역 축소량)

        // 커스텀 격자 설정
        private Vector2 _offset = Vector2.zero;   // 격자 시작 위치
        private Vector2 _cellSize = new Vector2(100, 100);
        private Vector2 _spacing = Vector2.zero;  // 셀 간 간격

        // 전처리 설정
        private Color _removeColor = Color.white;
        private float _tolerance = 0.1f;
        private bool _removeBackground = true;
        private bool _useRgbRangeFilter = false;
        private int _thresholdR = 150;
        private int _thresholdG = 50;
        private int _thresholdB = 150;
        private ChannelComparison _compareR = ChannelComparison.GreaterOrEqual;
        private ChannelComparison _compareG = ChannelComparison.LessOrEqual;
        private ChannelComparison _compareB = ChannelComparison.GreaterOrEqual;
        private bool _cropToSelection = true;
        private bool _normalizeFrames = true;
        private bool _emitManifest = true;
        private float _smartSliceAlphaThreshold = 0.1f;
        private int _smartSliceMinPixels = 64;
        private int _smartSliceOuterPadding = 2;
        private bool _smartSliceSplitActionsByRows = false;
        private int _smartSliceActionRowCount = 4;
        
        // 프리뷰 상태
        private Vector2 _scrollPosition;
        private float _previewZoom = 1.0f;
        private bool _previewPlay = true;
        private float _previewFps = 8f;
        private int _previewFrameIndex;
        private double _previewLastTickTime;
        private bool _previewApplyProcessing = true;
        private Texture2D _previewProcessedTexture;
        private int _previewProcessedHash = int.MinValue;
        private List<Rect> _previewFrameRectsCache = new List<Rect>();
        private int _previewFrameRectsTextureId = 0;
        private bool _previewFrameRectsDirty = true;

        private readonly List<string> _presetNames = new List<string>();
        private int _selectedPresetIndex = -1;
        private string _presetNameInput = "Default";
        private bool _presetListDirty = true;
        private bool _presetDeleteArmed;
        private string _presetDeleteTarget = string.Empty;
        private AISpriteProcessorTab _activeTab = AISpriteProcessorTab.Common;
        private readonly BaseUnitBindingContext _baseUnitContext = new BaseUnitBindingContext();
        private readonly TowerBindingContext _towerContext = new TowerBindingContext();
        private string _lastProcessedAssetPath = string.Empty;
        private const string ComfyUiRembgNodeType = "Image Remove Background (rembg)";
        private float _animatorClipFps = 8f;
        private bool _showBaseUnitAdvancedActions;

        // 자동 검증/회귀 환경에서 모달로 멈추지 않도록 기본적으로 로그 알림을 사용한다.
        private static void NotifyInfo(string title, string message)
        {
            Debug.Log($"[AISpriteProcessor] {title}: {message}");
        }

        private static void NotifyWarning(string title, string message)
        {
            Debug.LogWarning($"[AISpriteProcessor] {title}: {message}");
        }

        private static void NotifyError(string title, string message)
        {
            Debug.LogError($"[AISpriteProcessor] {title}: {message}");
        }

        [MenuItem("Tools/Common/AI Sprite Processor")]
        // 에디터 창을 생성하고 초기 표시 상태를 설정한다.
        public static void ShowWindow()
        {
            var window = GetWindow<AISpriteProcessor>("AI Sprite Processor");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }

        [MenuItem("Tools/Common/AI Sprite Processor/Run Preset Load Regression")]
        // 저장된 모든 프리셋에 대해 로드 회귀 검증을 일괄 실행한다.
        public static void RunPresetLoadRegressionMenu()
        {
            var window = GetWindow<AISpriteProcessor>("AI Sprite Processor");
            window.minSize = new Vector2(1000, 700);
            window.Show();
            window.RunPresetLoadRegression();
        }

        // 창 활성화 시 프리셋 목록을 갱신한다.
        private void OnEnable()
        {
            RefreshPresetList();
        }

        // 창 비활성화 시 프리뷰용 임시 텍스처를 정리한다.
        private void OnDisable()
        {
            if (_previewProcessedTexture != null)
            {
                DestroyImmediate(_previewProcessedTexture);
                _previewProcessedTexture = null;
            }

            _previewProcessedHash = int.MinValue;
            InvalidatePreviewFrameRectsCache();
        }

        // 좌측 설정 패널과 우측 미리보기 패널을 렌더링한다.
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // 1. 좌측 설정 패널
            EditorGUILayout.BeginVertical(GUILayout.Width(320));
            DrawSettingsPanel();
            EditorGUILayout.EndVertical();

            // 구분선
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

            // 2. 우측 프리뷰 패널
            EditorGUILayout.BeginVertical();
            DrawPreviewPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // 현재 탭에 맞는 설정 화면을 그린다.
        private void DrawSettingsPanel()
        {
            DrawTabToolbar();
            EditorGUILayout.Space(6f);

            switch (_activeTab)
            {
                case AISpriteProcessorTab.AIPreprocess:
                    DrawAIPreprocessTab();
                    break;
                case AISpriteProcessorTab.BaseUnit:
                    DrawBaseUnitTab();
                    break;
                case AISpriteProcessorTab.TowerBase:
                    DrawTowerBaseTab();
                    break;
                default:
                    DrawCommonLayout();
                    break;
            }
        }

        // 공통/베이스 유닛/타워 베이스 탭 전환 툴바를 그린다.
        private void DrawTabToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_activeTab == AISpriteProcessorTab.Common, "Common", EditorStyles.toolbarButton))
            {
                _activeTab = AISpriteProcessorTab.Common;
            }

            if (GUILayout.Toggle(_activeTab == AISpriteProcessorTab.AIPreprocess, "AIPreprocess", EditorStyles.toolbarButton))
            {
                _activeTab = AISpriteProcessorTab.AIPreprocess;
            }

            if (GUILayout.Toggle(_activeTab == AISpriteProcessorTab.BaseUnit, "BaseUnit", EditorStyles.toolbarButton))
            {
                _activeTab = AISpriteProcessorTab.BaseUnit;
            }

            if (GUILayout.Toggle(_activeTab == AISpriteProcessorTab.TowerBase, "TowerBase", EditorStyles.toolbarButton))
            {
                _activeTab = AISpriteProcessorTab.TowerBase;
            }

            EditorGUILayout.EndHorizontal();
        }

        // 공통 전처리/슬라이싱 설정 화면을 그린다.
        private void DrawCommonLayout()
        {
            GUILayout.Label("이미지 설정 (Image Settings)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawSourceTextureField();

            EditorGUILayout.Space(5);
            _useManualActionGroup = EditorGUILayout.Toggle("액션 그룹 수동 지정", _useManualActionGroup);
            if (_useManualActionGroup)
            {
                _singleSourceActionGroup = (SpriteActionGroup)EditorGUILayout.EnumPopup("Action Group", _singleSourceActionGroup);
            }
            else
            {
                EditorGUILayout.HelpBox("파일명 키워드(idle/walk/run/attack/atk/die/death/dead)로 액션 그룹을 자동 판별합니다. 미검출은 unknown 처리됩니다.", MessageType.None);
            }
            _baseUnitContext.useManualActionGroup = _useManualActionGroup;
            _baseUnitContext.actionGroup = _singleSourceActionGroup;

            EditorGUILayout.Space(10);
            DrawPresetSection();

            EditorGUILayout.Space(10);
            DrawSlicingAndProcessingOptions();

            EditorGUILayout.Space(10);
            GUI.enabled = _sourceTexture != null;
            if (GUILayout.Button("처리 및 자르기 (Process & Slice)", GUILayout.Height(40)))
            {
                if (!TryValidateInputs(out string validationError))
                {
                    NotifyError("입력값 오류", validationError);
                    GUI.enabled = true;
                    return;
                }

                ProcessSpriteSheet();
            }
            GUI.enabled = true;
        }

        private void DrawSlicingAndProcessingOptions()
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("자르기 설정 (Slicing)", EditorStyles.boldLabel);

            _slicingMode = (SlicingMode)EditorGUILayout.EnumPopup("Mode", _slicingMode);

            EditorGUILayout.Space(5);
            if (_slicingMode != SlicingMode.SmartSlice)
            {
                _rows = EditorGUILayout.IntField("행 (Rows)", _rows);
                _cols = EditorGUILayout.IntField("열 (Cols)", _cols);
                _innerPadding = Mathf.Max(0, EditorGUILayout.IntField("내부 여백 (Padding px)", _innerPadding));
            }

            if (_slicingMode == SlicingMode.CustomGrid)
            {
                EditorGUILayout.Space(5);
                GUILayout.Label("Grid Details", EditorStyles.miniBoldLabel);
                _offset = EditorGUILayout.Vector2Field("시작 위치 (Offset)", _offset);
                _cellSize = EditorGUILayout.Vector2Field("셀 크기 (Size)", _cellSize);
                _spacing = EditorGUILayout.Vector2Field("간격 (Spacing)", _spacing);

                if (GUILayout.Button("Auto-Fit to Image"))
                {
                    RecalculateAutoGrid();
                }
            }
            else if (_slicingMode == SlicingMode.SmartSlice)
            {
                EditorGUILayout.Space(5);
                GUILayout.Label("Smart Slice Details", EditorStyles.miniBoldLabel);
                _smartSliceAlphaThreshold = EditorGUILayout.Slider("Alpha Threshold", _smartSliceAlphaThreshold, 0.001f, 1f);
                _smartSliceMinPixels = Mathf.Max(1, EditorGUILayout.IntField("Min Island Pixels", _smartSliceMinPixels));
                _smartSliceOuterPadding = Mathf.Max(0, EditorGUILayout.IntField("Outer Padding", _smartSliceOuterPadding));
                _smartSliceSplitActionsByRows = EditorGUILayout.Toggle("4행 액션 분리 사용", _smartSliceSplitActionsByRows);
                if (_smartSliceSplitActionsByRows)
                {
                    _smartSliceActionRowCount = Mathf.Max(2, EditorGUILayout.IntField("액션 행 수", _smartSliceActionRowCount));
                    EditorGUILayout.HelpBox("행 기준으로 rect를 분류해 top->bottom 순서로 idle/walk/attack/die(4행) 또는 row00..로 이름을 부여합니다.", MessageType.None);
                    EditorGUILayout.HelpBox("이 모드에서는 Action Group 수동/자동 설정을 사용하지 않습니다.", MessageType.None);
                }
                EditorGUILayout.HelpBox("알파 픽셀 덩어리를 자동 감지해 스프라이트를 자릅니다.", MessageType.None);
            }
            else
            {
            // 자동 모드에서는 값이 자동 계산됨을 표시
                if (_sourceTexture != null)
                {
                    int safeCols = Mathf.Max(1, _cols);
                    int safeRows = Mathf.Max(1, _rows);
                    EditorGUILayout.HelpBox($"Cell: {_sourceTexture.width / safeCols} x {_sourceTexture.height / safeRows}", MessageType.None);
                }
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("전처리 설정 (Processing)", EditorStyles.boldLabel);
            _removeBackground = EditorGUILayout.Toggle("배경 제거 (Remove BG)", _removeBackground);
            _cropToSelection = EditorGUILayout.Toggle("선택 영역만 저장 (Crop)", _cropToSelection);
            _normalizeFrames = EditorGUILayout.Toggle("프레임 정규화 (Normalize Frames)", _normalizeFrames);
            _emitManifest = EditorGUILayout.Toggle("manifest.json 기록", _emitManifest);
            _previewApplyProcessing = EditorGUILayout.Toggle("프리뷰에 전처리 적용", _previewApplyProcessing);
            if (_removeBackground)
            {
                _useRgbRangeFilter = EditorGUILayout.Toggle("RGB 범위 필터 사용", _useRgbRangeFilter);
                if (_useRgbRangeFilter)
                {
                    EditorGUILayout.HelpBox("각 RGB 채널을 기준값과 비교(이상/이하)하여 배경 제거", MessageType.None);

                    _compareR = (ChannelComparison)EditorGUILayout.EnumPopup("R 조건", _compareR);
                    _thresholdR = EditorGUILayout.IntSlider("R 기준값", _thresholdR, 0, 255);

                    _compareG = (ChannelComparison)EditorGUILayout.EnumPopup("G 조건", _compareG);
                    _thresholdG = EditorGUILayout.IntSlider("G 기준값", _thresholdG, 0, 255);

                    _compareB = (ChannelComparison)EditorGUILayout.EnumPopup("B 조건", _compareB);
                    _thresholdB = EditorGUILayout.IntSlider("B 기준값", _thresholdB, 0, 255);

                    if (GUILayout.Button("마젠타 범위 프리셋 (R150+ / G50- / B150+)"))
                    {
                        _compareR = ChannelComparison.GreaterOrEqual;
                        _thresholdR = 150;
                        _compareG = ChannelComparison.LessOrEqual;
                        _thresholdG = 50;
                        _compareB = ChannelComparison.GreaterOrEqual;
                        _thresholdB = 150;
                    }
                }
                else
                {
                    _removeColor = EditorGUILayout.ColorField("제거 색상 (Key Color)", _removeColor);
                    _tolerance = EditorGUILayout.Slider("허용 오차 (Tolerance)", _tolerance, 0f, 1f);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                InvalidatePreviewFrameRectsCache();
            }
        }

        private void DrawAIPreprocessTab()
        {
            GUILayout.Label("AI 이미지 전처리 (ComfyUI RemBG)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "이 탭은 ComfyUI 연동 전용입니다.\n" +
                "필수 조건:\n" +
                "1) ComfyUI 서버 실행 (http://127.0.0.1:8188)\n" +
                "2) custom node 'Image Remove Background (rembg)' 설치\n" +
                "위 조건이 충족되지 않으면 이 탭 처리는 실패합니다.",
                MessageType.Warning);
            DrawSourceTextureField();

            EditorGUILayout.Space(10);
            GUI.enabled = _sourceTexture != null;
            if (GUILayout.Button("ComfyUI RemBG 배경 제거 및 저장 (Remove BG & Save PNG)", GUILayout.Height(45)))
            {
                ProcessComfyUIRemBGAsync();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("이 탭은 ComfyUI의 RemBG 노드를 호출하여, 선택된 이미지의 배경을 인공지능으로 분리하고 투명한 배경의 PNG로 저장합니다. 완료 후 생성된 이미지는 '_Transparent' 접미사가 붙어 동일한 폴더에 저장됩니다. (ComfyUI 웹서버가 실행 중이어야 합니다.)", MessageType.Info);
        }

        private async void ProcessComfyUIRemBGAsync()
        {
            if (_sourceTexture == null)
            {
                NotifyError("Error", "Source Texture is not assigned.");
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(_sourceTexture);
            if (string.IsNullOrEmpty(sourcePath)) return;

            string dir = Path.GetDirectoryName(sourcePath);
            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            string processedPath = Path.Combine(dir, $"{fileName}_Transparent.png").Replace('\\', '/');
            string workflowPath = "Assets/Editor/ComfyUI/workflow_rembg_preprocess.json";

            if (!File.Exists(workflowPath))
            {
                NotifyError("Error", $"RemBG workflow not found at {workflowPath}");
                return;
            }

            EditorUtility.DisplayProgressBar("ComfyUI RemBG", $"Uploading {fileName}...", 0.1f);

            try
            {
                bool hasRembgNode = await ComfyUIManager.HasNodeType(ComfyUiRembgNodeType);
                if (!hasRembgNode)
                {
                    NotifyError(
                        "ComfyUI Node Missing",
                        $"필수 커스텀 노드 '{ComfyUiRembgNodeType}'를 찾지 못했습니다. AIPreprocess 탭은 ComfyUI 전용이며 fallback을 지원하지 않습니다.");
                    return;
                }

                string uploadFileName = $"{fileName}_{Guid.NewGuid().ToString().Substring(0, 8)}.png";
                byte[] sourceBytes = File.ReadAllBytes(sourcePath);
                
                bool uploaded = await ComfyUIManager.UploadImage(sourceBytes, uploadFileName);
                if (!uploaded)
                {
                    NotifyError("ComfyUI Upload Failed", "ComfyUI 서버에 파일 업로드를 실패했습니다. ComfyUI가 실행 중인지 확인하세요.");
                    return;
                }

                EditorUtility.DisplayProgressBar("ComfyUI RemBG", "Queuing prompt...", 0.3f);
                string workflowJson = File.ReadAllText(workflowPath);
                workflowJson = workflowJson.Replace("%UPLOADED_IMAGE_NAME%", uploadFileName);

                var workflow = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowJson);
                string promptId = await ComfyUIManager.QueuePrompt(workflow);

                if (string.IsNullOrEmpty(promptId))
                {
                    NotifyError("ComfyUI Execution Failed", "프롬프트를 큐에 넣는 데 실패했습니다.");
                    return;
                }

                EditorUtility.DisplayProgressBar("ComfyUI RemBG", "Waiting for RemBG completion...", 0.5f);
                
                HistoryData history = null;
                for (int i = 0; i < 45; i++) // 최대 45초 대기
                {
                    await Task.Delay(1000);
                    history = await ComfyUIManager.GetHistory(promptId);
                    if (history?.outputs != null && history.outputs.Count > 0)
                        break;
                    EditorUtility.DisplayProgressBar("ComfyUI RemBG", $"Processing image... ({i}s)", 0.5f + (i / 45f) * 0.4f);
                }

                if (history?.outputs == null)
                {
                    NotifyError("ComfyUI Timeout", "배경 제거 작업이 시간 초과되었습니다.");
                    return;
                }

                EditorUtility.DisplayProgressBar("ComfyUI RemBG", "Downloading result...", 0.9f);
                bool saved = false;

                foreach (var nodeOutput in history.outputs.Values)
                {
                    if (nodeOutput.images != null && nodeOutput.images.Length > 0)
                    {
                        var img = nodeOutput.images[0];
                        byte[] resultBytes = await ComfyUIManager.GetImage(img.filename, img.subfolder, img.type);

                        if (resultBytes != null)
                        {
                            File.WriteAllBytes(processedPath, resultBytes);
                            saved = true;
                            
                            // Import as single sprite
                            ResetImporterToSingleSprite(processedPath);
                            AssetDatabase.ImportAsset(processedPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                            NotifyInfo("저장 완료", $"위치: {processedPath}");
                            
                            Texture2D newlyCreatedAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(processedPath);
                            Selection.activeObject = newlyCreatedAsset;
                            
                            // Set it back to source texture for immediate slicing convenience
                            _sourceTexture = newlyCreatedAsset;
                        }
                        break;
                    }
                }

                if (!saved)
                {
                    NotifyError("ComfyUI Download Failed", "처리된 이미지를 다운로드하지 못했습니다.");
                }
            }
            catch (Exception ex)
            {
                NotifyError("Error", $"RemBG 프로세스 중 오류 발생: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // 베이스 유닛 대상 바인딩 전용 화면을 그린다.
        private void DrawBaseUnitTab()
        {
            GUILayout.Label("BaseUnit Binding", EditorStyles.boldLabel);
            DrawSourceTextureField();

            EditorGUI.BeginChangeCheck();
            BaseUnitTargetType nextTargetType = (BaseUnitTargetType)EditorGUILayout.EnumPopup("Target Type", _baseUnitContext.targetType);
            if (EditorGUI.EndChangeCheck() && nextTargetType != _baseUnitContext.targetType)
            {
                _baseUnitContext.targetType = nextTargetType;
                _baseUnitContext.targetAsset = null;
            }

            Type expectedTargetType = ResolveBaseUnitTargetAssetType(_baseUnitContext.targetType);
            if (expectedTargetType == null)
            {
                _baseUnitContext.targetAsset = null;
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Target Asset", null, typeof(UnityEngine.Object), false);
                GUI.enabled = true;
                EditorGUILayout.HelpBox(
                    $"Target type not found in loaded assemblies: {GetBaseUnitExpectedTypeName(_baseUnitContext.targetType)}",
                    MessageType.Warning);
            }
            else
            {
                if (_baseUnitContext.targetAsset != null &&
                    !expectedTargetType.IsAssignableFrom(_baseUnitContext.targetAsset.GetType()))
                {
                    _baseUnitContext.targetAsset = null;
                }

                _baseUnitContext.targetAsset = EditorGUILayout.ObjectField(
                    "Target Asset",
                    _baseUnitContext.targetAsset,
                    expectedTargetType,
                    false);
            }

            _baseUnitContext.useManualActionGroup = EditorGUILayout.Toggle("Use Manual Action Group", _baseUnitContext.useManualActionGroup);
            _baseUnitContext.autoApplyAfterProcess = EditorGUILayout.Toggle("Auto Apply After Process", _baseUnitContext.autoApplyAfterProcess);
            if (_baseUnitContext.useManualActionGroup)
            {
                _baseUnitContext.actionGroup =
                    (SpriteActionGroup)EditorGUILayout.EnumPopup("Action Group", _baseUnitContext.actionGroup);
            }
            if (IsSmartSliceActionSplitMode())
            {
                EditorGUILayout.HelpBox("SmartSlice 4행 액션 분리 모드에서는 Action Group 설정이 무시됩니다.", MessageType.Info);
            }
            _useManualActionGroup = _baseUnitContext.useManualActionGroup;
            _singleSourceActionGroup = _baseUnitContext.actionGroup;

            EditorGUILayout.Space(8f);
            DrawPresetSection();
            EditorGUILayout.Space(8f);
            DrawSlicingAndProcessingOptions();
            EditorGUILayout.Space(8f);

            if (!string.IsNullOrEmpty(_lastProcessedAssetPath))
            {
                EditorGUILayout.LabelField("Last Output", _lastProcessedAssetPath, EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4f);
            _animatorClipFps = EditorGUILayout.FloatField("Clip FPS", Mathf.Max(1f, _animatorClipFps));
            if (GUILayout.Button("Run All (Process + Apply + Generate)", GUILayout.Height(28f)))
            {
                if (!TryRunBaseUnitFullPipeline(out string pipelineError))
                {
                    NotifyError("Run All Failed", pipelineError);
                }
                else
                {
                    NotifyInfo("Run All Success", "Process, binding, and animator generation completed.");
                }
            }

            _showBaseUnitAdvancedActions = EditorGUILayout.Foldout(_showBaseUnitAdvancedActions, "Advanced Actions");
            if (_showBaseUnitAdvancedActions)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Process & Slice"))
                {
                    ProcessWithContext(_baseUnitContext.autoApplyAfterProcess);
                }

                GUI.enabled = !string.IsNullOrEmpty(_lastProcessedAssetPath);
                if (GUILayout.Button("Apply Binding"))
                {
                    if (!TryApplyBaseUnitBinding(_lastProcessedAssetPath, out string bindError))
                    {
                        NotifyError("BaseUnit Apply Failed", bindError);
                    }
                    else
                    {
                        NotifyInfo("BaseUnit Apply Success", _lastProcessedAssetPath);
                    }
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Process + Apply"))
                {
                    ProcessWithContext(true);
                }

                if (GUILayout.Button("Generate Animator + Clips"))
                {
                    if (!TryGenerateBaseUnitAnimatorAssets(out string animatorError))
                    {
                        NotifyError("Animator Generate Failed", animatorError);
                    }
                    else
                    {
                        NotifyInfo("Animator Generate Success", "Animator controller and clips generated.");
                    }
                }
            }

            if (GUILayout.Button("Validate Current"))
            {
                if (!TryValidateBaseUnitContext(out string validationError))
                {
                    NotifyError("BaseUnit Validate Failed", validationError);
                }
                else
                {
                    Debug.Log("[AISpriteProcessor] BaseUnit validation passed.");
                }
            }
        }

        // 타워 베이스 대상 바인딩 전용 화면을 그린다.
        private void DrawTowerBaseTab()
        {
            GUILayout.Label("TowerBase Binding", EditorStyles.boldLabel);
            DrawSourceTextureField();

            Type towerConfigType = ResolveTypeByFullName("Kingdom.Game.TowerConfig");
            if (towerConfigType == null)
            {
                _towerContext.targetAsset = null;
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Tower Config", null, typeof(UnityEngine.Object), false);
                GUI.enabled = true;
                EditorGUILayout.HelpBox("TowerConfig type not found in loaded assemblies: Kingdom.Game.TowerConfig", MessageType.Warning);
            }
            else
            {
                if (_towerContext.targetAsset != null &&
                    !towerConfigType.IsAssignableFrom(_towerContext.targetAsset.GetType()))
                {
                    _towerContext.targetAsset = null;
                }

                _towerContext.targetAsset = EditorGUILayout.ObjectField(
                    "Tower Config",
                    _towerContext.targetAsset,
                    towerConfigType,
                    false);
            }

            _towerContext.levelIndex = Mathf.Max(1, EditorGUILayout.IntField("Level (1-based)", _towerContext.levelIndex));
            _towerContext.autoApplyAfterProcess = EditorGUILayout.Toggle("Auto Apply After Process", _towerContext.autoApplyAfterProcess);

            EditorGUILayout.Space(8f);
            DrawPresetSection();
            EditorGUILayout.Space(8f);
            DrawSlicingAndProcessingOptions();
            EditorGUILayout.Space(8f);

            if (!string.IsNullOrEmpty(_lastProcessedAssetPath))
            {
                EditorGUILayout.LabelField("Last Output", _lastProcessedAssetPath, EditorStyles.miniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Process & Slice"))
            {
                ProcessWithContext(_towerContext.autoApplyAfterProcess);
            }

            GUI.enabled = !string.IsNullOrEmpty(_lastProcessedAssetPath);
            if (GUILayout.Button("Apply Binding"))
            {
                if (!TryApplyTowerBinding(_lastProcessedAssetPath, out string bindError))
                {
                    NotifyError("TowerBase Apply Failed", bindError);
                }
                else
                {
                    NotifyInfo("TowerBase Apply Success", _lastProcessedAssetPath);
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Process + Apply"))
            {
                ProcessWithContext(true);
            }

            if (GUILayout.Button("Validate Current"))
            {
                if (!TryValidateTowerContext(out string validationError))
                {
                    NotifyError("TowerBase Validate Failed", validationError);
                }
                else
                {
                    Debug.Log("[AISpriteProcessor] TowerBase validation passed.");
                }
            }
        }

        // 소스 텍스처 선택 영역을 그린다.
        private void DrawSourceTextureField()
        {
            EditorGUI.BeginChangeCheck();
            _sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", _sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
            {
                InvalidatePreviewFrameRectsCache();
                if (_sourceTexture != null && _slicingMode == SlicingMode.AutoDivide)
                {
                    RecalculateAutoGrid();
                }
            }
        }

        // 현재 탭 컨텍스트로 처리/자동 바인딩 흐름을 실행한다.
        private void ProcessWithContext(bool applyAfterProcess)
        {
            if (_activeTab == AISpriteProcessorTab.BaseUnit)
            {
                if (!TryValidateBaseUnitContext(out string baseUnitError))
                {
                    NotifyError("BaseUnit Validation Error", baseUnitError);
                    return;
                }
            }
            else if (_activeTab == AISpriteProcessorTab.TowerBase)
            {
                if (!TryValidateTowerContext(out string towerError))
                {
                    NotifyError("TowerBase Validation Error", towerError);
                    return;
                }
            }

            if (!TryValidateInputs(out string validationError))
            {
                NotifyError("Input Error", validationError);
                return;
            }

            bool useManualGroup = _useManualActionGroup;
            SpriteActionGroup actionGroup = _singleSourceActionGroup;
            if (_activeTab == AISpriteProcessorTab.BaseUnit)
            {
                useManualGroup = _baseUnitContext.useManualActionGroup;
                actionGroup = _baseUnitContext.actionGroup;
            }

            if (IsSmartSliceActionSplitMode())
            {
                useManualGroup = false;
                actionGroup = SpriteActionGroup.Unknown;
            }

            bool processed = ProcessSingleTexture(_sourceTexture, true, useManualGroup, actionGroup);
            if (!processed || !applyAfterProcess)
            {
                return;
            }

            if (!TryApplyBindingForActiveTab(_lastProcessedAssetPath, out string bindError))
            {
                NotifyError("Binding Failed", bindError);
                return;
            }

            NotifyInfo("Binding Success", _lastProcessedAssetPath);
        }

        // 프리셋 저장/로드/삭제 영역을 그린다.
        private void DrawPresetSection()
        {
            GUILayout.Label("프리셋 (Preset)", EditorStyles.boldLabel);

            if (_presetListDirty)
            {
                RefreshPresetList();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("새로고침", GUILayout.Width(80f)))
            {
                _presetDeleteArmed = false;
                _presetDeleteTarget = string.Empty;
                RefreshPresetList();
            }

            string[] options = _presetNames.Count > 0 ? _presetNames.ToArray() : new[] { "(프리셋 없음)" };
            int prevSelectedIndex = _selectedPresetIndex;
            int nextIndex = EditorGUILayout.Popup(Mathf.Max(0, _selectedPresetIndex), options);
            _selectedPresetIndex = _presetNames.Count > 0 ? Mathf.Clamp(nextIndex, 0, _presetNames.Count - 1) : -1;
            if (_selectedPresetIndex != prevSelectedIndex)
            {
                _presetDeleteArmed = false;
                _presetDeleteTarget = string.Empty;
            }
            EditorGUILayout.EndHorizontal();

            _presetNameInput = EditorGUILayout.TextField("Preset Name", _presetNameInput);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("저장"))
            {
                _presetDeleteArmed = false;
                _presetDeleteTarget = string.Empty;
                SaveCurrentAsPreset();
            }

            GUI.enabled = _selectedPresetIndex >= 0 && _selectedPresetIndex < _presetNames.Count;
            if (GUILayout.Button("로드"))
            {
                _presetDeleteArmed = false;
                _presetDeleteTarget = string.Empty;
                LoadSelectedPreset();
            }

            string deleteLabel = _presetDeleteArmed ? "삭제 확인" : "삭제";
            if (GUILayout.Button(deleteLabel))
            {
                if (_selectedPresetIndex < 0 || _selectedPresetIndex >= _presetNames.Count)
                {
                    _presetDeleteArmed = false;
                    _presetDeleteTarget = string.Empty;
                }
                else
                {
                    string currentPreset = _presetNames[_selectedPresetIndex];
                    if (!_presetDeleteArmed || !string.Equals(_presetDeleteTarget, currentPreset, StringComparison.Ordinal))
                    {
                        _presetDeleteArmed = true;
                        _presetDeleteTarget = currentPreset;
                        NotifyWarning("프리셋 삭제", $"한 번 더 누르면 삭제됩니다. preset={currentPreset}");
                    }
                    else
                    {
                        DeleteSelectedPreset();
                        _presetDeleteArmed = false;
                        _presetDeleteTarget = string.Empty;
                    }
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        // 현재 소스 텍스처 기준으로 자동 격자 값을 재계산한다.
        private void RecalculateAutoGrid()
        {
            if (_sourceTexture == null) return;
            _offset = Vector2.zero;
            _spacing = Vector2.zero;
            int safeCols = Mathf.Max(1, _cols);
            int safeRows = Mathf.Max(1, _rows);
            _cellSize = new Vector2(_sourceTexture.width / (float)safeCols, _sourceTexture.height / (float)safeRows);
        }

        // 프리뷰 텍스처와 격자/애니메이션 미리보기를 표시한다.
        private void DrawPreviewPanel()
        {
            GUILayout.Label("미리보기 (Preview)", EditorStyles.boldLabel);
            
            // 줌 컨트롤
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Zoom:", GUILayout.Width(40));
            _previewZoom = EditorGUILayout.Slider(_previewZoom, 0.1f, 5.0f);
            if (GUILayout.Button("Reset", GUILayout.Width(50))) _previewZoom = 1.0f;
            EditorGUILayout.EndHorizontal();

            if (_sourceTexture == null) return;

            Texture2D previewTexture = GetDisplayPreviewTexture();
            if (previewTexture == null)
            {
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // 실제 그리기 영역 확보
            float drawWidth = previewTexture.width * _previewZoom;
            float drawHeight = previewTexture.height * _previewZoom;
            Rect layoutRect = GUILayoutUtility.GetRect(drawWidth, drawHeight);

            // 축소 시 내부 정렬 오프셋 때문에 격자 위치가 어긋나는 문제를 막기 위해
            // 요청한 너비/높이 기준의 고정 이미지 사각형을 직접 계산해 동일 좌표계를 사용한다.
            Rect imageRect = new Rect(layoutRect.x, layoutRect.y, drawWidth, drawHeight);

            // 배경 체크무늬 (투명도 확인용) + 텍스처 표시
            EditorGUI.DrawTextureTransparent(imageRect, previewTexture, ScaleMode.StretchToFill);

            // 격자 그리기
            DrawGrid(imageRect, previewTexture);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            DrawAnimationPreviewPanel();
        }

        // 현재 슬라이스 결과를 프레임 애니메이션으로 재생한다.
        private void DrawAnimationPreviewPanel()
        {
            Texture2D previewTexture = GetDisplayPreviewTexture();
            if (previewTexture == null)
            {
                return;
            }

            List<Rect> frameRects = BuildCurrentFrameRects(previewTexture);
            if (frameRects.Count <= 0)
            {
                EditorGUILayout.HelpBox("애니메이션 프리뷰를 표시할 프레임이 없습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("애니메이션 프리뷰 (현재 Slice 기준)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _previewPlay = EditorGUILayout.Toggle("재생", _previewPlay);
            _previewFps = EditorGUILayout.Slider("FPS", _previewFps, 1f, 30f);
            if (GUILayout.Button("Reset", GUILayout.Width(60f)))
            {
                _previewFrameIndex = 0;
                _previewLastTickTime = EditorApplication.timeSinceStartup;
            }
            EditorGUILayout.EndHorizontal();

            if (_previewFrameIndex >= frameRects.Count)
            {
                _previewFrameIndex = 0;
            }

            if (_previewPlay)
            {
                double now = EditorApplication.timeSinceStartup;
                double frameDuration = 1d / Mathf.Max(1f, _previewFps);
                if (_previewLastTickTime <= 0d)
                {
                    _previewLastTickTime = now;
                }

                if (now - _previewLastTickTime >= frameDuration)
                {
                    int step = Mathf.Max(1, Mathf.FloorToInt((float)((now - _previewLastTickTime) / frameDuration)));
                    _previewFrameIndex = (_previewFrameIndex + step) % frameRects.Count;
                    _previewLastTickTime = now;
                }

                Repaint();
            }
            else
            {
                _previewLastTickTime = EditorApplication.timeSinceStartup;
            }

            Rect frameRect = frameRects[_previewFrameIndex];
            Rect previewRect = GUILayoutUtility.GetRect(220f, 220f, GUILayout.ExpandWidth(false));
            DrawSpriteFramePreview(previewRect, frameRect, previewTexture);
            EditorGUILayout.HelpBox($"Frame: {_previewFrameIndex + 1} / {frameRects.Count}", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        // 단일 프레임을 미리보기 영역에 맞춰 렌더링한다.
        private void DrawSpriteFramePreview(Rect targetRect, Rect frameRect, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return;
            }

            EditorGUI.DrawRect(targetRect, new Color(0f, 0f, 0f, 0.2f));

            float uvX = frameRect.x / texture.width;
            float uvY = frameRect.y / texture.height;
            float uvW = frameRect.width / texture.width;
            float uvH = frameRect.height / texture.height;
            Rect uv = new Rect(uvX, uvY, uvW, uvH);

            float scale = Mathf.Min(targetRect.width / Mathf.Max(1f, frameRect.width), targetRect.height / Mathf.Max(1f, frameRect.height));
            float drawW = frameRect.width * scale;
            float drawH = frameRect.height * scale;
            Rect drawRect = new Rect(
                targetRect.x + (targetRect.width - drawW) * 0.5f,
                targetRect.y + (targetRect.height - drawH) * 0.5f,
                drawW,
                drawH);

            GUI.DrawTextureWithTexCoords(drawRect, texture, uv, true);
        }

        // 현재 설정 기준 프레임 사각형 목록을 생성한다.
        private List<Rect> BuildCurrentFrameRects(Texture2D texture)
        {
            int textureId = texture != null ? texture.GetInstanceID() : 0;
            if (!_previewFrameRectsDirty && _previewFrameRectsTextureId == textureId && _previewFrameRectsCache != null)
            {
                return _previewFrameRectsCache;
            }

            var frames = new List<Rect>();
            if (texture == null)
            {
                _previewFrameRectsCache = frames;
                _previewFrameRectsTextureId = textureId;
                _previewFrameRectsDirty = false;
                return frames;
            }

            int width = texture.width;
            int height = texture.height;

            float cellW;
            float cellH;
            float offX;
            float offY;
            float spcX;
            float spcY;

            if (_slicingMode == SlicingMode.AutoDivide)
            {
                int safeCols = Mathf.Max(1, _cols);
                int safeRows = Mathf.Max(1, _rows);
                cellW = width / (float)safeCols;
                cellH = height / (float)safeRows;
                offX = 0f;
                offY = 0f;
                spcX = 0f;
                spcY = 0f;
            }
            else if (_slicingMode == SlicingMode.SmartSlice)
            {
                frames = DetectIslands(texture, _smartSliceAlphaThreshold, _smartSliceMinPixels, _smartSliceOuterPadding);
                _previewFrameRectsCache = frames;
                _previewFrameRectsTextureId = textureId;
                _previewFrameRectsDirty = false;
                return frames;
            }
            else
            {
                cellW = _cellSize.x;
                cellH = _cellSize.y;
                offX = _offset.x;
                offY = _offset.y;
                spcX = _spacing.x;
                spcY = _spacing.y;
            }

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    float guiY = offY + (r * (cellH + spcY));
                    float textureBottomY = height - (guiY + cellH);

                    float x = offX + (c * (cellW + spcX)) + _innerPadding;
                    float y = textureBottomY + _innerPadding;
                    float w = cellW - (_innerPadding * 2f);
                    float h = cellH - (_innerPadding * 2f);

                    if (w <= 0f || h <= 0f)
                    {
                        continue;
                    }

                    Rect rect = new Rect(x, y, w, h);
                    Rect clamped = ClampRectToTexture(rect, width, height);
                    if (clamped.width > 0f && clamped.height > 0f)
                    {
                        frames.Add(clamped);
                    }
                }
            }

            _previewFrameRectsCache = frames;
            _previewFrameRectsTextureId = textureId;
            _previewFrameRectsDirty = false;
            return frames;
        }

        // 프레임 사각형을 텍스처 경계 내로 보정한다.
        private static Rect ClampRectToTexture(Rect rect, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return Rect.zero;
            }

            float xMinFloat = Mathf.Clamp(rect.xMin, 0f, width);
            float yMinFloat = Mathf.Clamp(rect.yMin, 0f, height);
            float xMaxFloat = Mathf.Clamp(rect.xMax, 0f, width);
            float yMaxFloat = Mathf.Clamp(rect.yMax, 0f, height);

            int xMin = Mathf.Clamp(Mathf.FloorToInt(xMinFloat + 0.0001f), 0, width - 1);
            int yMin = Mathf.Clamp(Mathf.FloorToInt(yMinFloat + 0.0001f), 0, height - 1);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(xMaxFloat - 0.0001f), xMin + 1, width);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(yMaxFloat - 0.0001f), yMin + 1, height);

            if (xMax <= xMin || yMax <= yMin)
            {
                return Rect.zero;
            }

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        // 현재 슬라이싱 기준의 격자 오버레이를 그린다.
        private void DrawGrid(Rect rect, Texture2D texture)
        {
            if (Event.current.type != EventType.Repaint || texture == null) return;

            // 계산 로직 (자동/커스텀)
            float cellW, cellH;
            float offX, offY;
            float spcX, spcY;

            if (_slicingMode == SlicingMode.AutoDivide)
            {
                int safeCols = Mathf.Max(1, _cols);
                int safeRows = Mathf.Max(1, _rows);
                cellW = (texture.width / (float)safeCols);
                cellH = (texture.height / (float)safeRows);
                offX = 0; offY = 0;
                spcX = 0; spcY = 0;
            }
            else if (_slicingMode == SlicingMode.SmartSlice)
            {
                List<Rect> smartRects = BuildCurrentFrameRects(texture);
                float smartScale = _previewZoom;
                for (int i = 0; i < smartRects.Count; i++)
                {
                    Rect rt = smartRects[i];
                    float x = rect.x + (rt.x * smartScale);
                    float y = rect.y + ((texture.height - rt.yMax) * smartScale);
                    float w = rt.width * smartScale;
                    float h = rt.height * smartScale;
                    Rect draw = new Rect(x, y, w, h);
                    Handles.color = new Color(1f, 0.8f, 0f, 0.9f);
                    Handles.DrawWireCube(draw.center, draw.size);
                }
                Handles.color = Color.white;
                return;
            }
            else
            {
                cellW = _cellSize.x;
                cellH = _cellSize.y;
                offX = _offset.x;
                offY = _offset.y;
                spcX = _spacing.x;
                spcY = _spacing.y;
            }

            // 프리뷰 배율 적용
            float scale = _previewZoom;
            float drawCellW = cellW * scale;
            float drawCellH = cellH * scale;
            float drawOffX = offX * scale;
            float drawOffY = offY * scale;
            float drawSpcX = spcX * scale;
            float drawSpcY = spcY * scale;
            float drawPad = _innerPadding * scale;

            // 그리기 (좌상단 기준. 텍스처 좌표계는 좌하단이지만 에디터 화면은 좌상단)
            // 주의: 커스텀 격자의 세로 오프셋은 보통 "상단에서 얼마나 떨어졌는지"로 이해하는 것이 직관적이다.
            // 하지만 스프라이트 시트(텍스처)는 하단 기준. 
            // 여기서는 화면(상단) 기준으로 그리며, 처리 시 좌표 변환이 필요하다.
            
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    float x = rect.x + drawOffX + (c * (drawCellW + drawSpcX));
                    float y = rect.y + drawOffY + (r * (drawCellH + drawSpcY)); // GUI 좌표계 (아래로 증가)

                    Rect cellRect = new Rect(x, y, drawCellW, drawCellH);
                    
                    // 1. 녹색 선 (격자 셀)
                    Handles.color = new Color(0, 1, 0, 0.5f);
                    Handles.DrawWireCube(cellRect.center, cellRect.size);

                    // 2. 붉은 선 (내부 패딩 적용 후 실제 스프라이트 영역)
                    if (_innerPadding > 0)
                    {
                        Rect contentRect = new Rect(x + drawPad, y + drawPad, 
                                                  drawCellW - (drawPad * 2), 
                                                  drawCellH - (drawPad * 2));
                        
                        Handles.color = new Color(1, 0, 0, 0.8f);
                        Handles.DrawWireCube(contentRect.center, contentRect.size);
                    }
                }
            }
            
            Handles.color = Color.white;
        }

        // 입력 검증 후 스프라이트 시트 처리를 실행한다.
        private void ProcessSpriteSheet()
        {
            if (!TryValidateInputs(out string validationError))
            {
                NotifyError("입력값 오류", validationError);
                return;
            }

            ProcessSingleTexture(_sourceTexture, true, _useManualActionGroup, _singleSourceActionGroup);
        }

        // 단일 텍스처 전처리/슬라이스/임포트/메타 저장을 수행한다.
        private bool ProcessSingleTexture(Texture2D sourceTexture, bool selectOutput = true, bool hasManualGroup = false, SpriteActionGroup manualGroup = SpriteActionGroup.Unknown)
        {
            if (sourceTexture == null)
            {
                return false;
            }

            string sourcePath = AssetDatabase.GetAssetPath(sourceTexture);
            if (string.IsNullOrEmpty(sourcePath))
            {
                return false;
            }

            string dir = Path.GetDirectoryName(sourcePath);
            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            bool ignoreActionGroup = IsSmartSliceActionSplitMode();
            SpriteActionGroup group = ignoreActionGroup ? SpriteActionGroup.Unknown : (hasManualGroup ? manualGroup : DetectActionGroupFromFileName(fileName));
            string actionGroup = ignoreActionGroup ? "multi" : ToActionGroupName(group);
            string processedPath = Path.Combine(dir, $"{actionGroup}_{fileName}_Processed.png");
            List<string> warnings = new List<string>();

            Texture2D readableTexture = CreateReadableTexture(sourceTexture);
            if (readableTexture == null)
            {
                return false;
            }

            if (_removeBackground)
            {
                RemoveBackground(readableTexture, _removeColor, _tolerance);
            }

            int outputWidth = readableTexture.width;
            int outputHeight = readableTexture.height;
            float applyOffX = _offset.x;
            float applyOffY = _offset.y;

            Texture2D outputTexture = readableTexture;
            bool outputTextureOwned = false;

            if (_cropToSelection && _slicingMode != SlicingMode.SmartSlice &&
                TryGetCropRect(readableTexture.width, readableTexture.height, out RectInt cropRect, out float cropTop))
            {
                Texture2D cropped = CropTexture(readableTexture, cropRect);
                if (cropped != null)
                {
                    outputTexture = cropped;
                    outputTextureOwned = true;
                    outputWidth = cropped.width;
                    outputHeight = cropped.height;
                    applyOffX = _offset.x - cropRect.x;
                    applyOffY = _offset.y - cropTop;
                }
            }

            List<SpriteMetaData> metas = BuildSpriteMetaData(processedPath, outputTexture, outputWidth, outputHeight, applyOffX, applyOffY, warnings);
            int maxFrameWidth = 0;
            int maxFrameHeight = 0;
            bool normalizedFrames = false;

            if (_normalizeFrames && metas.Count > 0 &&
                TryNormalizeFrames(outputTexture, metas, out Texture2D normalizedTexture, out List<SpriteMetaData> normalizedMetas, out maxFrameWidth, out maxFrameHeight, out normalizedFrames))
            {
                if (outputTextureOwned && outputTexture != null && outputTexture != readableTexture)
                {
                    DestroyImmediate(outputTexture);
                }

                outputTexture = normalizedTexture;
                outputTextureOwned = true;
                metas = normalizedMetas;
                outputWidth = outputTexture.width;
                outputHeight = outputTexture.height;
            }

            if (maxFrameWidth <= 0 || maxFrameHeight <= 0)
            {
                CalculateMaxFrameSize(metas, out maxFrameWidth, out maxFrameHeight);
            }

            if (metas.Count <= 0)
            {
                warnings.Add("No valid frame rects were detected from current slicing settings.");
            }

            string processedAssetPath = processedPath.Replace('\\', '/');
            _lastProcessedAssetPath = processedAssetPath;
            byte[] bytes = outputTexture.EncodeToPNG();
            File.WriteAllBytes(processedPath, bytes);

            // 이전 임포트의 오래된 스프라이트시트 메타데이터가 먼저 적용되는 문제를 방지한다.
            ResetImporterToSingleSprite(processedAssetPath);
            AssetDatabase.ImportAsset(processedAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            ApplySlicing(processedAssetPath, metas);

            if (_emitManifest)
            {
                WriteManifest(sourcePath, processedPath, actionGroup, fileName, metas.Count, maxFrameWidth, maxFrameHeight, normalizedFrames, warnings);

                string manifestAssetPath = Path.Combine(Path.GetDirectoryName(processedAssetPath) ?? string.Empty, ManifestFileName).Replace('\\', '/');
                AssetDatabase.ImportAsset(manifestAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }

            DestroyImmediate(readableTexture);
            if (outputTextureOwned && outputTexture != null && outputTexture != readableTexture)
            {
                DestroyImmediate(outputTexture);
            }

            if (selectOutput)
            {
                NotifyInfo("완료 (Success)", $"처리가 완료되었습니다. path={processedPath}");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(processedAssetPath);
            }

            return true;
        }

        // 활성 탭 유형에 맞는 바인딩 적용을 시도한다.
        private bool TryApplyBindingForActiveTab(string processedAssetPath, out string error)
        {
            switch (_activeTab)
            {
                case AISpriteProcessorTab.BaseUnit:
                    return TryApplyBaseUnitBinding(processedAssetPath, out error);
                case AISpriteProcessorTab.TowerBase:
                    return TryApplyTowerBinding(processedAssetPath, out error);
                default:
                    error = "Binding is supported only on BaseUnit/TowerBase tabs.";
                    return false;
            }
        }

        // 베이스 유닛 바인딩 입력값 유효성을 검증한다.
        private bool TryValidateBaseUnitContext(out string error)
        {
            if (_sourceTexture == null)
            {
                error = "Source Texture is required.";
                return false;
            }

            if (_baseUnitContext.targetAsset == null)
            {
                error = "Target Asset is required.";
                return false;
            }

            bool ignoreActionGroup = IsSmartSliceActionSplitMode();
            if (!ignoreActionGroup && _baseUnitContext.useManualActionGroup && _baseUnitContext.actionGroup == SpriteActionGroup.Unknown)
            {
                _baseUnitContext.actionGroup = SpriteActionGroup.Idle;
                _singleSourceActionGroup = _baseUnitContext.actionGroup;
                _useManualActionGroup = _baseUnitContext.useManualActionGroup;
                NotifyWarning("BaseUnit Action Group Auto-Fix", "Manual Action Group was Unknown. It has been auto-corrected to Idle.");
            }

            if (!ignoreActionGroup && !_baseUnitContext.useManualActionGroup)
            {
                SpriteActionGroup detected = DetectActionGroupFromFileName(_sourceTexture.name);
                if (detected == SpriteActionGroup.Unknown)
                {
                    error = "Could not detect Action Group from file name. Enable manual action group.";
                    return false;
                }
            }

            switch (_baseUnitContext.targetType)
            {
                case BaseUnitTargetType.HeroConfig:
                    if (!IsAssetTypeName(_baseUnitContext.targetAsset, "HeroConfig"))
                    {
                        error = "Target Asset must be HeroConfig.";
                        return false;
                    }

                    if (!TryReadTrimmedStringProperty(_baseUnitContext.targetAsset, "HeroId", out string heroId) ||
                        string.IsNullOrWhiteSpace(heroId))
                    {
                        error = "HeroConfig.HeroId is required.";
                        return false;
                    }
                    break;

                case BaseUnitTargetType.EnemyConfig:
                    if (!IsAssetTypeName(_baseUnitContext.targetAsset, "EnemyConfig"))
                    {
                        error = "Target Asset must be EnemyConfig.";
                        return false;
                    }

                    if (!TryReadTrimmedStringProperty(_baseUnitContext.targetAsset, "EnemyId", out string enemyId) ||
                        string.IsNullOrWhiteSpace(enemyId))
                    {
                        error = "EnemyConfig.EnemyId is required.";
                        return false;
                    }
                    break;

                case BaseUnitTargetType.BarracksSoldierConfig:
                    if (!IsAssetTypeName(_baseUnitContext.targetAsset, "BarracksSoldierConfig"))
                    {
                        error = "Target Asset must be BarracksSoldierConfig.";
                        return false;
                    }
                    break;
            }

            error = null;
            return true;
        }

        // 타워 베이스 바인딩 입력값 유효성을 검증한다.
        private bool TryValidateTowerContext(out string error)
        {
            if (_sourceTexture == null)
            {
                error = "Source Texture is required.";
                return false;
            }

            if (_towerContext.targetAsset == null)
            {
                error = "Tower Config is required.";
                return false;
            }

            if (!IsAssetTypeName(_towerContext.targetAsset, "TowerConfig"))
            {
                error = "Target Asset must be TowerConfig.";
                return false;
            }

            if (_towerContext.levelIndex <= 0)
            {
                error = "Level index must be 1 or higher.";
                return false;
            }

            SerializedObject so = new SerializedObject(_towerContext.targetAsset);
            SerializedProperty levels = so.FindProperty("Levels");
            if (levels == null || !levels.isArray)
            {
                error = "TowerConfig.Levels array was not found.";
                return false;
            }

            error = null;
            return true;
        }

        // 베이스 유닛 대상 에셋에 처리 결과 경로를 적용한다.
        private bool TryApplyBaseUnitBinding(string processedAssetPath, out string error)
        {
            if (!TryValidateBaseUnitContext(out error))
            {
                return false;
            }

            if (!TryConvertAssetPathToResourcePath(processedAssetPath, out string resourcePath, out error))
            {
                return false;
            }

            switch (_baseUnitContext.targetType)
            {
                case BaseUnitTargetType.HeroConfig:
                {
                    string normalized = processedAssetPath.Replace('\\', '/');
                    if (normalized.IndexOf("/Resources/Sprites/Heroes/", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        error = "Hero generated sprites must be under Assets/Resources/Sprites/Heroes for manifest fallback loading.";
                        return false;
                    }

                    Debug.Log($"[AISpriteProcessor] HeroConfig binding validated (manifest-driven). resourcePath={resourcePath}");
                    error = null;
                    return true;
                }

                case BaseUnitTargetType.EnemyConfig:
                    // Enemy runtime uses animator binding; sprite path is no longer persisted on EnemyConfig.
                    error = null;
                    return true;

                case BaseUnitTargetType.BarracksSoldierConfig:
                    return TryAssignStringProperty(_baseUnitContext.targetAsset, "RuntimeSpriteResourcePath", resourcePath, out error);

                default:
                    error = "Unsupported BaseUnit target type.";
                    return false;
            }
        }

        // 타워 설정의 레벨 데이터에 처리 결과 경로를 적용한다.
        private bool TryApplyTowerBinding(string processedAssetPath, out string error)
        {
            if (!TryValidateTowerContext(out error))
            {
                return false;
            }

            if (!TryConvertAssetPathToResourcePath(processedAssetPath, out string resourcePath, out error))
            {
                return false;
            }

            SerializedObject so = new SerializedObject(_towerContext.targetAsset);
            SerializedProperty levels = so.FindProperty("Levels");
            if (levels == null || !levels.isArray)
            {
                error = "TowerConfig.Levels array was not found.";
                return false;
            }

            int targetIndex = Mathf.Max(0, _towerContext.levelIndex - 1);
            if (levels.arraySize <= targetIndex)
            {
                levels.arraySize = targetIndex + 1;
            }

            SerializedProperty levelData = levels.GetArrayElementAtIndex(targetIndex);
            SerializedProperty spritePathProp = levelData.FindPropertyRelative("SpriteResourcePath");
            if (spritePathProp == null || spritePathProp.propertyType != SerializedPropertyType.String)
            {
                error = "TowerLevelData.SpriteResourcePath field was not found.";
                return false;
            }

            spritePathProp.stringValue = resourcePath;

            SerializedProperty runtimeTemplate = so.FindProperty("RuntimeSpriteResourcePath");
            if (runtimeTemplate != null &&
                runtimeTemplate.propertyType == SerializedPropertyType.String &&
                string.IsNullOrWhiteSpace(runtimeTemplate.stringValue))
            {
                string towerId = "Tower";
                if (TryReadTrimmedStringProperty(_towerContext.targetAsset, "TowerId", out string parsedTowerId) &&
                    !string.IsNullOrWhiteSpace(parsedTowerId))
                {
                    towerId = parsedTowerId;
                }

                runtimeTemplate.stringValue = $"Sprites/Towers/{towerId}/L{{level}}";
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_towerContext.targetAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            error = null;
            return true;
        }

        // 대상 에셋의 문자열 프로퍼티 값을 안전하게 갱신한다.
        private static bool TryAssignStringProperty(UnityEngine.Object targetAsset, string propertyName, string value, out string error)
        {
            if (targetAsset == null)
            {
                error = "Target asset is null.";
                return false;
            }

            SerializedObject so = new SerializedObject(targetAsset);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.String)
            {
                error = $"String field '{propertyName}' was not found on {targetAsset.GetType().Name}.";
                return false;
            }

            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            error = null;
            return true;
        }

        // 대상 에셋의 문자열 프로퍼티를 공백 제거 후 읽는다.
        private static bool TryReadTrimmedStringProperty(UnityEngine.Object targetAsset, string propertyName, out string value)
        {
            value = string.Empty;
            if (targetAsset == null)
            {
                return false;
            }

            SerializedObject so = new SerializedObject(targetAsset);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.String)
            {
                return false;
            }

            value = (prop.stringValue ?? string.Empty).Trim();
            return true;
        }

        // 대상 에셋 타입명이 기대값과 일치하는지 확인한다.
        private static bool IsAssetTypeName(UnityEngine.Object targetAsset, string expectedTypeName)
        {
            return targetAsset != null &&
                   string.Equals(targetAsset.GetType().Name, expectedTypeName, StringComparison.Ordinal);
        }

        // 베이스 유닛 대상 타입 열거형 값을 실제 형식으로 해석한다.
        private static Type ResolveBaseUnitTargetAssetType(BaseUnitTargetType targetType)
        {
            return ResolveTypeByFullName(GetBaseUnitExpectedTypeName(targetType));
        }

        // 베이스 유닛 대상 타입별 기대 형식 풀네임을 반환한다.
        private static string GetBaseUnitExpectedTypeName(BaseUnitTargetType targetType)
        {
            switch (targetType)
            {
                case BaseUnitTargetType.HeroConfig:
                    return "Kingdom.Game.HeroConfig";
                case BaseUnitTargetType.EnemyConfig:
                    return "Kingdom.Game.EnemyConfig";
                case BaseUnitTargetType.BarracksSoldierConfig:
                    return "Kingdom.Game.BarracksSoldierConfig";
                default:
                    return string.Empty;
            }
        }

        // 로드된 어셈블리에서 풀네임으로 형식을 조회한다.
        private static Type ResolveTypeByFullName(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            for (int i = 0; i < AppDomain.CurrentDomain.GetAssemblies().Length; i++)
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()[i];
                Type type = assembly.GetType(fullTypeName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        // 에셋 경로를 리소스 로드용 경로로 변환한다.
        private static bool TryConvertAssetPathToResourcePath(string assetPath, out string resourcePath, out string error)
        {
            resourcePath = string.Empty;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "No processed output asset path is available. Run Process first.";
                return false;
            }

            string normalized = assetPath.Replace('\\', '/');
            const string marker = "/Resources/";
            int markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                error = $"Processed asset is outside Resources folder: {normalized}";
                return false;
            }

            string relative = normalized.Substring(markerIndex + marker.Length);
            resourcePath = Path.ChangeExtension(relative, null)?.Replace('\\', '/') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                error = $"Could not build resource path from: {normalized}";
                return false;
            }

            error = null;
            return true;
        }

        // 파일명 키워드로 액션 그룹을 추론한다.
        private static SpriteActionGroup DetectActionGroupFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return SpriteActionGroup.Unknown;
            }

            string n = fileName.ToLowerInvariant();
            if (n.Contains("idle")) return SpriteActionGroup.Idle;
            if (n.Contains("walk") || n.Contains("run")) return SpriteActionGroup.Walk;
            if (n.Contains("attack") || n.Contains("atk")) return SpriteActionGroup.Attack;
            if (n.Contains("die") || n.Contains("death") || n.Contains("dead")) return SpriteActionGroup.Die;
            return SpriteActionGroup.Unknown;
        }

        // 액션 그룹 열거형 값을 소문자 문자열로 변환한다.
        private static string ToActionGroupName(SpriteActionGroup group)
        {
            switch (group)
            {
                case SpriteActionGroup.Idle:
                    return "idle";
                case SpriteActionGroup.Walk:
                    return "walk";
                case SpriteActionGroup.Attack:
                    return "attack";
                case SpriteActionGroup.Die:
                    return "die";
                default:
                    return "unknown";
            }
        }

        private bool IsSmartSliceActionSplitMode()
        {
            return _slicingMode == SlicingMode.SmartSlice && _smartSliceSplitActionsByRows && _smartSliceActionRowCount >= 2;
        }

        // BaseUnit에서 처리/바인딩/애니메이터 생성을 한 번에 실행한다.
        private bool TryRunBaseUnitFullPipeline(out string error)
        {
            if (!TryValidateBaseUnitContext(out error))
            {
                return false;
            }

            if (!TryValidateInputs(out error))
            {
                return false;
            }

            bool useManualGroup = _baseUnitContext.useManualActionGroup;
            SpriteActionGroup actionGroup = _baseUnitContext.actionGroup;
            if (IsSmartSliceActionSplitMode())
            {
                useManualGroup = false;
                actionGroup = SpriteActionGroup.Unknown;
            }

            bool processed = ProcessSingleTexture(_sourceTexture, true, useManualGroup, actionGroup);
            if (!processed)
            {
                error = "Process failed. Check previous log for details.";
                return false;
            }

            if (!TryApplyBaseUnitBinding(_lastProcessedAssetPath, out error))
            {
                return false;
            }

            if (!TryGenerateBaseUnitAnimatorAssets(out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        // 베이스 유닛 대상의 액션 스프라이트에서 AnimationClip/AnimatorController를 생성한다.
        private bool TryGenerateBaseUnitAnimatorAssets(out string error)
        {
            if (_baseUnitContext.targetAsset == null)
            {
                error = "Target Asset is required.";
                return false;
            }

            if (!TryResolveAnimatorTargetId(_baseUnitContext.targetAsset, _baseUnitContext.targetType, out string targetId, out error))
            {
                return false;
            }

            if (!TryResolveAnimatorSourceSpriteAssetPath(
                    _baseUnitContext.targetAsset,
                    _baseUnitContext.targetType,
                    targetId,
                    out string spriteAssetPath,
                    out error))
            {
                return false;
            }

            Sprite[] sprites = LoadSpritesFromAssetPath(spriteAssetPath);
            if (sprites == null || sprites.Length <= 0)
            {
                error = $"No sprites were found at: {spriteAssetPath}";
                return false;
            }

            var idleFrames = FilterSpritesByActionAliases(sprites, "idle");
            var walkFrames = FilterSpritesByActionAliases(sprites, "walk", "move", "run");
            var attackFrames = FilterSpritesByActionAliases(sprites, "attack", "atk");
            var dieFrames = FilterSpritesByActionAliases(sprites, "die", "death", "dead");

            // Grid slice fallback:
            // If sprite names are row/column-based (..._r_c), infer 4 actions by row order.
            if (TryInferFourActionRows(
                    sprites,
                    out List<Sprite> inferredIdle,
                    out List<Sprite> inferredWalk,
                    out List<Sprite> inferredAttack,
                    out List<Sprite> inferredDie))
            {
                if (walkFrames.Count <= 0 || attackFrames.Count <= 0 || dieFrames.Count <= 0)
                {
                    idleFrames = inferredIdle;
                    walkFrames = inferredWalk;
                    attackFrames = inferredAttack;
                    dieFrames = inferredDie;
                    Debug.Log($"[AISpriteProcessor] Action inference fallback applied by row index. target={targetId}");
                }
            }

            if (idleFrames.Count <= 0 && walkFrames.Count <= 0 && attackFrames.Count <= 0 && dieFrames.Count <= 0)
            {
                error = $"Could not detect action groups from sprite names at: {spriteAssetPath}";
                return false;
            }

            if (!TryEnsureAnimatorOutputFolder(_baseUnitContext.targetType, targetId, out string outputFolder, out error))
            {
                return false;
            }

            AnimationClip idleClip = CreateOrUpdateActionClip(outputFolder, targetId, "idle", idleFrames, true, _animatorClipFps);
            AnimationClip walkClip = CreateOrUpdateActionClip(outputFolder, targetId, "walk", walkFrames, true, _animatorClipFps);
            AnimationClip attackClip = CreateOrUpdateActionClip(outputFolder, targetId, "attack", attackFrames, false, _animatorClipFps);
            AnimationClip dieClip = CreateOrUpdateActionClip(outputFolder, targetId, "die", dieFrames, false, _animatorClipFps);

            string controllerPath = $"{outputFolder}/{targetId}.controller";
            AnimatorController controller = CreateOrUpdateAnimatorController(controllerPath, idleClip, walkClip, attackClip, dieClip);
            if (controller == null)
            {
                error = $"Failed to create animator controller at: {controllerPath}";
                return false;
            }

            if (_baseUnitContext.targetType == BaseUnitTargetType.EnemyConfig &&
                TryConvertAssetPathToResourcePath(controllerPath, out string controllerResourcePath, out _))
            {
                if (!TryAssignStringProperty(_baseUnitContext.targetAsset, "RuntimeAnimatorControllerPath", controllerResourcePath, out string assignError))
                {
                    NotifyWarning("Animator Path Bind Warning", assignError);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[AISpriteProcessor] Animator generated. target={targetId}, source={spriteAssetPath}, controller={controllerPath}, " +
                $"idle={idleFrames.Count}, walk={walkFrames.Count}, attack={attackFrames.Count}, die={dieFrames.Count}");

            error = null;
            return true;
        }

        // 대상 타입에 맞는 식별자(HeroId/EnemyId/SoldierId)를 가져온다.
        private static bool TryResolveAnimatorTargetId(UnityEngine.Object targetAsset, BaseUnitTargetType targetType, out string targetId, out string error)
        {
            targetId = string.Empty;
            string propertyName = "EnemyId";
            if (targetType == BaseUnitTargetType.HeroConfig)
            {
                propertyName = "HeroId";
            }
            else if (targetType == BaseUnitTargetType.BarracksSoldierConfig)
            {
                propertyName = "SoldierId";
            }

            if (!TryReadTrimmedStringProperty(targetAsset, propertyName, out targetId) || string.IsNullOrWhiteSpace(targetId))
            {
                error = $"{targetAsset.GetType().Name}.{propertyName} is required.";
                return false;
            }

            error = null;
            return true;
        }

        // Animator 생성용 소스 스프라이트 에셋 경로를 해석한다.
        private bool TryResolveAnimatorSourceSpriteAssetPath(
            UnityEngine.Object targetAsset,
            BaseUnitTargetType targetType,
            string targetId,
            out string spriteAssetPath,
            out string error)
        {
            spriteAssetPath = string.Empty;

            if (!string.IsNullOrWhiteSpace(_lastProcessedAssetPath) && File.Exists(_lastProcessedAssetPath))
            {
                spriteAssetPath = _lastProcessedAssetPath.Replace('\\', '/');
                error = null;
                return true;
            }

            if (targetType == BaseUnitTargetType.BarracksSoldierConfig &&
                TryReadTrimmedStringProperty(targetAsset, "RuntimeSpriteResourcePath", out string runtimePath) &&
                !string.IsNullOrWhiteSpace(runtimePath))
            {
                if (TryResolveResourcePathToSpriteAssetPath(runtimePath, out spriteAssetPath))
                {
                    error = null;
                    return true;
                }
            }

            if (_sourceTexture != null)
            {
                string sourceTexturePath = AssetDatabase.GetAssetPath(_sourceTexture);
                if (!string.IsNullOrWhiteSpace(sourceTexturePath) && File.Exists(sourceTexturePath))
                {
                    Sprite[] sourceSprites = LoadSpritesFromAssetPath(sourceTexturePath);
                    if (sourceSprites.Length > 0)
                    {
                        spriteAssetPath = sourceTexturePath.Replace('\\', '/');
                        error = null;
                        return true;
                    }
                }
            }

            if (TryFindSpriteAssetPathByTargetId(targetType, targetId, out string discoveredPath))
            {
                spriteAssetPath = discoveredPath;
                error = null;
                return true;
            }

            error = "No source sprite path found. Run Process first or assign Source Texture.";
            return false;
        }

        // 대상 ID 기반으로 Resources/Sprites 하위의 후보 텍스처를 찾아 첫 유효 스프라이트 경로를 반환한다.
        private static bool TryFindSpriteAssetPathByTargetId(BaseUnitTargetType targetType, string targetId, out string spriteAssetPath)
        {
            spriteAssetPath = string.Empty;
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            string searchRoot = "Assets/Resources/Sprites";
            if (targetType == BaseUnitTargetType.EnemyConfig)
            {
                searchRoot = "Assets/Resources/Sprites/Enemies";
            }
            else if (targetType == BaseUnitTargetType.HeroConfig)
            {
                searchRoot = "Assets/Resources/Sprites/Heroes";
            }
            else if (targetType == BaseUnitTargetType.BarracksSoldierConfig)
            {
                searchRoot = "Assets/Resources/Sprites/Barracks";
            }

            if (!AssetDatabase.IsValidFolder(searchRoot))
            {
                return false;
            }

            string[] guids = AssetDatabase.FindAssets($"t:Texture2D {targetId}", new[] { searchRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                Sprite[] sprites = LoadSpritesFromAssetPath(path);
                if (sprites == null || sprites.Length <= 0)
                {
                    continue;
                }

                spriteAssetPath = path.Replace('\\', '/');
                return true;
            }

            return false;
        }

        // Resources 경로를 실제 에셋 경로로 변환한다.
        private static bool TryResolveResourcePathToSpriteAssetPath(string runtimePath, out string assetPath)
        {
            assetPath = string.Empty;
            if (string.IsNullOrWhiteSpace(runtimePath))
            {
                return false;
            }

            string normalized = runtimePath.Trim().Replace('\\', '/').TrimStart('/');
            string[] extensions = { ".png", ".jpg", ".jpeg" };
            for (int i = 0; i < extensions.Length; i++)
            {
                string candidate = $"Assets/Resources/{normalized}{extensions[i]}";
                if (!File.Exists(candidate))
                {
                    continue;
                }

                assetPath = candidate;
                return true;
            }

            return false;
        }

        // 에셋 경로의 스프라이트 서브에셋 목록을 로드한다.
        private static Sprite[] LoadSpritesFromAssetPath(string spriteAssetPath)
        {
            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spriteAssetPath);
            if (allAssets == null || allAssets.Length <= 0)
            {
                return Array.Empty<Sprite>();
            }

            var sprites = new List<Sprite>(allAssets.Length);
            for (int i = 0; i < allAssets.Length; i++)
            {
                if (allAssets[i] is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites.ToArray();
        }

        // 액션 alias 토큰으로 스프라이트를 필터링한다.
        private static List<Sprite> FilterSpritesByActionAliases(Sprite[] sprites, params string[] aliases)
        {
            var result = new List<Sprite>();
            if (sprites == null || aliases == null || aliases.Length <= 0)
            {
                return result;
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite == null || !SpriteNameContainsAnyAliasToken(sprite.name, aliases))
                {
                    continue;
                }

                result.Add(sprite);
            }

            result.Sort(CompareSpriteByActionFrame);
            return result;
        }

        // 스프라이트 이름 토큰에 alias가 존재하는지 확인한다.
        private static bool SpriteNameContainsAnyAliasToken(string name, string[] aliases)
        {
            if (string.IsNullOrWhiteSpace(name) || aliases == null || aliases.Length <= 0)
            {
                return false;
            }

            string[] tokens = name.Split('_');
            for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
            {
                string token = tokens[tokenIndex];
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                for (int aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
                {
                    if (string.Equals(token, aliases[aliasIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 액션 프레임 번호 기준으로 스프라이트 정렬 비교를 수행한다.
        private static int CompareSpriteByActionFrame(Sprite a, Sprite b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int aIndex = ParseTrailingFrameIndex(a.name);
            int bIndex = ParseTrailingFrameIndex(b.name);
            if (aIndex != bIndex)
            {
                return aIndex.CompareTo(bIndex);
            }

            return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        }

        // 이름 끝 숫자를 프레임 인덱스로 파싱한다.
        private static int ParseTrailingFrameIndex(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return int.MaxValue;
            }

            string[] tokens = name.Split('_');
            if (tokens.Length <= 0)
            {
                return int.MaxValue;
            }

            return int.TryParse(tokens[tokens.Length - 1], out int parsed) ? parsed : int.MaxValue;
        }

        private static bool TryInferFourActionRows(
            Sprite[] sprites,
            out List<Sprite> idleFrames,
            out List<Sprite> walkFrames,
            out List<Sprite> attackFrames,
            out List<Sprite> dieFrames)
        {
            idleFrames = new List<Sprite>();
            walkFrames = new List<Sprite>();
            attackFrames = new List<Sprite>();
            dieFrames = new List<Sprite>();

            if (sprites.IsNull() || sprites.Length <= 0)
            {
                return false;
            }

            var rows = new SortedDictionary<int, List<(int col, Sprite sprite)>>();
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite.IsNull() || !TryParseRowColFromSpriteName(sprite.name, out int row, out int col))
                {
                    continue;
                }

                if (!rows.TryGetValue(row, out List<(int col, Sprite sprite)> rowList))
                {
                    rowList = new List<(int col, Sprite sprite)>();
                    rows[row] = rowList;
                }

                rowList.Add((col, sprite));
            }

            if (rows.Count < 4)
            {
                return false;
            }

            var orderedRows = new List<int>(rows.Keys);
            for (int i = 0; i < orderedRows.Count; i++)
            {
                rows[orderedRows[i]].Sort((a, b) => a.col.CompareTo(b.col));
            }

            idleFrames = ExtractSpritesFromRow(rows[orderedRows[0]]);
            walkFrames = ExtractSpritesFromRow(rows[orderedRows[1]]);
            attackFrames = ExtractSpritesFromRow(rows[orderedRows[2]]);
            dieFrames = ExtractSpritesFromRow(rows[orderedRows[3]]);

            return idleFrames.Count > 0 && walkFrames.Count > 0 && attackFrames.Count > 0 && dieFrames.Count > 0;
        }

        private static List<Sprite> ExtractSpritesFromRow(List<(int col, Sprite sprite)> rowFrames)
        {
            var result = new List<Sprite>();
            if (rowFrames.IsNull() || rowFrames.Count <= 0)
            {
                return result;
            }

            for (int i = 0; i < rowFrames.Count; i++)
            {
                if (rowFrames[i].sprite.IsNotNull())
                {
                    result.Add(rowFrames[i].sprite);
                }
            }

            return result;
        }

        private static bool TryParseRowColFromSpriteName(string spriteName, out int row, out int col)
        {
            row = -1;
            col = -1;
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return false;
            }

            string[] tokens = spriteName.Split('_');
            if (tokens.Length < 2)
            {
                return false;
            }

            if (!int.TryParse(tokens[tokens.Length - 2], out row))
            {
                return false;
            }

            if (!int.TryParse(tokens[tokens.Length - 1], out col))
            {
                return false;
            }

            return row >= 0 && col >= 0;
        }

        // 대상 타입/ID 기준 Animator 출력 폴더를 보장한다.
        private static bool TryEnsureAnimatorOutputFolder(BaseUnitTargetType targetType, string targetId, out string folderPath, out string error)
        {
            string typeFolder = "Enemies";
            if (targetType == BaseUnitTargetType.HeroConfig)
            {
                typeFolder = "Heroes";
            }
            else if (targetType == BaseUnitTargetType.BarracksSoldierConfig)
            {
                typeFolder = "Barracks";
            }

            folderPath = $"Assets/Resources/Animations/{typeFolder}/{targetId}";
            string current = "Assets";
            string[] segments = folderPath.Split('/');
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                error = $"Failed to create folder: {folderPath}";
                return false;
            }

            error = null;
            return true;
        }

        // 액션 클립을 생성 또는 갱신한다.
        private static AnimationClip CreateOrUpdateActionClip(
            string outputFolder,
            string targetId,
            string actionName,
            List<Sprite> frames,
            bool loop,
            float fps)
        {
            if (frames == null || frames.Count <= 0)
            {
                return null;
            }

            string clipPath = $"{outputFolder}/{targetId}_{actionName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = Mathf.Max(1f, fps);
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[frames.Count];
            float frameDuration = 1f / Mathf.Max(1f, fps);
            for (int i = 0; i < frames.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * frameDuration,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            SetAnimationClipLoop(clip, loop);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        // 클립 루프 옵션을 설정한다.
        private static void SetAnimationClipLoop(AnimationClip clip, bool loop)
        {
            if (clip == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(clip);
            SerializedProperty settings = so.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                SerializedProperty loopTime = settings.FindPropertyRelative("m_LoopTime");
                if (loopTime != null)
                {
                    loopTime.boolValue = loop;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 액션 클립들로 AnimatorController를 생성 또는 갱신한다.
        private static AnimatorController CreateOrUpdateAnimatorController(
            string controllerPath,
            AnimationClip idleClip,
            AnimationClip walkClip,
            AnimationClip attackClip,
            AnimationClip dieClip)
        {
            if (File.Exists(controllerPath))
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (controller == null)
            {
                return null;
            }

            controller.AddParameter("MotionState", AnimatorControllerParameterType.Int);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            stateMachine.states = Array.Empty<ChildAnimatorState>();

            AnimatorState idleState = AddStateIfClipExists(stateMachine, "Idle", idleClip);
            AnimatorState walkState = AddStateIfClipExists(stateMachine, "Walk", walkClip);
            AnimatorState attackState = AddStateIfClipExists(stateMachine, "Attack", attackClip);
            AnimatorState dieState = AddStateIfClipExists(stateMachine, "Die", dieClip);

            stateMachine.defaultState = idleState ?? walkState ?? attackState ?? dieState;

            AddAnyStateTransition(stateMachine, idleState, "MotionState", 0);
            AddAnyStateTransition(stateMachine, walkState, "MotionState", 1);
            AddAnyStateTransition(stateMachine, attackState, "MotionState", 2);
            AddAnyStateTransition(stateMachine, dieState, "MotionState", 3);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // 상태 머신에 클립이 있을 때만 상태를 추가한다.
        private static AnimatorState AddStateIfClipExists(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip)
        {
            if (stateMachine == null || clip == null)
            {
                return null;
            }

            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
            return state;
        }

        // AnyState -> 타겟 상태 전이를 정수 조건으로 추가한다.
        private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState targetState, string parameterName, int value)
        {
            if (stateMachine == null || targetState == null)
            {
                return;
            }

            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(targetState);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, value, parameterName);
        }

        // 읽기 가능한 임시 텍스처를 생성한다.
        private Texture2D CreateReadableTexture(Texture2D source)
        {
            // 감마/리니어 색공간 변환으로 인한 톤 변화 방지를 위해 기본 표준 적녹청 경로를 유지한다.
            RenderTexture tmp = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default);

            Graphics.Blit(source, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            // 선형 비활성 값으로 생성해 일반 스프라이트 텍스처와 동일한 색공간을 유지한다.
            Texture2D myTexture2D = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply(false, false);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return myTexture2D;
        }

        // 설정된 키 컬러/필터 기준으로 배경 알파를 제거한다.
        private void RemoveBackground(Texture2D texture, Color key, float tolerance)
        {
            Color[] pixels = texture.GetPixels();

            if (_useRgbRangeFilter)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color p = pixels[i];
                    int r = Mathf.RoundToInt(p.r * 255f);
                    int g = Mathf.RoundToInt(p.g * 255f);
                    int b = Mathf.RoundToInt(p.b * 255f);

                    if (IsChannelMatched(r, _thresholdR, _compareR) &&
                        IsChannelMatched(g, _thresholdG, _compareG) &&
                        IsChannelMatched(b, _thresholdB, _compareB))
                    {
                        p.a = 0f;
                        pixels[i] = p;
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                return;
            }

            // 이전 평균절대차 기반은 마젠타(1,0,1)와 파랑(0,0,1)의 차이를 과소평가해
            // 캐릭터 본체가 함께 투명해지는 문제가 있었다.
            // 따라서 색상 채널 유클리드 거리 기반 컷오프로 변경해 본체 보존성을 높인다.
            float cutoff = Mathf.Clamp01(tolerance) * 1.7320508f; // sqrt(3) 스케일로 RGB 거리계에 매핑
            float feather = Mathf.Clamp(tolerance * 0.18f, 0.01f, 0.08f);
            float despillRange = Mathf.Clamp(feather * 2.2f, 0.02f, 0.16f);

            Vector3 keyVec = new Vector3(key.r, key.g, key.b);
            float keyMag = Mathf.Max(0.0001f, keyVec.magnitude);
            Vector3 keyDir = keyVec / keyMag;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                Vector3 rgb = new Vector3(p.r, p.g, p.b);
                float dist = Vector3.Distance(rgb, keyVec);

                // 1) 키 컬러 근접 픽셀은 제거
                if (dist <= cutoff)
                {
                    p.a = 0f;
                    pixels[i] = p;
                    continue;
                }

                // 2) 경계부는 아주 얕은 페더만 적용 (본체 투명화 방지)
                if (dist <= cutoff + feather)
                {
                    float t = Mathf.Clamp01((dist - cutoff) / Mathf.Max(0.0001f, feather));
                    p.a *= Mathf.Lerp(0.75f, 1f, t);
                }

                // 3) 디스필: 키 컬러 방향 성분만 약하게 제거 (알파는 유지)
                if (dist <= cutoff + despillRange && p.a > 0f)
                {
                    float near = 1f - Mathf.Clamp01((dist - cutoff) / Mathf.Max(0.0001f, despillRange));
                    float keyProjection = Mathf.Max(0f, Vector3.Dot(rgb, keyDir));
                    Vector3 keyComponent = keyDir * keyProjection;
                    Vector3 corrected = rgb - (keyComponent * (near * 0.35f));

                    p.r = Mathf.Clamp01(corrected.x);
                    p.g = Mathf.Clamp01(corrected.y);
                    p.b = Mathf.Clamp01(corrected.z);
                }

                if (p.a <= 0.003f)
                {
                    p.a = 0f;
                }

                pixels[i] = p;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        // 색상 채널 비교 조건 만족 여부를 확인한다.
        private static bool IsChannelMatched(int value, int threshold, ChannelComparison compare)
        {
            switch (compare)
            {
                case ChannelComparison.Ignore:
                    return true;
                case ChannelComparison.GreaterOrEqual:
                    return value >= threshold;
                case ChannelComparison.LessOrEqual:
                    return value <= threshold;
                default:
                    return false;
            }
        }

        // 재임포트 전에 임포터를 단일 모드로 초기화한다.
        private static void ResetImporterToSingleSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;

#pragma warning disable 0618
            importer.spritesheet = Array.Empty<SpriteMetaData>();
#pragma warning restore 0618
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        // 스프라이트 메타데이터 목록을 임포터에 적용한다.
        private void ApplySlicing(string path, List<SpriteMetaData> metas)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;

            if (metas == null)
            {
                metas = new List<SpriteMetaData>();
            }

#pragma warning disable 0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore 0618
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        // 현재 슬라이싱 설정으로 스프라이트 메타데이터 목록을 생성한다.
        private List<SpriteMetaData> BuildSpriteMetaData(string path, Texture2D slicingTexture, int width, int height, float overrideOffX, float overrideOffY, List<string> warnings)
        {
            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            if (slicingTexture == null || width <= 0 || height <= 0)
            {
                if (warnings != null)
                {
                    warnings.Add("Slicing texture is null or has invalid dimensions.");
                }

                return metas;
            }

            string baseName = Path.GetFileNameWithoutExtension(path);
            if (_slicingMode == SlicingMode.SmartSlice)
            {
                List<Rect> smartRects = DetectIslands(slicingTexture, _smartSliceAlphaThreshold, _smartSliceMinPixels, _smartSliceOuterPadding);
                if (_smartSliceSplitActionsByRows)
                {
                    return BuildSmartSliceActionMetaData(baseName, smartRects, width, height, warnings);
                }

                for (int i = 0; i < smartRects.Count; i++)
                {
                    Rect clamped = ClampRectToTexture(smartRects[i], width, height);
                    if (clamped.width <= 0f || clamped.height <= 0f)
                    {
                        if (warnings != null)
                        {
                            warnings.Add($"SmartSlice frame #{i} was out of bounds and skipped.");
                        }

                        continue;
                    }

                    metas.Add(CreateSpriteMeta($"{baseName}_{i:00}", clamped));
                }

                return metas;
            }

            float cellW;
            float cellH;
            float offX;
            float offY;
            float spcX;
            float spcY;

            if (_slicingMode == SlicingMode.AutoDivide)
            {
                int safeCols = Mathf.Max(1, _cols);
                int safeRows = Mathf.Max(1, _rows);
                cellW = width / (float)safeCols;
                cellH = height / (float)safeRows;
                offX = 0f;
                offY = 0f;
                spcX = 0f;
                spcY = 0f;
            }
            else
            {
                cellW = _cellSize.x;
                cellH = _cellSize.y;
                offX = overrideOffX;
                offY = overrideOffY;
                spcX = _spacing.x;
                spcY = _spacing.y;
            }

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    float guiY = offY + (r * (cellH + spcY));
                    float textureBottomY = height - (guiY + cellH);

                    float x = offX + (c * (cellW + spcX)) + _innerPadding;
                    float y = textureBottomY + _innerPadding;
                    float w = cellW - (_innerPadding * 2f);
                    float h = cellH - (_innerPadding * 2f);

                    if (w <= 0f || h <= 0f)
                    {
                        if (warnings != null)
                        {
                            warnings.Add($"Grid frame ({r},{c}) collapsed to zero size and was skipped.");
                        }

                        continue;
                    }

                    Rect clamped = ClampRectToTexture(new Rect(x, y, w, h), width, height);
                    if (clamped.width <= 0f || clamped.height <= 0f)
                    {
                        if (warnings != null)
                        {
                            warnings.Add($"Grid frame ({r},{c}) was out of texture bounds and skipped.");
                        }

                        continue;
                    }

                    metas.Add(CreateSpriteMeta($"{baseName}_{r}_{c}", clamped));
                }
            }

            return metas;
        }

        private List<SpriteMetaData> BuildSmartSliceActionMetaData(string baseName, List<Rect> smartRects, int width, int height, List<string> warnings)
        {
            var metas = new List<SpriteMetaData>();
            int rowCount = Mathf.Max(2, _smartSliceActionRowCount);
            if (smartRects == null || smartRects.Count <= 0)
            {
                return metas;
            }

            var clampedRects = new List<Rect>(smartRects.Count);
            for (int i = 0; i < smartRects.Count; i++)
            {
                Rect clamped = ClampRectToTexture(smartRects[i], width, height);
                if (clamped.width <= 0f || clamped.height <= 0f)
                {
                    if (warnings != null)
                    {
                        warnings.Add($"SmartSlice frame #{i} was out of bounds and skipped.");
                    }

                    continue;
                }

                clampedRects.Add(clamped);
            }

            if (clampedRects.Count <= 0)
            {
                return metas;
            }

            if (clampedRects.Count < rowCount)
            {
                if (warnings != null)
                {
                    warnings.Add($"SmartSlice action-row split skipped: frameCount({clampedRects.Count}) < rowCount({rowCount}).");
                }

                for (int i = 0; i < clampedRects.Count; i++)
                {
                    metas.Add(CreateSpriteMeta($"{baseName}_{i:00}", clampedRects[i]));
                }

                return metas;
            }

            float topCenter = float.MinValue;
            float bottomCenter = float.MaxValue;
            float[] centers = new float[clampedRects.Count];
            for (int i = 0; i < clampedRects.Count; i++)
            {
                float center = clampedRects[i].center.y;
                centers[i] = center;
                if (center > topCenter) topCenter = center;
                if (center < bottomCenter) bottomCenter = center;
            }

            float range = topCenter - bottomCenter;
            if (range < 0.0001f)
            {
                if (warnings != null)
                {
                    warnings.Add("SmartSlice action-row split skipped: vertical range too small.");
                }

                clampedRects.Sort((a, b) => a.x.CompareTo(b.x));
                for (int i = 0; i < clampedRects.Count; i++)
                {
                    metas.Add(CreateSpriteMeta($"{baseName}_{i:00}", clampedRects[i]));
                }

                return metas;
            }

            float step = range / rowCount;
            if (step < 0.5f)
            {
                if (warnings != null)
                {
                    warnings.Add("SmartSlice action-row split skipped: inferred row step too small.");
                }

                for (int i = 0; i < clampedRects.Count; i++)
                {
                    metas.Add(CreateSpriteMeta($"{baseName}_{i:00}", clampedRects[i]));
                }

                return metas;
            }

            var buckets = new List<Rect>[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                buckets[i] = new List<Rect>();
            }

            for (int i = 0; i < clampedRects.Count; i++)
            {
                int rowIndex = Mathf.Clamp(Mathf.FloorToInt((topCenter - centers[i]) / step), 0, rowCount - 1);
                buckets[rowIndex].Add(clampedRects[i]);
            }

            bool hasEmptyRow = false;
            for (int i = 0; i < rowCount; i++)
            {
                if (buckets[i].Count <= 0)
                {
                    hasEmptyRow = true;
                    break;
                }
            }

            if (hasEmptyRow)
            {
                if (warnings != null)
                {
                    warnings.Add("SmartSlice action-row split skipped: one or more rows are empty.");
                }

                clampedRects.Sort((a, b) =>
                {
                    int yCompare = b.center.y.CompareTo(a.center.y);
                    return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
                });

                for (int i = 0; i < clampedRects.Count; i++)
                {
                    metas.Add(CreateSpriteMeta($"{baseName}_{i:00}", clampedRects[i]));
                }

                return metas;
            }

            string[] actionLabels = { "idle", "walk", "attack", "die" };
            for (int row = 0; row < rowCount; row++)
            {
                buckets[row].Sort((a, b) => a.x.CompareTo(b.x));
                string label = rowCount == 4 && row < actionLabels.Length ? actionLabels[row] : $"row{row:00}";
                for (int frame = 0; frame < buckets[row].Count; frame++)
                {
                    metas.Add(CreateSpriteMeta($"{baseName}_{label}_{frame:00}", buckets[row][frame]));
                }
            }

            return metas;
        }

        // 단일 스프라이트 메타데이터를 생성한다.
        private static SpriteMetaData CreateSpriteMeta(string name, Rect rect)
        {
            return new SpriteMetaData
            {
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(DefaultPivotX, DefaultPivotY),
                border = Vector4.zero,
                name = name,
                rect = rect
            };
        }

        // 프레임 목록에서 최대 폭/높이를 계산한다.
        private static void CalculateMaxFrameSize(List<SpriteMetaData> metas, out int maxFrameWidth, out int maxFrameHeight)
        {
            maxFrameWidth = 0;
            maxFrameHeight = 0;
            if (metas == null)
            {
                return;
            }

            for (int i = 0; i < metas.Count; i++)
            {
                SpriteMetaData meta = metas[i];
                maxFrameWidth = Mathf.Max(maxFrameWidth, Mathf.CeilToInt(meta.rect.width));
                maxFrameHeight = Mathf.Max(maxFrameHeight, Mathf.CeilToInt(meta.rect.height));
            }
        }

        // 정규화 결과용 열 개수를 계산한다.
        private int GetNormalizedColumnCount(int frameCount)
        {
            if (frameCount <= 0)
            {
                return 1;
            }

            if (_slicingMode == SlicingMode.AutoDivide || _slicingMode == SlicingMode.CustomGrid)
            {
                return Mathf.Max(1, _cols);
            }

            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(frameCount)));
        }

        // 프레임 크기를 통일한 정규화 시트를 생성한다.
        private bool TryNormalizeFrames(Texture2D sourceTexture, List<SpriteMetaData> sourceMetas, out Texture2D normalizedTexture, out List<SpriteMetaData> normalizedMetas, out int maxFrameWidth, out int maxFrameHeight, out bool normalizedFrames)
        {
            normalizedTexture = null;
            normalizedMetas = new List<SpriteMetaData>();
            maxFrameWidth = 0;
            maxFrameHeight = 0;
            normalizedFrames = false;

            if (sourceTexture == null || sourceMetas == null || sourceMetas.Count <= 0)
            {
                return false;
            }

            CalculateMaxFrameSize(sourceMetas, out maxFrameWidth, out maxFrameHeight);
            if (maxFrameWidth <= 0 || maxFrameHeight <= 0)
            {
                return false;
            }

            int columns = GetNormalizedColumnCount(sourceMetas.Count);
            int rows = Mathf.Max(1, Mathf.CeilToInt(sourceMetas.Count / (float)columns));
            int atlasWidth = columns * maxFrameWidth;
            int atlasHeight = rows * maxFrameHeight;

            if (atlasWidth <= 0 || atlasHeight <= 0)
            {
                return false;
            }

            Color32[] sourcePixels = sourceTexture.GetPixels32();
            Color32[] destPixels = new Color32[atlasWidth * atlasHeight];
            int sourceWidth = sourceTexture.width;
            int sourceHeight = sourceTexture.height;

            for (int i = 0; i < sourceMetas.Count; i++)
            {
                Rect srcRect = ClampRectToTexture(sourceMetas[i].rect, sourceWidth, sourceHeight);
                int srcXMin = Mathf.Clamp(Mathf.FloorToInt(srcRect.xMin), 0, sourceWidth - 1);
                int srcYMin = Mathf.Clamp(Mathf.FloorToInt(srcRect.yMin), 0, sourceHeight - 1);
                int srcXMax = Mathf.Clamp(Mathf.CeilToInt(srcRect.xMax), srcXMin + 1, sourceWidth);
                int srcYMax = Mathf.Clamp(Mathf.CeilToInt(srcRect.yMax), srcYMin + 1, sourceHeight);

                int srcW = Mathf.Max(1, srcXMax - srcXMin);
                int srcH = Mathf.Max(1, srcYMax - srcYMin);

                int col = i % columns;
                int rowFromTop = i / columns;
                int rowFromBottom = rows - 1 - rowFromTop;
                int cellX = col * maxFrameWidth;
                int cellY = rowFromBottom * maxFrameHeight;

                int destX = cellX + Mathf.Max(0, (maxFrameWidth - srcW) / 2);
                int destY = cellY;

                for (int y = 0; y < srcH; y++)
                {
                    int srcRow = (srcYMin + y) * sourceWidth;
                    int dstRow = (destY + y) * atlasWidth;
                    for (int x = 0; x < srcW; x++)
                    {
                        int srcIndex = srcRow + srcXMin + x;
                        int dstIndex = dstRow + destX + x;
                        if (srcIndex < 0 || srcIndex >= sourcePixels.Length || dstIndex < 0 || dstIndex >= destPixels.Length)
                        {
                            continue;
                        }

                        destPixels[dstIndex] = sourcePixels[srcIndex];
                    }
                }

                normalizedMetas.Add(CreateSpriteMeta(sourceMetas[i].name, new Rect(cellX, cellY, maxFrameWidth, maxFrameHeight)));
            }

            normalizedTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false);
            normalizedTexture.SetPixels32(destPixels);
            normalizedTexture.Apply(false, false);
            normalizedFrames = true;
            return true;
        }

        // 현재 처리 옵션을 매니페스트 옵션 객체로 구성한다.
        private ManifestOptions BuildManifestOptions()
        {
            return new ManifestOptions
            {
                removeBackground = _removeBackground,
                useRgbRangeFilter = _useRgbRangeFilter,
                tolerance = _tolerance,
                thresholdR = _thresholdR,
                thresholdG = _thresholdG,
                thresholdB = _thresholdB,
                compareR = _compareR,
                compareG = _compareG,
                compareB = _compareB,
                cropToSelection = _cropToSelection,
                normalizeFrames = _normalizeFrames,
                slicingMode = _slicingMode,
                rows = _rows,
                cols = _cols,
                innerPadding = _innerPadding,
                smartSliceAlphaThreshold = _smartSliceAlphaThreshold,
                smartSliceMinPixels = _smartSliceMinPixels,
                smartSliceOuterPadding = _smartSliceOuterPadding,
                smartSliceSplitActionsByRows = _smartSliceSplitActionsByRows,
                smartSliceActionRowCount = _smartSliceActionRowCount,
            };
        }

        // 중복 없이 문자열 엔트리를 목록에 추가한다.
        private static void AddUniqueEntry(List<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            values.Add(value);
        }

        // 매니페스트에서 액션 그룹 레코드를 찾는다.
        private static ManifestActionRecord FindActionRecord(AISpriteManifest manifest, string actionGroup)
        {
            if (manifest == null || manifest.actions == null)
            {
                return null;
            }

            for (int i = 0; i < manifest.actions.Count; i++)
            {
                ManifestActionRecord record = manifest.actions[i];
                if (string.Equals(record.actionGroup, actionGroup, StringComparison.OrdinalIgnoreCase))
                {
                    return record;
                }
            }

            return null;
        }

        // 매니페스트 파일을 로드한다.
        private static AISpriteManifest LoadManifest(string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                return new AISpriteManifest();
            }

            try
            {
                string json = File.ReadAllText(manifestPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new AISpriteManifest();
                }

                AISpriteManifest manifest = JsonUtility.FromJson<AISpriteManifest>(json);
                if (manifest == null)
                {
                    manifest = new AISpriteManifest();
                }

                if (manifest.sourceFiles == null)
                {
                    manifest.sourceFiles = new List<string>();
                }

                if (manifest.actions == null)
                {
                    manifest.actions = new List<ManifestActionRecord>();
                }

                return manifest;
            }
            catch
            {
                return new AISpriteManifest();
            }
        }

        // 처리 결과를 매니페스트 파일에 기록한다.
        private void WriteManifest(string sourcePath, string processedPath, string actionGroup, string sourceFileName, int frameCount, int maxFrameWidth, int maxFrameHeight, bool normalizedFrames, List<string> warnings)
        {
            string outputDir = Path.GetDirectoryName(processedPath);
            if (string.IsNullOrEmpty(outputDir))
            {
                return;
            }

            string manifestPath = Path.Combine(outputDir, ManifestFileName);
            AISpriteManifest manifest = LoadManifest(manifestPath);
            manifest.updatedAtUtc = DateTime.UtcNow.ToString("o");

            string sourceFile = Path.GetFileName(sourcePath);
            AddUniqueEntry(manifest.sourceFiles, sourceFile);

            ManifestActionRecord record = FindActionRecord(manifest, actionGroup);
            if (record == null)
            {
                record = new ManifestActionRecord();
                manifest.actions.Add(record);
            }

            record.actionGroup = actionGroup;
            record.sourceFile = sourceFile;
            record.outputTexture = Path.GetFileName(processedPath);
            record.frameCount = frameCount;
            record.maxFrameWidth = maxFrameWidth;
            record.maxFrameHeight = maxFrameHeight;
            record.pivotX = DefaultPivotX;
            record.pivotY = DefaultPivotY;
            record.normalizedFrames = normalizedFrames;
            record.slicingMode = _slicingMode.ToString();
            record.options = BuildManifestOptions();

            if (record.warnings == null)
            {
                record.warnings = new List<string>();
            }
            else
            {
                record.warnings.Clear();
            }

            AddUniqueEntry(record.warnings, $"source={sourceFileName}");
            if (warnings != null)
            {
                for (int i = 0; i < warnings.Count; i++)
                {
                    AddUniqueEntry(record.warnings, warnings[i]);
                }
            }

            string json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(manifestPath, json);
        }











        // 미리보기에 사용할 텍스처를 반환한다.
        private Texture2D GetDisplayPreviewTexture()
        {
            if (_sourceTexture == null)
            {
                InvalidatePreviewFrameRectsCache();
                return null;
            }

            if (!_previewApplyProcessing || !_removeBackground)
            {
                if (_previewProcessedTexture != null)
                {
                    DestroyImmediate(_previewProcessedTexture);
                    _previewProcessedTexture = null;
                    InvalidatePreviewFrameRectsCache();
                }

                _previewProcessedHash = int.MinValue;
                return _sourceTexture;
            }

            int hash = ComputePreviewProcessHash();
            if (_previewProcessedTexture != null && _previewProcessedHash == hash)
            {
                return _previewProcessedTexture;
            }

            if (_previewProcessedTexture != null)
            {
                DestroyImmediate(_previewProcessedTexture);
                _previewProcessedTexture = null;
            }

            Texture2D readable = CreateReadableTexture(_sourceTexture);
            if (readable == null)
            {
                return _sourceTexture;
            }

            RemoveBackground(readable, _removeColor, _tolerance);
            _previewProcessedTexture = readable;
            _previewProcessedHash = hash;
            InvalidatePreviewFrameRectsCache();
            return _previewProcessedTexture;
        }

        // 프리뷰 처리 상태 해시를 계산한다.
        private int ComputePreviewProcessHash()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (_sourceTexture != null ? _sourceTexture.GetInstanceID() : 0);
                hash = (hash * 31) + _removeBackground.GetHashCode();
                hash = (hash * 31) + _previewApplyProcessing.GetHashCode();
                hash = (hash * 31) + _removeColor.GetHashCode();
                hash = (hash * 31) + _tolerance.GetHashCode();
                hash = (hash * 31) + _useRgbRangeFilter.GetHashCode();
                hash = (hash * 31) + _thresholdR;
                hash = (hash * 31) + _thresholdG;
                hash = (hash * 31) + _thresholdB;
                hash = (hash * 31) + (int)_compareR;
                hash = (hash * 31) + (int)_compareG;
                hash = (hash * 31) + (int)_compareB;
                return hash;
            }
        }

        // 현재 설정에서 유효한 크롭 영역을 계산한다.
        private bool TryGetCropRect(int texWidth, int texHeight, out RectInt cropRect, out float cropTop)
        {
            cropRect = new RectInt(0, 0, texWidth, texHeight);
            cropTop = 0f;

            float cellW;
            float cellH;
            float offX;
            float offY;
            float spcX;
            float spcY;

            if (_slicingMode == SlicingMode.AutoDivide)
            {
                cellW = texWidth / (float)Mathf.Max(1, _cols);
                cellH = texHeight / (float)Mathf.Max(1, _rows);
                offX = 0f;
                offY = 0f;
                spcX = 0f;
                spcY = 0f;
            }
            else
            {
                cellW = _cellSize.x;
                cellH = _cellSize.y;
                offX = _offset.x;
                offY = _offset.y;
                spcX = _spacing.x;
                spcY = _spacing.y;
            }

            float minX = offX;
            float minYTop = offY;
            float maxX = offX + (_cols * cellW) + ((_cols - 1) * spcX);
            float maxYTop = offY + (_rows * cellH) + ((_rows - 1) * spcY);

            int x = Mathf.Clamp(Mathf.FloorToInt(minX), 0, texWidth - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(texHeight - maxYTop), 0, texHeight - 1);
            int w = Mathf.Clamp(Mathf.CeilToInt(maxX - minX), 1, texWidth - x);
            int h = Mathf.Clamp(Mathf.CeilToInt(maxYTop - minYTop), 1, texHeight - y);

            cropRect = new RectInt(x, y, w, h);
            cropTop = texHeight - (y + h);
            return true;
        }

        // 지정 영역으로 텍스처를 잘라 새 텍스처를 만든다.
        private static Texture2D CropTexture(Texture2D source, RectInt rect)
        {
            if (source == null || rect.width <= 0 || rect.height <= 0)
            {
                return null;
            }

            Texture2D result = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
            Color[] colors = source.GetPixels(rect.x, rect.y, rect.width, rect.height);
            result.SetPixels(colors);
            result.Apply();
            return result;
        }

        // 공통 입력값의 유효성을 검증한다.
        private bool TryValidateInputs(out string error)
        {
            if (_sourceTexture == null)
            {
                error = "Source Texture가 비어 있습니다.";
                return false;
            }

            if (_slicingMode != SlicingMode.SmartSlice && (_rows <= 0 || _cols <= 0))
            {
                error = "Rows/Cols는 1 이상이어야 합니다.";
                return false;
            }

            if (_innerPadding < 0)
            {
                error = "Padding은 0 이상이어야 합니다.";
                return false;
            }

            if (_removeBackground && _useRgbRangeFilter)
            {
                _thresholdR = Mathf.Clamp(_thresholdR, 0, 255);
                _thresholdG = Mathf.Clamp(_thresholdG, 0, 255);
                _thresholdB = Mathf.Clamp(_thresholdB, 0, 255);
            }

            if (_slicingMode == SlicingMode.CustomGrid)
            {
                if (_cellSize.x <= 0f || _cellSize.y <= 0f)
                {
                    error = "CustomGrid의 Cell Size는 0보다 커야 합니다.";
                    return false;
                }

                float maxPadding = Mathf.Min(_cellSize.x, _cellSize.y) * 0.5f;
                if (_innerPadding >= maxPadding)
                {
                    error = "Padding이 Cell Size 대비 너무 큽니다. (결과 rect가 0 이하가 됨)";
                    return false;
                }
            }
            else if (_slicingMode == SlicingMode.SmartSlice)
            {
                if (_smartSliceAlphaThreshold <= 0f || _smartSliceAlphaThreshold > 1f)
                {
                    error = "SmartSlice Alpha Threshold는 0보다 크고 1 이하여야 합니다.";
                    return false;
                }

                if (_smartSliceMinPixels <= 0)
                {
                    error = "SmartSlice Min Island Pixels는 1 이상이어야 합니다.";
                    return false;
                }
            }
            else
            {
                float cellW = _sourceTexture.width / (float)Mathf.Max(1, _cols);
                float cellH = _sourceTexture.height / (float)Mathf.Max(1, _rows);
                float maxPadding = Mathf.Min(cellW, cellH) * 0.5f;
                if (_innerPadding >= maxPadding)
                {
                    error = "Padding이 Auto 셀 크기 대비 너무 큽니다. Rows/Cols/Padding을 조정하세요.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        // 현재 화면/처리 설정을 프리셋 객체로 캡처한다.
        private AISpriteProcessConfig CaptureCurrentConfig()
        {
            return new AISpriteProcessConfig
            {
                schemaVersion = 2,
                hasBindingContext = true,
                removeBackground = _removeBackground,
                keyColor = _removeColor,
                tolerance = _tolerance,
                useRgbRangeFilter = _useRgbRangeFilter,
                thresholdR = _thresholdR,
                thresholdG = _thresholdG,
                thresholdB = _thresholdB,
                compareR = _compareR,
                compareG = _compareG,
                compareB = _compareB,
                slicingMode = _slicingMode,
                rows = _rows,
                cols = _cols,
                innerPadding = _innerPadding,
                offset = _offset,
                cellSize = _cellSize,
                spacing = _spacing,
                smartSliceAlphaThreshold = _smartSliceAlphaThreshold,
                smartSliceMinPixels = _smartSliceMinPixels,
                smartSliceOuterPadding = _smartSliceOuterPadding,
                smartSliceSplitActionsByRows = _smartSliceSplitActionsByRows,
                smartSliceActionRowCount = _smartSliceActionRowCount,
                normalizeFrames = _normalizeFrames,
                emitManifest = _emitManifest,
                useManualActionGroup = _useManualActionGroup,
                singleSourceActionGroup = _singleSourceActionGroup,
                activeTab = _activeTab,
                baseUnitTargetType = _baseUnitContext.targetType,
                baseUnitUseManualActionGroup = _baseUnitContext.useManualActionGroup,
                baseUnitActionGroup = _baseUnitContext.actionGroup,
                baseUnitAutoApplyAfterProcess = _baseUnitContext.autoApplyAfterProcess,
                towerLevelIndex = _towerContext.levelIndex,
                towerAutoApplyAfterProcess = _towerContext.autoApplyAfterProcess,
            };
        }

        // 프리셋 설정을 현재 화면/컨텍스트에 적용한다.
        private void ApplyConfig(AISpriteProcessConfig config)
        {
            if (config == null)
            {
                return;
            }

            _removeBackground = config.removeBackground;
            _removeColor = config.keyColor;
            _tolerance = Mathf.Clamp01(config.tolerance);
            _useRgbRangeFilter = config.useRgbRangeFilter;
            _thresholdR = Mathf.Clamp(config.thresholdR, 0, 255);
            _thresholdG = Mathf.Clamp(config.thresholdG, 0, 255);
            _thresholdB = Mathf.Clamp(config.thresholdB, 0, 255);
            _compareR = config.compareR;
            _compareG = config.compareG;
            _compareB = config.compareB;
            _slicingMode = config.slicingMode;
            _rows = Mathf.Max(1, config.rows);
            _cols = Mathf.Max(1, config.cols);
            _innerPadding = Mathf.Max(0, config.innerPadding);
            _offset = config.offset;
            _cellSize = new Vector2(Mathf.Max(1f, config.cellSize.x), Mathf.Max(1f, config.cellSize.y));
            _spacing = config.spacing;
            _smartSliceAlphaThreshold = Mathf.Clamp(config.smartSliceAlphaThreshold <= 0f ? 0.1f : config.smartSliceAlphaThreshold, 0.001f, 1f);
            _smartSliceMinPixels = Mathf.Max(1, config.smartSliceMinPixels <= 0 ? 64 : config.smartSliceMinPixels);
            _smartSliceOuterPadding = Mathf.Max(0, config.smartSliceOuterPadding);
            _smartSliceSplitActionsByRows = config.smartSliceSplitActionsByRows;
            _smartSliceActionRowCount = Mathf.Max(2, config.smartSliceActionRowCount <= 0 ? 4 : config.smartSliceActionRowCount);
            _normalizeFrames = config.normalizeFrames;
            _emitManifest = config.emitManifest;

            bool hasBindingContext = config.schemaVersion >= 2 && config.hasBindingContext;
            if (hasBindingContext)
            {
                _activeTab = config.activeTab;
                _baseUnitContext.targetType = config.baseUnitTargetType;
                _baseUnitContext.useManualActionGroup = config.baseUnitUseManualActionGroup;
                _baseUnitContext.actionGroup = config.baseUnitActionGroup;
                _baseUnitContext.autoApplyAfterProcess = config.baseUnitAutoApplyAfterProcess;
                _towerContext.levelIndex = Mathf.Max(1, config.towerLevelIndex);
                _towerContext.autoApplyAfterProcess = config.towerAutoApplyAfterProcess;
            }
            else
            {
                _baseUnitContext.useManualActionGroup = config.useManualActionGroup;
                _baseUnitContext.actionGroup = config.singleSourceActionGroup;
            }

            _useManualActionGroup = _baseUnitContext.useManualActionGroup;
            _singleSourceActionGroup = _baseUnitContext.actionGroup;
            InvalidatePreviewFrameRectsCache();
        }

        private void InvalidatePreviewFrameRectsCache()
        {
            _previewFrameRectsDirty = true;
            _previewFrameRectsTextureId = 0;
            if (_previewFrameRectsCache == null)
            {
                _previewFrameRectsCache = new List<Rect>();
            }
            else
            {
                _previewFrameRectsCache.Clear();
            }
        }

        // 알파 기반 연결 영역을 감지해 사각형 목록을 생성한다.
        private List<Rect> DetectIslands(Texture2D texture, float alphaThreshold, int minIslandPixels, int outerPadding)
        {
            var result = new List<Rect>();
            if (texture == null)
            {
                return result;
            }

            int width = texture.width;
            int height = texture.height;
            if (width <= 0 || height <= 0)
            {
                return result;
            }

            Color32[] pixels = texture.GetPixels32();
            int size = width * height;
            bool[] visited = new bool[size];
            int alphaByteThreshold = Mathf.Clamp(Mathf.RoundToInt(alphaThreshold * 255f), 1, 255);
            var queue = new Queue<int>(256);
            int[] neighbors = { -1, 1, -width, width };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int start = (y * width) + x;
                    if (visited[start] || pixels[start].a < alphaByteThreshold)
                    {
                        continue;
                    }

                    int minX = x, minY = y, maxX = x, maxY = y;
                    int count = 0;
                    visited[start] = true;
                    queue.Enqueue(start);

                    while (queue.Count > 0)
                    {
                        int idx = queue.Dequeue();
                        int cx = idx % width;
                        int cy = idx / width;
                        count++;

                        if (cx < minX) minX = cx;
                        if (cy < minY) minY = cy;
                        if (cx > maxX) maxX = cx;
                        if (cy > maxY) maxY = cy;

                        for (int i = 0; i < neighbors.Length; i++)
                        {
                            int next = idx + neighbors[i];
                            if (next < 0 || next >= size || visited[next])
                            {
                                continue;
                            }

                            int nx = next % width;
                            int ny = next / width;
                            if ((neighbors[i] == -1 || neighbors[i] == 1) && ny != cy)
                            {
                                continue;
                            }

                            if (pixels[next].a < alphaByteThreshold)
                            {
                                continue;
                            }

                            visited[next] = true;
                            queue.Enqueue(next);
                        }
                    }

                    if (count < Mathf.Max(1, minIslandPixels))
                    {
                        continue;
                    }

                    int xMin = Mathf.Clamp(minX - outerPadding, 0, width - 1);
                    int yMin = Mathf.Clamp(minY - outerPadding, 0, height - 1);
                    int xMax = Mathf.Clamp(maxX + outerPadding, 0, width - 1);
                    int yMax = Mathf.Clamp(maxY + outerPadding, 0, height - 1);

                    result.Add(Rect.MinMaxRect(xMin, yMin, xMax + 1, yMax + 1));
                }
            }

            result.Sort((a, b) =>
            {
                float aTop = height - a.yMax;
                float bTop = height - b.yMax;
                if (Mathf.Abs(aTop - bTop) > 1f)
                {
                    return aTop.CompareTo(bTop);
                }

                return a.x.CompareTo(b.x);
            });

            return result;
        }

        // 현재 설정을 프리셋 파일로 저장한다.
        private void SaveCurrentAsPreset()
        {
            string cleanName = SanitizePresetName(_presetNameInput);
            if (string.IsNullOrEmpty(cleanName))
            {
                NotifyWarning("프리셋 저장", "Preset Name을 입력하세요.");
                return;
            }

            EnsurePresetFolder();
            string path = GetPresetPath(cleanName);
            string json = EditorJsonUtility.ToJson(CaptureCurrentConfig(), true);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();

            _presetListDirty = true;
            RefreshPresetList(cleanName);
        }

        // 선택한 프리셋 파일을 로드해 적용한다.
        private void LoadSelectedPreset()
        {
            if (_selectedPresetIndex < 0 || _selectedPresetIndex >= _presetNames.Count)
            {
                return;
            }

            string presetName = _presetNames[_selectedPresetIndex];
            string path = GetPresetPath(presetName);
            if (!File.Exists(path))
            {
                NotifyError("프리셋 로드", $"파일이 존재하지 않습니다. path={path}");
                _presetListDirty = true;
                return;
            }

            string json = File.ReadAllText(path);
            var config = new AISpriteProcessConfig();
            EditorJsonUtility.FromJsonOverwrite(json, config);
            ApplyConfig(config);
            ValidatePresetLoadState(config, presetName);
            _presetNameInput = presetName;
        }

        // 저장된 모든 프리셋을 순회하며 로드 상태 회귀 검증을 수행한다.
        private void RunPresetLoadRegression()
        {
            RefreshPresetList();
            if (_presetNames.Count <= 0)
            {
                Debug.LogWarning("[AISpriteProcessor] 로드 회귀 검증 중단: 프리셋이 없습니다.");
                NotifyWarning("프리셋 로드 회귀 검증", "검증할 프리셋이 없습니다.");
                return;
            }

            AISpriteProcessConfig backupConfig = CaptureCurrentConfig();
            string backupNameInput = _presetNameInput;
            int backupSelectedPresetIndex = _selectedPresetIndex;

            var presetNames = new List<string>(_presetNames);
            int passCount = 0;
            int failCount = 0;
            var failureSummaries = new List<string>();

            for (int i = 0; i < presetNames.Count; i++)
            {
                string presetName = presetNames[i];
                string path = GetPresetPath(presetName);
                if (!File.Exists(path))
                {
                    failCount++;
                    string missingText = $"preset={presetName} path={path} (파일 없음)";
                    failureSummaries.Add(missingText);
                    Debug.LogWarning($"[AISpriteProcessor] 프리셋 로드 상태 검증 실패. {missingText}");
                    continue;
                }

                var config = new AISpriteProcessConfig();
                try
                {
                    string json = File.ReadAllText(path);
                    EditorJsonUtility.FromJsonOverwrite(json, config);
                }
                catch (Exception ex)
                {
                    failCount++;
                    string errorText = $"preset={presetName} (역직렬화 예외: {ex.Message})";
                    failureSummaries.Add(errorText);
                    Debug.LogWarning($"[AISpriteProcessor] 프리셋 로드 상태 검증 실패. {errorText}");
                    continue;
                }

                ApplyConfig(config);
                if (TryCollectPresetLoadStateMismatches(config, presetName, out string contextText, out List<string> mismatches))
                {
                    passCount++;
                    Debug.Log($"[AISpriteProcessor] 프리셋 로드 상태 검증 통과. {contextText}");
                    continue;
                }

                failCount++;
                string report = BuildPresetLoadValidationReport("[AISpriteProcessor] 프리셋 로드 상태 검증 실패.", contextText, mismatches);
                failureSummaries.Add($"{presetName} ({mismatches.Count}건)");
                Debug.LogWarning(report);
            }

            ApplyConfig(backupConfig);
            _presetNameInput = backupNameInput;
            _selectedPresetIndex = backupSelectedPresetIndex;
            Repaint();

            string summary = $"총 {presetNames.Count}개 / 통과 {passCount} / 실패 {failCount}";
            if (failCount <= 0)
            {
                Debug.Log($"[AISpriteProcessor] 프리셋 로드 회귀 검증 완료. {summary}");
                NotifyInfo("프리셋 로드 회귀 검증", $"검증 완료. {summary}");
                return;
            }

            string failureList = string.Join("\n - ", failureSummaries);
            Debug.LogWarning($"[AISpriteProcessor] 프리셋 로드 회귀 검증 완료(실패 포함). {summary}\n - {failureList}");
            NotifyWarning("프리셋 로드 회귀 검증", $"검증 완료(실패 포함). {summary}");
        }

        // 프리셋 로드 직후 핵심 상태가 유지되는지 회귀 검증 로그를 남긴다.
        private void ValidatePresetLoadState(AISpriteProcessConfig loadedConfig, string presetName)
        {
            if (loadedConfig == null)
            {
                Debug.LogWarning("[AISpriteProcessor] 프리셋 로드 검증 실패: loadedConfig가 null입니다.");
                return;
            }

            if (TryCollectPresetLoadStateMismatches(loadedConfig, presetName, out string contextText, out List<string> mismatches))
            {
                Debug.Log($"[AISpriteProcessor] 프리셋 로드 상태 검증 통과. {contextText}");
                return;
            }

            Debug.LogWarning(BuildPresetLoadValidationReport("[AISpriteProcessor] 프리셋 로드 상태 검증 실패.", contextText, mismatches));
        }

        // 프리셋 로드 결과의 불일치 항목을 수집한다.
        private bool TryCollectPresetLoadStateMismatches(
            AISpriteProcessConfig loadedConfig,
            string presetName,
            out string contextText,
            out List<string> mismatches)
        {
            mismatches = new List<string>();
            contextText =
                $"preset={presetName}, targetType={_baseUnitContext.targetType}, actionGroup={_baseUnitContext.actionGroup}, " +
                $"baseAutoApply={_baseUnitContext.autoApplyAfterProcess}, towerAutoApply={_towerContext.autoApplyAfterProcess}";

            if (loadedConfig == null)
            {
                mismatches.Add("loadedConfig is null");
                return false;
            }

            bool hasBindingContext = loadedConfig.schemaVersion >= 2 && loadedConfig.hasBindingContext;
            if (hasBindingContext)
            {
                if (_activeTab != loadedConfig.activeTab)
                {
                    mismatches.Add($"activeTab expected={loadedConfig.activeTab}, actual={_activeTab}");
                }

                if (_baseUnitContext.targetType != loadedConfig.baseUnitTargetType)
                {
                    mismatches.Add($"baseUnit.targetType expected={loadedConfig.baseUnitTargetType}, actual={_baseUnitContext.targetType}");
                }

                if (_baseUnitContext.useManualActionGroup != loadedConfig.baseUnitUseManualActionGroup)
                {
                    mismatches.Add($"baseUnit.useManualActionGroup expected={loadedConfig.baseUnitUseManualActionGroup}, actual={_baseUnitContext.useManualActionGroup}");
                }

                if (_baseUnitContext.actionGroup != loadedConfig.baseUnitActionGroup)
                {
                    mismatches.Add($"baseUnit.actionGroup expected={loadedConfig.baseUnitActionGroup}, actual={_baseUnitContext.actionGroup}");
                }

                if (_baseUnitContext.autoApplyAfterProcess != loadedConfig.baseUnitAutoApplyAfterProcess)
                {
                    mismatches.Add($"baseUnit.autoApplyAfterProcess expected={loadedConfig.baseUnitAutoApplyAfterProcess}, actual={_baseUnitContext.autoApplyAfterProcess}");
                }

                int expectedTowerLevel = Mathf.Max(1, loadedConfig.towerLevelIndex);
                if (_towerContext.levelIndex != expectedTowerLevel)
                {
                    mismatches.Add($"tower.levelIndex expected={expectedTowerLevel}, actual={_towerContext.levelIndex}");
                }

                if (_towerContext.autoApplyAfterProcess != loadedConfig.towerAutoApplyAfterProcess)
                {
                    mismatches.Add($"tower.autoApplyAfterProcess expected={loadedConfig.towerAutoApplyAfterProcess}, actual={_towerContext.autoApplyAfterProcess}");
                }
            }
            else
            {
                if (_baseUnitContext.useManualActionGroup != loadedConfig.useManualActionGroup)
                {
                    mismatches.Add($"legacy.baseUnit.useManualActionGroup expected={loadedConfig.useManualActionGroup}, actual={_baseUnitContext.useManualActionGroup}");
                }

                if (_baseUnitContext.actionGroup != loadedConfig.singleSourceActionGroup)
                {
                    mismatches.Add($"legacy.baseUnit.actionGroup expected={loadedConfig.singleSourceActionGroup}, actual={_baseUnitContext.actionGroup}");
                }
            }

            if (_useManualActionGroup != _baseUnitContext.useManualActionGroup)
            {
                mismatches.Add($"mirror.useManualActionGroup expected={_baseUnitContext.useManualActionGroup}, actual={_useManualActionGroup}");
            }

            if (_singleSourceActionGroup != _baseUnitContext.actionGroup)
            {
                mismatches.Add($"mirror.singleSourceActionGroup expected={_baseUnitContext.actionGroup}, actual={_singleSourceActionGroup}");
            }

            return mismatches.Count <= 0;
        }

        // 프리셋 로드 검증 불일치 로그 문자열을 생성한다.
        private static string BuildPresetLoadValidationReport(string title, string contextText, List<string> mismatches)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine(title);
            builder.AppendLine(contextText);
            if (mismatches != null)
            {
                for (int i = 0; i < mismatches.Count; i++)
                {
                    builder.Append(" - ");
                    builder.AppendLine(mismatches[i]);
                }
            }

            return builder.ToString();
        }

        // 선택한 프리셋 파일을 삭제한다.
        private void DeleteSelectedPreset()
        {
            if (_selectedPresetIndex < 0 || _selectedPresetIndex >= _presetNames.Count)
            {
                return;
            }

            string presetName = _presetNames[_selectedPresetIndex];
            string path = GetPresetPath(presetName);
            NotifyInfo("프리셋 삭제", $"preset={presetName}");

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            string metaPath = path + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            AssetDatabase.Refresh();
            _presetListDirty = true;
            RefreshPresetList();
        }

        // 프리셋 파일 목록을 스캔해 화면 목록을 갱신한다.
        private void RefreshPresetList(string preferSelectName = null)
        {
            EnsurePresetFolder();
            _presetNames.Clear();

            string[] jsonFiles = Directory.GetFiles(PresetFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < jsonFiles.Length; i++)
            {
                _presetNames.Add(Path.GetFileNameWithoutExtension(jsonFiles[i]));
            }

            _presetNames.Sort();

            if (_presetNames.Count == 0)
            {
                _selectedPresetIndex = -1;
                _presetListDirty = false;
                return;
            }

            if (!string.IsNullOrEmpty(preferSelectName))
            {
                int found = _presetNames.IndexOf(preferSelectName);
                if (found >= 0)
                {
                    _selectedPresetIndex = found;
                    _presetListDirty = false;
                    return;
                }
            }

            _selectedPresetIndex = Mathf.Clamp(_selectedPresetIndex, 0, _presetNames.Count - 1);
            _presetListDirty = false;
        }

        // 프리셋 저장 폴더를 보장한다.
        private static void EnsurePresetFolder()
        {
            if (AssetDatabase.IsValidFolder("Assets/Editor") == false)
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }

            if (AssetDatabase.IsValidFolder(PresetFolderPath) == false)
            {
                AssetDatabase.CreateFolder("Assets/Editor", "AISpritePresets");
            }
        }

        // 프리셋 파일명으로 사용할 문자열을 정리한다.
        private static string SanitizePresetName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string name = raw.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid.ToString(), string.Empty);
            }

            return name;
        }

        // 프리셋 이름으로 저장 파일 경로를 생성한다.
        private static string GetPresetPath(string presetName)
        {
            return Path.Combine(PresetFolderPath, presetName + ".json");
        }

    }
}
