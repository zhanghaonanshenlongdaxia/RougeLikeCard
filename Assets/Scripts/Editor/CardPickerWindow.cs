using System;
using System.Collections.Generic;
using System.Linq;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Enums;
using UnityEditor;
using UnityEngine;

namespace CardGame.Editor
{
    /// <summary>
    /// 卡牌选择窗口 — 网格展示所有卡牌，支持按道/名称筛选，多选后确定返回卡牌ID列表
    /// </summary>
    public class CardPickerWindow : EditorWindow
    {
        private static CardPickerWindow _current;
        private static Action<List<string>> _callback;
        private static List<string> _initialSelection;

        private List<CardData> _allCards;
        private List<CardData> _filteredCards;
        private HashSet<string> _selectedIds;
        private string _searchText = "";
        private PathType _filterPath = PathType.None;
        private RarityType _filterRarity = RarityType.Common;
        private bool _filterByRarity = false;
        private Vector2 _scrollPos;

        private const float CardItemWidth = 160f;
        private const float CardItemHeight = 70f;

        public static void Open(Action<List<string>> onConfirm, List<string> initialSelection = null)
        {
            _callback = onConfirm;
            _initialSelection = initialSelection ?? new List<string>();
            _current = GetWindow<CardPickerWindow>("选择卡牌");
            _current.minSize = new Vector2(600, 400);
            _current.Show();
        }

        private void OnEnable()
        {
            LoadAllCards();
            _selectedIds = new HashSet<string>(_initialSelection);
            ApplyFilter();
        }

        private void LoadAllCards()
        {
            _allCards = new List<CardData>();
            var guids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/NueGames/NueDeck/Data/Cards" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null) _allCards.Add(card);
            }
            _allCards.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));
        }

        private void ApplyFilter()
        {
            _filteredCards = _allCards.Where(c =>
            {
                if (_filterPath != PathType.None && c.PathType != _filterPath) return false;
                if (_filterByRarity && c.Rarity != _filterRarity) return false;
                if (!string.IsNullOrEmpty(_searchText) &&
                    !c.CardName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) &&
                    !c.Id.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }).ToList();
        }

        private void OnGUI()
        {
            DrawFilterBar();

            EditorGUILayout.Space(3);

            // 已选数量 + 确定按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"已选: {_selectedIds.Count} 张", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("确定", GUILayout.Width(80), GUILayout.Height(30)))
            {
                _callback?.Invoke(_selectedIds.ToList());
                Close();
            }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("取消", GUILayout.Width(80), GUILayout.Height(30)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // 卡牌网格
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            float width = position.width - 30;
            int columns = Mathf.Max(1, Mathf.FloorToInt(width / CardItemWidth));
            int count = _filteredCards.Count;

            for (int i = 0; i < count; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < columns && i + j < count; j++)
                {
                    var card = _filteredCards[i + j];
                    DrawCardItem(card);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal("box");

            // 搜索框
            _searchText = EditorGUILayout.TextField("搜索:", _searchText, GUILayout.Width(200));

            // 道筛选
            EditorGUILayout.LabelField("道:", GUILayout.Width(25));
            var newPath = (PathType)EditorGUILayout.EnumPopup(_filterPath, GUILayout.Width(80));
            if (newPath != _filterPath) { _filterPath = newPath; ApplyFilter(); }

            // 品质筛选
            _filterByRarity = EditorGUILayout.Toggle("品质:", _filterByRarity, GUILayout.Width(60));
            if (_filterByRarity)
            {
                var newRarity = (RarityType)EditorGUILayout.EnumPopup(_filterRarity, GUILayout.Width(80));
                if (newRarity != _filterRarity) { _filterRarity = newRarity; ApplyFilter(); }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("筛选", GUILayout.Width(60)))
            {
                ApplyFilter();
            }

            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                foreach (var c in _filteredCards) _selectedIds.Add(c.Id);
            }

            if (GUILayout.Button("清空", GUILayout.Width(60)))
            {
                _selectedIds.Clear();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCardItem(CardData card)
        {
            bool isSelected = _selectedIds.Contains(card.Id);

            Color bg = isSelected ? new Color(0.2f, 0.5f, 0.2f, 0.9f) : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            GUI.backgroundColor = bg;

            EditorGUILayout.BeginVertical("box", GUILayout.Width(CardItemWidth), GUILayout.Height(CardItemHeight));

            // 卡牌名
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = isSelected ? Color.yellow : Color.white;
            EditorGUILayout.LabelField(card.CardName, style);

            // ID + 费用 + 道
            EditorGUILayout.LabelField($"{card.Id} | {card.ManaCost}费 | {PathText(card.PathType)}", new GUIStyle(EditorStyles.miniLabel));

            // 品质色条
            var rarityColor = GetRarityColor(card.Rarity);
            var rect = GUILayoutUtility.GetRect(CardItemWidth - 16, 3);
            EditorGUI.DrawRect(rect, rarityColor);

            // 点击切换选中
            if (GUILayout.Button(isSelected ? "✓ 已选" : "选择", GUILayout.Height(20)))
            {
                if (isSelected) _selectedIds.Remove(card.Id);
                else _selectedIds.Add(card.Id);
            }

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }

        private static string PathText(PathType p) => p switch
        {
            PathType.Sword => "剑道", PathType.Body => "体道", PathType.Spirit => "灵道", _ => "通用"
        };

        private static Color GetRarityColor(RarityType r) => r switch
        {
            RarityType.Common => new Color(0.7f, 0.7f, 0.7f),
            RarityType.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            RarityType.Rare => new Color(0.2f, 0.4f, 0.9f),
            RarityType.Legendary => new Color(0.9f, 0.7f, 0.1f),
            _ => Color.gray
        };
    }
}
