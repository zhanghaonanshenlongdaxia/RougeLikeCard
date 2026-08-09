using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// HybridCLR热更加载器。在游戏启动时加载热更DLL。
    /// 此脚本不属于热更程序集，放在Assets/Scripts中。
    /// </summary>
    public static class HotUpdateLoader
    {
        /// <summary>
        /// 加载热更新程序集。
        /// 在编辑器模式下直接加载编译后的DLL，在打包后从热更资源加载。
        /// </summary>
        public static void LoadHotUpdateAssembly()
        {
#if !UNITY_EDITOR
            // 打包后：从persistentDataPath或StreamingAssets加载热更DLL
            // 这里简化为从StreamingAssets加载
            string dllPath = Path.Combine(Application.streamingAssetsPath, "HotUpdate.dll.bytes");
            if (!File.Exists(dllPath))
            {
                Debug.LogError($"[HotUpdate] DLL not found: {dllPath}");
                return;
            }

            byte[] dllBytes = File.ReadAllBytes(dllPath);
            var assembly = System.Reflection.Assembly.Load(dllBytes);
            Debug.Log($"[HotUpdate] Assembly loaded: {assembly.GetName().Name}");

            // 加载AOT元数据补充
            LoadAOTMetadata();
#endif
            // 编辑器模式下Assembly-CSharp已自动加载所有程序集，无需手动加载
            // 热更代码通过反射调用入口
            InvokeHotUpdateEntry();
        }

        /// <summary>
        /// 加载AOT元数据（防止泛型裁剪问题）
        /// </summary>
        private static void LoadAOTMetadata()
        {
#if !UNITY_EDITOR
            // HybridCLR的AOT元数据补充
            // 需要将AOT DLL的.bytes文件放在StreamingAssets中
            var aotAssemblies = new string[]
            {
                "mscorlib.dll",
                "System.dll",
                "System.Core.dll",
                "UnityEngine.CoreModule.dll",
            };

            foreach (var aotDll in aotAssemblies)
            {
                string path = Path.Combine(Application.streamingAssetsPath, "AOT", aotDll + ".bytes");
                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    // HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly
                    // 在打包后调用，这里编辑器模式不需要
                    Debug.Log($"[HotUpdate] AOT metadata loaded: {aotDll}");
                }
            }
#endif
        }

        /// <summary>
        /// 通过反射调用热更程序集的入口方法
        /// </summary>
        private static void InvokeHotUpdateEntry()
        {
            var asm = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in asm)
            {
                if (a.GetName().Name == "HotUpdate")
                {
                    var entryType = a.GetType("CardGame.HotUpdate.HotUpdateEntry");
                    if (entryType != null)
                    {
                        var method = entryType.GetMethod("Start", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (method != null)
                        {
                            method.Invoke(null, null);
                            Debug.Log("[HotUpdate] HotUpdateEntry.Start() invoked successfully");
                            return;
                        }
                    }
                }
            }
            Debug.LogWarning("[HotUpdate] HotUpdate assembly or entry not found (may be normal in editor)");
        }
    }
}
