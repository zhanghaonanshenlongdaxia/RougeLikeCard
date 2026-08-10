using System.Collections.Generic;
using System.Linq;
using CardGame;
using NueGames.NueDeck.Scripts.Data.Characters;
using UnityEditor;
using UnityEngine;

namespace CardGame.EditorTools
{
    /// <summary>
    /// 敌人测试模式窗口：Tools > 敌人测试模式
    /// 可以从所有敌人中选择要强制遇到的敌人（按顺序），
    /// 进入战斗后会按此列表生成敌人。
    /// </summary>
    public class EnemyTestModeWindow : EditorWindow
    {
        private Vector2 _enemyListScroll;
        private Vector2 _selectedScroll;
        private string _searchFilter = "";
        private List<EnemyCharacterData> _allEnemies;
        private string[] _regionNames = { "全部", "山野荒原", "幽冥秘境", "万蛊沼泽", "天魔裂隙" };
        private int _selectedRegion = 0;

        [MenuItem("Tools/敌人测试模式")]
        public static void Open()
        {
            var window = GetWindow<EnemyTestModeWindow>("敌人测试模式");
            window.minSize = new Vector2(600, 500);
        }

        private void OnEnable()
        {
            LoadAllEnemies();
        }

        private void LoadAllEnemies()
        {
            _allEnemies = new List<EnemyCharacterData>();
            var guids = AssetDatabase.FindAssets("t:EnemyCharacterData", new[] { "Assets/NueGames/NueDeck/Data/Enemies" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyCharacterData>(path);
                if (enemy != null) _allEnemies.Add(enemy);
            }
            // 按名称排序
            _allEnemies = _allEnemies.OrderBy(e => e.name).ToList();
        }

        private void OnGUI()
        {
            // 顶部状态栏
            EditorGUILayout.BeginHorizontal("box");
            var oldColor = GUI.color;
            GUI.color = EnemyTestMode.Enabled ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
            EnemyTestMode.Enabled = EditorGUILayout.ToggleLeft("启用测试模式", EnemyTestMode.Enabled, GUILayout.Width(150));
            GUI.color = oldColor;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("清空选择", GUILayout.Width(80)))
            {
                EnemyTestMode.ForcedEnemies.Clear();
            }

            if (GUILayout.Button("刷新列表", GUILayout.Width(80)))
            {
                LoadAllEnemies();
            }
            EditorGUILayout.EndHorizontal();

            if (EnemyTestMode.Enabled)
            {
                EditorGUILayout.HelpBox(
                    "✅ 测试模式已开启！进入战斗时将按下方列表生成敌人。\n" +
                    "关闭后恢复随机抽取。", MessageType.Info);
            }

            // 筛选栏
            EditorGUILayout.BeginHorizontal("box");
            _selectedRegion = EditorGUILayout.Popup("区域:", _selectedRegion, _regionNames, GUILayout.Width(200));
            _searchFilter = EditorGUILayout.TextField("搜索:", _searchFilter, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            // 已选敌人列表
            EditorGUILayout.LabelField($"已选敌人 ({EnemyTestMode.ForcedEnemies.Count}/5):", EditorStyles.boldLabel);

            if (EnemyTestMode.ForcedEnemies.Count > 0)
            {
                _selectedScroll = EditorGUILayout.BeginScrollView(_selectedScroll, GUILayout.Height(100));
                for (int i = EnemyTestMode.ForcedEnemies.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField($"位置{i + 1}:", GUILayout.Width(50));
                    var enemy = EnemyTestMode.ForcedEnemies[i];
                    EditorGUILayout.ObjectField(enemy, typeof(EnemyCharacterData), false);

                    // 上移/下移按钮
                    if (i > 0 && GUILayout.Button("↑", GUILayout.Width(25)))
                    {
                        var temp = EnemyTestMode.ForcedEnemies[i - 1];
                        EnemyTestMode.ForcedEnemies[i - 1] = EnemyTestMode.ForcedEnemies[i];
                        EnemyTestMode.ForcedEnemies[i] = temp;
                    }
                    if (i < EnemyTestMode.ForcedEnemies.Count - 1 && GUILayout.Button("↓", GUILayout.Width(25)))
                    {
                        var temp = EnemyTestMode.ForcedEnemies[i + 1];
                        EnemyTestMode.ForcedEnemies[i + 1] = EnemyTestMode.ForcedEnemies[i];
                        EnemyTestMode.ForcedEnemies[i] = temp;
                    }

                    if (GUILayout.Button("✕", GUILayout.Width(25)))
                    {
                        EnemyTestMode.ForcedEnemies.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("（空 — 从下方列表点击 + 添加敌人）", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Separator();

            // 全部敌人列表
            EditorGUILayout.LabelField("所有敌人:", EditorStyles.boldLabel);

            _enemyListScroll = EditorGUILayout.BeginScrollView(_enemyListScroll);

            // 筛选敌人
            string regionFolder = _selectedRegion switch
            {
                1 => "Region1_ShanYe",
                2 => "Region2_YouMing",
                3 => "Region3_WanGu",
                4 => "Region4_TianMo",
                _ => null
            };

            foreach (var enemy in _allEnemies)
            {
                var path = AssetDatabase.GetAssetPath(enemy);

                // 区域筛选
                if (regionFolder != null && !path.Contains(regionFolder)) continue;

                // 名称搜索
                if (!string.IsNullOrEmpty(_searchFilter) && !enemy.name.Contains(_searchFilter) &&
                    !(enemy.CharacterName?.Contains(_searchFilter) ?? false)) continue;

                EditorGUILayout.BeginHorizontal("box");

                // 敌人信息
                var tierStr = enemy.EnemyTier.ToString();
                EditorGUILayout.LabelField($"[{tierStr}]", GUILayout.Width(50));
                EditorGUILayout.ObjectField(enemy, typeof(EnemyCharacterData), false);

                // 添加按钮
                var alreadySelected = EnemyTestMode.ForcedEnemies.Contains(enemy);
                GUI.enabled = !alreadySelected && EnemyTestMode.ForcedEnemies.Count < 5;
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    EnemyTestMode.ForcedEnemies.Add(enemy);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
