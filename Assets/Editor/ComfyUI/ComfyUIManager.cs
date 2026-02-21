using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace CatSudoku.Editor
{
    public class ComfyUIManager
    {
        private static readonly string BaseUrl = "http://127.0.0.1:8188";
        private static HttpClient _client;
        private static HttpClient client
        {
            get
            {
                if (_client == null) _client = new HttpClient();
                return _client;
            }
        }

        public static async Task<string> QueuePrompt(Dictionary<string, object> workflow)
        {
            try
            {
                var json = JsonConvert.SerializeObject(new { prompt = workflow });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{BaseUrl}/prompt", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<QueuePromptResponse>(responseString);
                    return data.prompt_id;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Failed to queue prompt: {response.StatusCode} {response.ReasonPhrase}\nResponse: {errorBody}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ComfyUI Connection Error: {e.Message}");
                return null;
            }
        }

        public static async Task<byte[]> GetImage(string filename, string subfolder, string type)
        {
            try
            {
                var query = $"filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={type}";
                var response = await client.GetAsync($"{BaseUrl}/view?{query}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get image: {e.Message}");
                return null;
            }
        }

        public static async Task<byte[]> GetAudio(string filename, string subfolder, string type)
        {
            try
            {
                var query = $"filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={type}";
                var response = await client.GetAsync($"{BaseUrl}/view?{query}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get audio: {e.Message}");
                return null;
            }
        }

        public static async Task<HistoryData> GetHistory(string promptId)
        {
            try 
            {
                var response = await client.GetAsync($"{BaseUrl}/history/{promptId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    // ComfyUI returns a dictionary where key is prompt_id
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, HistoryData>>(json);
                    if (dict != null && dict.ContainsKey(promptId))
                    {
                        return dict[promptId];
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                 Debug.LogError($"Failed to get history: {e.Message}");
                 return null;
            }
        }
        public static async Task<List<string>> GetCheckpoints()
        {
            try
            {
                var response = await client.GetAsync($"{BaseUrl}/object_info/CheckpointLoaderSimple");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
                    
                    if (data.ContainsKey("CheckpointLoaderSimple"))
                    {
                        var input = data["CheckpointLoaderSimple"]["input"]["required"];
                        var ckptNames = input["ckpt_name"][0].ToObject<List<string>>();
                        return ckptNames;
                    }
                }
                return new List<string>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get checkpoints: {e.Message}");
                return new List<string>();
            }
        }

        public static async Task<bool> UploadImage(byte[] imageData, string filename, string overwrite = "true")
        {
            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    var imageContent = new ByteArrayContent(imageData);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    form.Add(imageContent, "image", filename);
                    
                    var overwriteContent = new StringContent(overwrite);
                    form.Add(overwriteContent, "overwrite");

                    var response = await client.PostAsync($"{BaseUrl}/upload/image", form);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"Failed to upload image: {response.StatusCode} {error}");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Upload Image Error: {e.Message}");
                return false;
            }
        }

        public static async Task<bool> HasNodeType(string classType)
        {
            if (string.IsNullOrWhiteSpace(classType))
            {
                return false;
            }

            try
            {
                string encoded = Uri.EscapeDataString(classType);
                var directResponse = await client.GetAsync($"{BaseUrl}/object_info/{encoded}");
                if (directResponse.IsSuccessStatusCode)
                {
                    return true;
                }

                var allResponse = await client.GetAsync($"{BaseUrl}/object_info");
                if (!allResponse.IsSuccessStatusCode)
                {
                    return false;
                }

                var json = await allResponse.Content.ReadAsStringAsync();
                var objectInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                return objectInfo != null && objectInfo.ContainsKey(classType);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to query node type '{classType}': {e.Message}");
                return false;
            }
        }
    }


}
