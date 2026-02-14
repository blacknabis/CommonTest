using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Kingdom.Editor
{
    /// <summary>
    /// 모든 씬의 카메라 배경색을 어두운 색으로 변경하는 일회용 도구.
    /// 실행 후 삭제할 것.
    /// </summary>
    public static class CameraBackgroundColorFixer
    {
        [MenuItem("Tools/Kingdom/Fix Camera Background Color (All Scenes)")]
        public static void Fix()
        {
            // 현재 열린 씬 저장
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            var darkColor = new Color(0.05f, 0.05f, 0.05f, 1f); // 거의 검정
            string[] scenePaths = {
                "Assets/Scenes/InitScene.unity",
                "Assets/Scenes/TitleScene.unity",
                "Assets/Scenes/WorldMapScene.unity",
                "Assets/Scenes/GameScene.unity"
            };

            int fixed_count = 0;
            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    Undo.RecordObject(cam, "Fix Camera BG Color");
                    cam.backgroundColor = darkColor;
                    EditorUtility.SetDirty(cam);
                    fixed_count++;
                    Debug.Log($"[CameraBGFix] {scene.name} → {cam.name} 배경색 변경 완료");
                }
                EditorSceneManager.SaveScene(scene);
            }

            // 원래 씬으로 복귀
            EditorSceneManager.OpenScene(scenePaths[0], OpenSceneMode.Single);

            EditorUtility.DisplayDialog("완료",
                $"카메라 {fixed_count}개의 배경색을 어둡게 변경했습니다.", "확인");
        }
    }
}
