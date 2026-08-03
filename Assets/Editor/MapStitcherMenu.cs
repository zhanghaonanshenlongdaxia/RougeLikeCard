using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CardProject.Editor
{
    public static class MapStitcherMenu
    {
        private const string ServerUrl = "http://127.0.0.1:7860";

        [MenuItem("Tools/地图拼接工具 %&M")]
        public static async void OpenMapStitcher()
        {
            if (!await IsServerRunning())
            {
                StartServer();
                await Task.Delay(2000);
            }
            Application.OpenURL(ServerUrl);
        }

        private static async Task<bool> IsServerRunning()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync(ServerUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static void StartServer()
        {
            string scriptPath = Path.Combine(Application.dataPath, "..", "Tools", "ImageOutpaintTool", "app.py");
            string pythonExe = "python";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath),
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            Process.Start(psi);
            UnityEngine.Debug.Log("[MapStitcher] Starting local server: " + scriptPath);
        }
    }
}
