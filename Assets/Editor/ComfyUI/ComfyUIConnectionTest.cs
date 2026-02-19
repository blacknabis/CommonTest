using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CatSudoku.Editor
{
    [InitializeOnLoad]
    public class ComfyUIConnectionTest
    {
        private static readonly string TestLogPath = "Temp/ComfyUITestResult.txt";

        static ComfyUIConnectionTest()
        {
            if (!SessionState.GetBool("ComfyUITestRun", false))
            {
                SessionState.SetBool("ComfyUITestRun", true);
                RunTest();
            }
        }

        private static async void RunTest()
        {
            Debug.Log("Starting ComfyUI Connection Test...");
            string result = "FAILURE";
            string details = "";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    // Just check if we can reach the server
                    var response = await client.GetAsync("http://127.0.0.1:8188/object_info");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        result = "SUCCESS";
                        details = "Connected to ComfyUI successfully.";
                    }
                    else
                    {
                        details = $"Status Code: {response.StatusCode}";
                    }
                }
            }
            catch (Exception e)
            {
                details = e.Message;
            }

            string logContent = $"{DateTime.Now}\nResult: {result}\nDetails: {details}";
            File.WriteAllText(TestLogPath, logContent);
            Debug.Log($"ComfyUI Verification Complete: {result}");
        }
    }
}
