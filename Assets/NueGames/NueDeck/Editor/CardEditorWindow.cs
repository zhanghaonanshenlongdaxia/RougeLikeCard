using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NueGames.NueDeck.Editor
{
    public class CardEditorWindow : ExtendedEditorWindow
    {
#if UNITY_EDITOR
        

        private static CardEditorWindow CurrentWindow { get; set; }
        private SerializedObject _serializedObject;

        private const string CardDataDefaultPath = "Assets/NueGames/NueDeck/Data/Cards/";
       
        #region Cache Card Data
        private static CardData CachedCardData { get; set; }
        private List<CardData> AllCardDataList { get; set; }
        private HashSet<string> _activeMethodCardIds = new HashSet<string>();
        private CardData SelectedCardData { get; set; }
        private string CardId { get; set; }
        private string CardName{ get; set; }
        private int ManaCost{ get; set; }
        private Sprite CardSprite{ get; set; }
        private bool UsableWithoutTarget{ get; set; }
        private bool ExhaustAfterPlay{ get; set; }
        private List<CardActionData> CardActionDataList{ get; set; }
        private List<CardDescriptionData> CardDescriptionDataList{ get; set; }
        private List<StatusType> SpecialKeywordsList{ get; set; }
        private AudioActionType AudioType{ get; set; }
        
        private RarityType CardRarity { get; set; }

        private void CacheCardData()
        {
            CardId = SelectedCardData.Id;
            CardName = SelectedCardData.CardName;
            ManaCost = SelectedCardData.ManaCost;
            CardSprite = SelectedCardData.CardSprite;
            UsableWithoutTarget = SelectedCardData.UsableWithoutTarget;
            ExhaustAfterPlay = SelectedCardData.ExhaustAfterPlay;
            CardActionDataList = SelectedCardData.CardActionDataList.Count>0 ? new List<CardActionData>(SelectedCardData.CardActionDataList) : new List<CardActionData>();
            CardDescriptionDataList = SelectedCardData.CardDescriptionDataList.Count>0 ? new List<CardDescriptionData>(SelectedCardData.CardDescriptionDataList) : new List<CardDescriptionData>();
            SpecialKeywordsList = SelectedCardData.KeywordsList.Count>0 ? new List<StatusType>(SelectedCardData.KeywordsList) : new List<StatusType>();
            AudioType = SelectedCardData.AudioType;
            CardRarity = SelectedCardData.Rarity;
        }
        
        private void ClearCachedCardData()
        {
            CardId = String.Empty;
            CardName = String.Empty;
            ManaCost = 0;
            CardSprite = null;
            UsableWithoutTarget = false;
            ExhaustAfterPlay = false;
            CardActionDataList?.Clear();
            CardDescriptionDataList?.Clear();
            SpecialKeywordsList?.Clear();
            AudioType = AudioActionType.Attack;
            CardRarity = RarityType.Common;
        }
        #endregion
        
        #region Setup
        [MenuItem("Tools/NueDeck/Card Editor")]
        public static void OpenCardEditor() =>  CurrentWindow = GetWindow<CardEditorWindow>("Card Editor");
        public static void OpenCardEditor(CardData targetData)
        {
            CachedCardData = targetData;
            OpenCardEditor();
        } 
        
        private void OnEnable()
        {
            AllCardDataList?.Clear();
            AllCardDataList = ListExtentions.GetAllInstances<CardData>().ToList();
            CacheActiveMethodCardIds();
            
            if (CachedCardData)
            {
                SelectedCardData = CachedCardData;
                _serializedObject = new SerializedObject(SelectedCardData);
                CacheCardData();
            }
            
            Selection.selectionChanged += Repaint;
        }

        /// <summary>缓存所有功法中RewardType=Card的卡牌ID，用于列表排序和高亮</summary>
        private void CacheActiveMethodCardIds()
        {
            _activeMethodCardIds.Clear();
            var methodGuids = AssetDatabase.FindAssets("t:CultivationMethodData");
            foreach (var guid in methodGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var method = AssetDatabase.LoadAssetAtPath<NueGames.NueDeck.Scripts.Data.Cultivation.CultivationMethodData>(path);
                if (method?.Nodes == null) continue;
                foreach (var node in method.Nodes)
                {
                    if (node.RewardType == NueGames.NueDeck.Scripts.Enums.NodeRewardType.Card && node.RewardIds != null)
                        foreach (var id in node.RewardIds)
                            _activeMethodCardIds.Add(id);
                }
            }
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
            CachedCardData = null;
            SelectedCardData = null;
        }
        #endregion

        #region Process
        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            DrawAllCardButtons();
            EditorGUILayout.Space();
            DrawSelectedCard();
            
            EditorGUILayout.EndHorizontal();
        }
        #endregion
        
        #region Layout Methods
        private Vector2 _allCardButtonsScrollPos;
        private void DrawAllCardButtons()
        {
            _allCardButtonsScrollPos = EditorGUILayout.BeginScrollView(_allCardButtonsScrollPos, GUILayout.Width(200), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical("box", GUILayout.Width(200), GUILayout.ExpandHeight(true));
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("卡牌列表",EditorStyles.boldLabel,GUILayout.Width(70),GUILayout.Height(20));
            
            GUILayout.FlexibleSpace();
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.blue;
            if (GUILayout.Button("刷新",GUILayout.Width(60),GUILayout.Height(20)))
                RefreshCardData();
            GUI.backgroundColor = oldColor;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            // 排序：当前功法卡牌排最前面
            var sortedList = AllCardDataList
                .OrderByDescending(c => _activeMethodCardIds.Contains(c.Id))
                .ThenBy(c => c.CardName)
                .ToList();

            var oldBtnColor = GUI.backgroundColor;
            foreach (var data in sortedList)
            {
                bool isMethodCard = _activeMethodCardIds.Contains(data.Id);
                GUI.backgroundColor = isMethodCard ? new Color(0.9f, 0.7f, 0.2f) : oldBtnColor;
                if (GUILayout.Button((isMethodCard ? "★ " : "") + data.CardName, GUILayout.MaxWidth(200)))
                {
                    SelectedCardData = data;
                    _serializedObject = new SerializedObject(SelectedCardData);
                    CacheCardData();
                    GUI.FocusControl(null);
                }
            }
            GUI.backgroundColor = oldBtnColor;

            if (GUILayout.Button("+ 新建卡牌",GUILayout.MaxWidth(200)))
            {
                CreateNewCard();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void CreateNewCard()
        {
            var clone = CreateInstance<CardData>();
            var str = new StringBuilder();
            var count = AllCardDataList.Count;

            str.Append(count + 1).Append("_").Append("new_card_name");
            clone.EditId(str.ToString());
            clone.EditCardName(str.ToString());
            clone.EditCardActionDataList(new List<CardActionData>());
            clone.EditCardDescriptionDataList(new List<CardDescriptionData>());
            clone.EditSpecialKeywordsList(new List<StatusType>());
            clone.EditRarity(RarityType.Common);
            var path = str.Insert(0, CardDataDefaultPath).Append(".asset").ToString();
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(clone, uniquePath);
            AssetDatabase.SaveAssets();
            RefreshCardData();
            SelectedCardData = AllCardDataList.Find(x => x.Id == clone.Id);
            CacheCardData();
        }

        private void DrawSelectedCard()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            if (!SelectedCardData)
            {
                EditorGUILayout.LabelField("请选择卡牌");
                EditorGUILayout.EndVertical();
                return;
            }
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
           
            
            ChangeGeneralSettings();
           
            ChangeCardActionDataList();
            ChangeCardDescriptionDataList();
            ChangeSpecialKeywords();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save",GUILayout.Width(100),GUILayout.Height(30)))
                SaveCardData();
            
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Card Data Methods

        private void ChangeId()
        {
            CardId = EditorGUILayout.TextField("卡牌ID:", CardId);
        }
        private void ChangeCardName()
        {
            CardName = EditorGUILayout.TextField("卡牌名称:", CardName);
        }
        private void ChangeManaCost()
        {
            ManaCost = EditorGUILayout.IntField("灵力消耗:", ManaCost);
        }
        
        private void ChangeRarity()
        { 
            // 显示中文品质名
            var rarityNames = new[] { "黄", "玄", "地", "天" };
            var rarityValues = new[] { RarityType.Common, RarityType.Uncommon, RarityType.Rare, RarityType.Legendary };
            var currentIndex = System.Array.IndexOf(rarityValues, CardRarity);
            if (currentIndex < 0) currentIndex = 0;
            var newIndex = EditorGUILayout.Popup("品质:", currentIndex, rarityNames, GUILayout.Width(250));
            CardRarity = rarityValues[newIndex];
        }
        private void ChangeCardSprite()
        {
            EditorGUILayout.BeginHorizontal();
            CardSprite = (Sprite)EditorGUILayout.ObjectField("卡牌图片:", CardSprite,typeof(Sprite));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        private void ChangeUsableWithoutTarget()
        {
            UsableWithoutTarget = EditorGUILayout.Toggle("无需选目标即可使用:", UsableWithoutTarget);
        }
        
        private void ChangeExhaustAfterPlay()
        {
            ExhaustAfterPlay = EditorGUILayout.Toggle("打出后消耗:", ExhaustAfterPlay);
        }
        
        private bool _isGeneralSettingsFolded;
        private Vector2 _generalSettingsScrollPos;
        private void ChangeGeneralSettings()
        {
            _isGeneralSettingsFolded = EditorGUILayout.BeginFoldoutHeaderGroup(_isGeneralSettingsFolded, "基本设置");
            if (_isGeneralSettingsFolded)
            {
            ChangeId();
            ChangeCardName();
            _generalSettingsScrollPos = EditorGUILayout.BeginScrollView(_generalSettingsScrollPos,GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            ChangeManaCost();
            ChangeRarity();
            ChangeUsableWithoutTarget();
            ChangeExhaustAfterPlay();
            ChangeAudioActionType();
            EditorGUILayout.EndVertical();
            GUILayout.Space(100);
            ChangeCardSprite();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private bool _isCardActionDataListFolded;
        private Vector2 _cardActionScrollPos;
        private void ChangeCardActionDataList()
        {
            _isCardActionDataListFolded =EditorGUILayout.BeginFoldoutHeaderGroup(_isCardActionDataListFolded, "卡牌效果");
            if (_isCardActionDataListFolded)
            {
                _cardActionScrollPos = EditorGUILayout.BeginScrollView(_cardActionScrollPos,GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginHorizontal();
                List<CardActionData> _removedList = new List<CardActionData>();
                for (var i = 0; i < CardActionDataList.Count; i++)
                {
                    var cardActionData = CardActionDataList[i];
                    EditorGUILayout.BeginVertical("box", GUILayout.Width(150), GUILayout.MaxHeight(50));
                
                    EditorGUILayout.BeginHorizontal();
                    GUIStyle idStyle = new GUIStyle();
                    idStyle.fontSize = 16;
                    idStyle.fixedWidth = 25;
                    idStyle.fixedHeight = 25;
                    idStyle.fontStyle = FontStyle.Bold;
                    idStyle.normal.textColor = Color.white;
                    EditorGUILayout.LabelField($"效果序号: {i}",idStyle);
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("X", GUILayout.MaxWidth(25), GUILayout.MaxHeight(25)))
                        _removedList.Add(cardActionData);
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Separator();
                    var actionTypes = new[] { "攻击", "治疗", "格挡", "增加力量", "增加最大生命", "抽牌", "获得灵力", "吸血", "眩晕", "消耗", "施加虚弱", "施加脆弱", "施加易伤", "增加敏捷", "反伤" };
                    var actionValues = (CardActionType[])System.Enum.GetValues(typeof(CardActionType));
                    var currentActionIdx = System.Array.IndexOf(actionValues, cardActionData.CardActionType);
                    if (currentActionIdx < 0) currentActionIdx = 0;
                    var newActionIdx = EditorGUILayout.Popup("效果类型", currentActionIdx, actionTypes, GUILayout.Width(250));
                    var newActionType = actionValues[newActionIdx];

                    if (newActionType != CardActionType.Exhaust)
                    {
                        var targetTypes = new[] { "敌方", "友方", "全体敌方", "全体友方", "随机敌方", "随机友方" };
                        var targetValues = (ActionTargetType[])System.Enum.GetValues(typeof(ActionTargetType));
                        var currentTargetIdx = System.Array.IndexOf(targetValues, cardActionData.ActionTargetType);
                        if (currentTargetIdx < 0) currentTargetIdx = 0;
                        var newTargetIdx = EditorGUILayout.Popup("目标类型", currentTargetIdx, targetTypes, GUILayout.Width(250));
                        cardActionData.EditActionTarget(targetValues[newTargetIdx]);
                        var newActionValue = EditorGUILayout.FloatField("数值: ",cardActionData.ActionValue);
                        cardActionData.EditActionValue(newActionValue);
                    }
                    
                    var newActionDelay = EditorGUILayout.FloatField("延迟: ",cardActionData.ActionDelay);
                    
                    cardActionData.EditActionDelay(newActionDelay);
                    cardActionData.EditActionType(newActionType);
                    EditorGUILayout.EndVertical();
                }

                foreach (var cardActionData in _removedList)
                    CardActionDataList.Remove(cardActionData);

                if (GUILayout.Button("+ 添加效果",GUILayout.Width(80),GUILayout.Height(50)))
                    CardActionDataList.Add(new CardActionData());
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private bool _isDescriptonDataListFolded;
        private Vector2 _descriptionDataScrollPos;
      
        private void ChangeCardDescriptionDataList()
        {
            _isDescriptonDataListFolded =EditorGUILayout.BeginFoldoutHeaderGroup(_isDescriptonDataListFolded, "描述");
            if (_isDescriptonDataListFolded)
            {
                _descriptionDataScrollPos = EditorGUILayout.BeginScrollView(_descriptionDataScrollPos,GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginHorizontal();
                List<CardDescriptionData> _removedList = new List<CardDescriptionData>();
                for (var i = 0; i < CardDescriptionDataList.Count; i++)
                {
                    var descriptionData = CardDescriptionDataList[i];
                    
                    EditorGUILayout.BeginVertical("box", GUILayout.Width(175), GUILayout.Height(100));
                    EditorGUILayout.BeginHorizontal();
                    descriptionData.EditUseModifier(EditorGUILayout.ToggleLeft("使用数值修饰", descriptionData.UseModifier,
                        GUILayout.Width(125), GUILayout.Height(25)));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(25)))
                        _removedList.Add(descriptionData);
                    EditorGUILayout.EndHorizontal();
                    
                    descriptionData.EditEnableOverrideColor(EditorGUILayout.ToggleLeft("自定义颜色", descriptionData.EnableOverrideColor,
                        GUILayout.Width(125), GUILayout.Height(25)));
                    
                    EditorGUILayout.Space(5);

                    if (descriptionData.EnableOverrideColor)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.Separator();
                        descriptionData.EditOverrideColor(EditorGUILayout.ColorField(descriptionData.OverrideColor));
                        descriptionData.EditOverrideColorOnValueScaled(EditorGUILayout.ToggleLeft("数值变化时变色", descriptionData.OverrideColorOnValueScaled,
                            GUILayout.Width(125), GUILayout.Height(25)));
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space(5);
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    if (descriptionData.UseModifier)
                    {
                        EditorGUILayout.BeginVertical();
                        
                        var clampedIndex = Mathf.Clamp(descriptionData.ModifiedActionValueIndex, 0,
                            CardActionDataList.Count - 1);
                        descriptionData.EditModifiedActionValueIndex(
                            EditorGUILayout.IntField("效果序号:",clampedIndex));
                       
                        var statNames = new[] { "无", "格挡", "中毒", "力量", "敏捷", "眩晕", "虚弱", "脆弱", "易伤", "反伤" };
                        var statValues = (StatusType[])System.Enum.GetValues(typeof(StatusType));
                        var currentStatIdx = System.Array.IndexOf(statValues, descriptionData.ModiferStats);
                        if (currentStatIdx < 0) currentStatIdx = 0;
                        var newStatIdx = EditorGUILayout.Popup("属性类型:", currentStatIdx, statNames);
                        descriptionData.EditModiferStats(statValues[newStatIdx]);
                        descriptionData.EditUsePrefixOnModifiedValues(EditorGUILayout.ToggleLeft("数值前缀", descriptionData.UsePrefixOnModifiedValue,
                            GUILayout.Width(125), GUILayout.Height(25)));
                        if (descriptionData.UsePrefixOnModifiedValue)
                            descriptionData.EditPrefixOnModifiedValues(
                                EditorGUILayout.TextField("前缀:",descriptionData.ModifiedValuePrefix));
                        
                        EditorGUILayout.EndVertical();
                    }
                    else
                    { 
                        var desc = EditorGUILayout.TextArea(descriptionData.DescriptionText, GUILayout.Width(150),
                            GUILayout.Height(50));

                        // var hasExhaust = CardActionDataList.Find(x => x.CardActionType == CardActionType.Exhaust);
                        // if (ExhaustAfterPlay || hasExhaust != null)
                        // {
                        //     desc += " Exhaust ";
                        // }
                        //
                        descriptionData.EditDescriptionText(desc);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                
                

                foreach (var cardActionData in _removedList)
                    CardDescriptionDataList.Remove(cardActionData);

                if (GUILayout.Button("+",GUILayout.Width(50),GUILayout.Height(50)))
                    CardDescriptionDataList.Add(new CardDescriptionData());
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Separator();
                EditorGUILayout.BeginHorizontal("box");
                var str = new StringBuilder();
                foreach (var cardDescriptionData in CardDescriptionDataList)
                {
                    str.Append(cardDescriptionData.UseModifier
                        ? cardDescriptionData.GetModifiedValueEditor(SelectedCardData)
                        : cardDescriptionData.GetDescriptionEditor());
                }
                
                GUIStyle headStyle = new GUIStyle();
                headStyle.fontStyle = FontStyle.Bold;
                headStyle.normal.textColor = Color.white;
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.LabelField("描述预览",headStyle);
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField(str.ToString());
                EditorGUILayout.Separator();

               

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private Vector2 _specialKeywordScrool;
        private bool _isSpecialKeywordsFolded;
        private void ChangeSpecialKeywords()
        {
            _isSpecialKeywordsFolded =EditorGUILayout.BeginFoldoutHeaderGroup(_isSpecialKeywordsFolded, "特殊关键词（Tooltip提示）");
            if (_isSpecialKeywordsFolded)
            {
            EditorGUILayout.BeginVertical("box");
            _specialKeywordScrool = EditorGUILayout.BeginScrollView(_specialKeywordScrool);
            EditorGUILayout.BeginHorizontal();
            var specialKeyCount = Enum.GetNames(typeof(StatusType));

            for (var i = 0; i < specialKeyCount.Length; i++)
            {
                if ((StatusType)i == StatusType.None) continue;
                EditorGUILayout.BeginVertical(GUILayout.Width(100));
                var hasKey = SpecialKeywordsList.Contains((StatusType)i);
                var displayNames = new[] { "无", "格挡", "中毒", "力量", "敏捷", "眩晕", "虚弱", "脆弱", "易伤", "反伤" };
                var displayName = i < displayNames.Length ? displayNames[i] : ((StatusType)i).ToString();
                EditorGUILayout.LabelField(displayName);
                var newValue = EditorGUILayout.Toggle(hasKey);
                if (newValue)
                {
                    if (!SpecialKeywordsList.Contains((StatusType)i))
                        SpecialKeywordsList.Add((StatusType)i);
                }
                else
                {
                    if (SpecialKeywordsList.Contains((StatusType)i))
                        SpecialKeywordsList.Remove((StatusType)i);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        private void ChangeAudioActionType()
        {
            var audioNames = new[] { "攻击", "撕咬", "格挡", "按钮", "抽牌", "饮用", "敌人死亡", "治疗", "中毒", "能力", "挥舞" };
            var audioValues = (AudioActionType[])System.Enum.GetValues(typeof(AudioActionType));
            var currentIdx = System.Array.IndexOf(audioValues, AudioType);
            if (currentIdx < 0) currentIdx = 0;
            var newIdx = EditorGUILayout.Popup("音效类型:", currentIdx, audioNames);
            AudioType = audioValues[newIdx];
        }

       
        private void SaveCardData()
        {
            if (!SelectedCardData) return;
            
            SelectedCardData.EditId(CardId);
            SelectedCardData.EditCardName(CardName);
            SelectedCardData.EditManaCost(ManaCost);
            SelectedCardData.EditCardSprite(CardSprite);
            SelectedCardData.EditUsableWithoutTarget(UsableWithoutTarget);
            SelectedCardData.EditExhaustAfterPlay(ExhaustAfterPlay);
            SelectedCardData.EditCardActionDataList(CardActionDataList);
            SelectedCardData.EditCardDescriptionDataList(CardDescriptionDataList);
            SelectedCardData.EditSpecialKeywordsList(SpecialKeywordsList);
            SelectedCardData.EditAudioType(AudioType);
            EditorUtility.SetDirty(SelectedCardData);
            AssetDatabase.SaveAssets();
        }
        private void RefreshCardData()
        {
            SelectedCardData = null;
            ClearCachedCardData();
            AllCardDataList?.Clear();
            AllCardDataList = ListExtentions.GetAllInstances<CardData>().ToList();
            CacheActiveMethodCardIds();
        }
        #endregion
#endif
    }
}
