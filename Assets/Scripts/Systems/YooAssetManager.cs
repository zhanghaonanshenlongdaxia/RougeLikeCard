using UnityEngine;
using YooAsset;

namespace CardGame
{
    /// <summary>
    /// YooAsset资源热更管理器。
    /// 负责初始化资源系统、检查更新、加载热更资源。
    /// 本地测试模式：EditorSimulateMode，从项目直接加载，无需打包AB。
    /// </summary>
    public static class YooAssetManager
    {
        private const string PackageName = "DefaultPackage";
        private static bool _initialized = false;
        private static ResourcePackage _package;

        public static bool IsInitialized => _initialized;

        /// <summary>
        /// 初始化YooAsset资源系统
        /// </summary>
        public static async void Init()
        {
            if (_initialized) return;

            // 1. 初始化YooAsset
            YooAssets.Initialize();

            // 2. 创建资源包
            _package = YooAssets.CreatePackage(PackageName);

            // 3. 编辑器模拟模式（本地测试）
            var options = new EditorSimulateModeOptions();
            string packageRoot = System.IO.Path.Combine(Application.dataPath, "YooAsset", "DefaultPackage").Replace('\\', '/');
            options.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            var initOp = _package.InitializePackageAsync(options);

            // 等待完成
            while (!initOp.IsDone)
                await System.Threading.Tasks.Task.Yield();

            if (_package.InitializeStatus == EOperationStatus.Succeeded)
            {
                _initialized = true;
                Debug.Log("[YooAsset] 初始化成功 (EditorSimulateMode)");
            }
            else
            {
                Debug.LogError($"[YooAsset] 初始化失败: {_package.InitializeStatus}");
            }
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public static async System.Threading.Tasks.Task<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            if (!_initialized || _package == null)
            {
                Debug.LogWarning("[YooAsset] YooAsset未初始化，使用AssetDatabase加载");
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
                return null;
#endif
            }
            var handle = _package.LoadAssetAsync<T>(assetPath);
            while (!handle.IsDone)
                await System.Threading.Tasks.Task.Yield();
            return handle.GetAssetObject<T>();
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            if (!_initialized || _package == null)
            {
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
                return null;
#endif
            }
            var handle = _package.LoadAssetAsync<T>(assetPath);
            handle.WaitForAsyncComplete();
            return handle.GetAssetObject<T>();
        }
    }
}
