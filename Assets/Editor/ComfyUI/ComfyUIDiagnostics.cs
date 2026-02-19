using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CatSudoku.Editor
{
    public class ComfyUIDiagnostics
    {
        [MenuItem("Tools/Check Available Models")]
        public static async void CheckModels()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("http://127.0.0.1:8188/object_info/CheckpointLoaderSimple");
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.Log($"ComfyUI Models:\n{content}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to check models: {e.Message}");
                }
            }
        }
    }
}
