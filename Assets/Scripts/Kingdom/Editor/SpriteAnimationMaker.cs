using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Kingdom.Editor
{
    /// <summary>
    /// 격자 형태의 스프라이트 시트를 자동으로 슬라이스하고 애니메이션 클립을 생성하는 도구.
    /// 사용법: 프로젝트 뷰에서 텍스처 선택 -> 우클릭 -> Kingdom -> Create Anim from Sprite Sheet
    /// </summary>
    public class SpriteAnimationMaker : EditorWindow
    {
        private Texture2D _targetTexture;
        private int _rows = 4;
        private int _cols = 6;
        private string[] _rowNames = new string[] { "Idle", "Run", "Attack", "Block" };
        private int _samples = 12; // FPS

        [MenuItem("Assets/Kingdom/Create Anim from Sprite Sheet", false, 2000)]
        private static void ShowWindow()
        {
            var window = GetWindow<SpriteAnimationMaker>("Anim Maker");
            if (Selection.activeObject is Texture2D tex)
            {
                window._targetTexture = tex;
            }
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Sprite Sheet Animation Maker", EditorStyles.boldLabel);

            _targetTexture = (Texture2D)EditorGUILayout.ObjectField("Texture", _targetTexture, typeof(Texture2D), false);
            _samples = EditorGUILayout.IntField("Sample Rate (FPS)", _samples);
            
            GUILayout.Space(10);
            GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
            _rows = EditorGUILayout.IntField("Rows", _rows);
            _cols = EditorGUILayout.IntField("Columns", _cols);

            GUILayout.Space(10);
            GUILayout.Label("Row Names (Top to Bottom)", EditorStyles.boldLabel);
            
            // 행 개수에 맞춰 배열 크기 조정
            if (_rowNames.Length != _rows)
            {
                System.Array.Resize(ref _rowNames, _rows);
            }

            for (int i = 0; i < _rows; i++)
            {
                _rowNames[i] = EditorGUILayout.TextField($"Row {i} Name", _rowNames[i]);
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Process Slicing & Create Anims"))
            {
                if (_targetTexture == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please select a texture.", "OK");
                    return;
                }
                ProcessSpriteSheet();
            }
        }

        private void ProcessSpriteSheet()
        {
            string path = AssetDatabase.GetAssetPath(_targetTexture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            // 1. 텍스처 설정
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100; // 적절히 조정
            importer.compressionQuality = 100; // High Quality
            
            // 2. 슬라이싱 메타데이터 생성
            var metaData = new List<SpriteMetaData>();
            int spriteWidth = _targetTexture.width / _cols;
            int spriteHeight = _targetTexture.height / _rows;

            for (int r = 0; r < _rows; r++) // Top to Bottom (텍스처 좌표는 Bottom-Left 기준이라 역순 주의)
            {
                // Unity 텍스처 좌표계: (0,0)은 좌하단. 
                // 위에서부터 읽으려면 y는 (_rows - 1 - r) * height
                int rowIndex = _rows - 1 - r; 
                
                for (int c = 0; c < _cols; c++)
                {
                    var rect = new Rect(c * spriteWidth, rowIndex * spriteHeight, spriteWidth, spriteHeight);
                    var spriteName = $"{_targetTexture.name}_{_rowNames[r]}_{c}";
                    
                    metaData.Add(new SpriteMetaData
                    {
                        pivot = new Vector2(0.5f, 0.2f), // 발 아래쪽 피벗
                        alignment = 9, // Custom
                        name = spriteName,
                        rect = rect
                    });
                }
            }

            importer.spritesheet = metaData.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // 3. 스프라이트 로드
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> sprites = new List<Sprite>();
            foreach (var asset in assets)
            {
                if (asset is Sprite s) sprites.Add(s);
            }

            // 4. 애니메이션 생성
            string dir = Path.GetDirectoryName(path);
            string animFolder = Path.Combine(dir, "Animations");
            if (!Directory.Exists(animFolder)) Directory.CreateDirectory(animFolder);

            // AnimatorController 생성
            string controllerPath = Path.Combine(animFolder, $"{_targetTexture.name}_Controller.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // 이름 순 정렬 주의 (위에서 생성한 이름 규칙: Name_RowName_Index)
            for (int r = 0; r < _rows; r++)
            {
                string rowName = _rowNames[r];
                if (string.IsNullOrEmpty(rowName)) continue;

                // 해당 행의 스프라이트만 필터링
                var rowSprites = sprites.FindAll(s => s.name.Contains($"_{rowName}_"));
                rowSprites.Sort((a, b) => NaturalCompare(a.name, b.name));

                if (rowSprites.Count == 0) continue;

                // Animation Clip 생성
                AnimationClip clip = new AnimationClip();
                clip.frameRate = _samples;
                
                // 스프라이트 바인딩
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    type = typeof(SpriteRenderer),
                    path = "",
                    propertyName = "m_Sprite"
                };

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[rowSprites.Count];
                for (int i = 0; i < rowSprites.Count; i++)
                {
                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = i / (float)_samples,
                        value = rowSprites[i]
                    };
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
                
                // 루프 설정
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // 저장
                string clipPath = Path.Combine(animFolder, $"{_targetTexture.name}_{rowName}.anim");
                AssetDatabase.CreateAsset(clip, clipPath);

                // Controller에 추가
                controller.AddMotion(clip);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Created {_rows} animations in {animFolder}", "Cool");
        }

        // 이름 정렬용 (숫자 비교)
        private int NaturalCompare(string a, string b)
        {
            return EditorUtility.NaturalCompare(a, b);
        }
    }
}
