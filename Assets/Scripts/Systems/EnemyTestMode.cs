using System.Collections.Generic;
using System.IO;
using NueGames.NueDeck.Scripts.Data.Characters;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 测试模式：可强制指定战斗中出现的敌人（按顺序）。
    /// 通过 Tools > 敌人测试模式 打开编辑器窗口设置。
    /// 设置后进入战斗时，会按此列表生成敌人，而非随机抽取。
    /// 设置持久化到磁盘，domain reload 后不丢失。
    /// </summary>
    public static class EnemyTestMode
    {
        private const string ConfigPath = "Assets/StreamingAssets/enemy_test_mode.json";

        private static bool _enabled = false;
        private static List<EnemyCharacterData> _forcedEnemies = new List<EnemyCharacterData>();
        private static bool _loaded = false;

        /// <summary>是否启用测试模式</summary>
        public static bool Enabled
        {
            get { EnsureLoaded(); return _enabled; }
            set { _enabled = value; Save(); }
        }

        /// <summary>强制出现的敌人列表（按顺序对应位置1/2/3...）</summary>
        public static List<EnemyCharacterData> ForcedEnemies
        {
            get { EnsureLoaded(); return _forcedEnemies; }
            set { _forcedEnemies = value; Save(); }
        }

        /// <summary>从磁盘加载配置（domain reload 后恢复）</summary>
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var data = JsonUtility.FromJson<TestModeData>(json);
                    _enabled = data.enabled;

                    _forcedEnemies = new List<EnemyCharacterData>();
#if UNITY_EDITOR
                    foreach (var guid in data.enemyGuids)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        if (string.IsNullOrEmpty(path)) continue;
                        var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyCharacterData>(path);
                        if (enemy != null) _forcedEnemies.Add(enemy);
                    }
#endif
                    Debug.Log($"[EnemyTestMode] Loaded from disk: enabled={_enabled}, count={_forcedEnemies.Count}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnemyTestMode] Failed to load config: {e.Message}");
            }
        }

        /// <summary>保存配置到磁盘</summary>
        public static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var data = new TestModeData { enabled = _enabled };

#if UNITY_EDITOR
                data.enemyGuids = new List<string>();
                foreach (var enemy in _forcedEnemies)
                {
                    if (enemy == null) continue;
                    var path = UnityEditor.AssetDatabase.GetAssetPath(enemy);
                    var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                    if (!string.IsNullOrEmpty(guid)) data.enemyGuids.Add(guid);
                }
#endif
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(ConfigPath, json);
                Debug.Log($"[EnemyTestMode] Saved to disk: enabled={_enabled}, count={data.enemyGuids?.Count ?? 0}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnemyTestMode] Failed to save config: {e.Message}");
            }
        }

        [System.Serializable]
        public class TestModeData
        {
            public bool enabled = false;
            public List<string> enemyGuids = new List<string>();
        }
    }
}
