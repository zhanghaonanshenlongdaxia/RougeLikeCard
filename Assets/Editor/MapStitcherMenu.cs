using System;
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
        private const int Port = 7860;

        [MenuItem("Tools/地图拼接工具 %&M")]
        public static async void OpenMapStitcher()
        {
            StopExistingServer();
            StartServer();
            await Task.Delay(2000);
            Application.OpenURL(ServerUrl);
        }

        private static void StopExistingServer()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c for /f \"tokens=5\" %a in ('netstat -ano ^| findstr :{Port} ^| findstr LISTENING') do taskkill /F /PID %a >nul 2>&1",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var process = Process.Start(psi);
                process?.WaitForExit(3000);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[MapStitcher] Failed to stop existing server: " + ex.Message);
            }
        }

        private static void StartServer()
        {
            string scriptPath = Path.Combine(Application.dataPath, "..", "Tools", "ImageOutpaintTool", "app.py");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "python",
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
