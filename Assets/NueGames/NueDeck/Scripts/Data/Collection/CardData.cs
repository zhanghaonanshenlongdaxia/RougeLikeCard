using System;
using System.Collections.Generic;
using System.Text;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Collection
{
    [CreateAssetMenu(fileName = "Card Data",menuName = "NueDeck/Collection/Card",order = 0)]
    public class CardData : ScriptableObject
    {
        [Header("Card Profile")] 
        [SerializeField] private string id;
        [SerializeField] private string cardName;
        [SerializeField] private int manaCost;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private RarityType rarity;
        
        [Header("Action Settings")]
        [SerializeField] private bool usableWithoutTarget;
        [SerializeField] private bool exhaustAfterPlay;
        [Tooltip("Power牌：打出后消耗，效果整局战斗持续（通过永久状态如Strength/Dexterity）")]
        [SerializeField] private bool isPowerCard;
        [SerializeField] private List<CardActionData> cardActionDataList;
        
        [Header("Description")]
        [SerializeField] private List<CardDescriptionData> cardDescriptionDataList;
        [SerializeField] private List<SpecialKeywords> specialKeywordsList;
        
        [Header("Fx")]
        [SerializeField] private AudioActionType audioType;

        [Header("Upgrade")]
        [Tooltip("升级后的卡牌名称（如 Strike+），留空则自动加+")]
        [SerializeField] private string upgradedCardName;
        [Tooltip("升级后的费用，-1表示不改变")]
        [SerializeField] private int upgradedManaCost = -1;
        [Tooltip("升级后替换的效果列表，留空则使用原效果")]
        [SerializeField] private List<CardActionData> upgradedCardActionDataList;

        [Header("Path & Build")]
        [Tooltip("道：None/Sword(剑道)/Body(体道)/Spirit(灵道)")]
        [SerializeField] private PathType pathType = PathType.None;
        [Tooltip("Build方向：MultiHit/Burst/Thorn/Sustain/Debuff/ManaBurst")]
        [SerializeField] private BuildTag buildTag = BuildTag.None;
        [Tooltip("解锁章节，0=初始可用，1+=需剧情解锁")]
        [SerializeField] private int unlockChapter;
        [Tooltip("0=前期基础，1=后期(1:1.1加成)")]
        [SerializeField] private int powerTier;
        [Tooltip("被此卡替换的卡牌Id(升级/进阶关系，奖励池互斥)")]
        [SerializeField] private string replacesCardId;

        #region Cache
        public string Id => id;
        public bool UsableWithoutTarget => usableWithoutTarget;
        public int ManaCost => IsUpgraded && upgradedManaCost >= 0 ? upgradedManaCost : manaCost;
        public string CardName => IsUpgraded ? (string.IsNullOrEmpty(upgradedCardName) ? cardName + "+" : upgradedCardName) : cardName;
        public Sprite CardSprite => cardSprite;
        public List<CardActionData> CardActionDataList => IsUpgraded && upgradedCardActionDataList != null && upgradedCardActionDataList.Count > 0 ? upgradedCardActionDataList : cardActionDataList;
        public List<CardDescriptionData> CardDescriptionDataList => cardDescriptionDataList;
        public List<SpecialKeywords> KeywordsList => specialKeywordsList;
        public AudioActionType AudioType => audioType;
        public string MyDescription { get; set; }
        public RarityType Rarity => rarity;

        public bool ExhaustAfterPlay => exhaustAfterPlay;
        public bool IsPowerCard => isPowerCard;

        public PathType PathType => pathType;
        public BuildTag BuildTag => buildTag;
        public int UnlockChapter => unlockChapter;
        public int PowerTier => powerTier;
        public string ReplacesCardId => replacesCardId;

        /// <summary>卡牌是否已升级</summary>
        public bool IsUpgraded { get; private set; }

        /// <summary>升级后的费用（只读）</summary>
        public int UpgradedManaCost => upgradedManaCost;

        /// <summary>是否有升级效果数据</summary>
        public bool HasUpgradeData => upgradedCardActionDataList != null && upgradedCardActionDataList.Count > 0 || upgradedManaCost >= 0 || !string.IsNullOrEmpty(upgradedCardName);

        #endregion
        
        #region Upgrade Methods
        /// <summary>升级卡牌（运行时调用）</summary>
        public void Upgrade()
        {
            if (IsUpgraded) return;
            IsUpgraded = true;
            UpdateDescription();
        }

        /// <summary>降级卡牌（运行时调用）</summary>
        public void Downgrade()
        {
            IsUpgraded = false;
            UpdateDescription();
        }
        #endregion
        
        #region Methods
        public void UpdateDescription()
        {
            var str = new StringBuilder();

            foreach (var descriptionData in cardDescriptionDataList)
            {
                str.Append(descriptionData.UseModifier
                    ? descriptionData.GetModifiedValue(this)
                    : descriptionData.GetDescription());
            }
            
            MyDescription = str.ToString();
        }
        #endregion

        #region Editor Methods
#if UNITY_EDITOR
        public void EditCardName(string newName) => cardName = newName;
        public void EditId(string newId) => id = newId;
        public void EditManaCost(int newCost) => manaCost = newCost;
        public void EditRarity(RarityType targetRarity) => rarity = targetRarity;
        public void EditCardSprite(Sprite newSprite) => cardSprite = newSprite;
        public void EditUsableWithoutTarget(bool newStatus) => usableWithoutTarget = newStatus;
        public void EditExhaustAfterPlay(bool newStatus) => exhaustAfterPlay = newStatus;
        public void EditCardActionDataList(List<CardActionData> newCardActionDataList) =>
            cardActionDataList = newCardActionDataList;
        public void EditCardDescriptionDataList(List<CardDescriptionData> newCardDescriptionDataList) =>
            cardDescriptionDataList = newCardDescriptionDataList;
        public void EditSpecialKeywordsList(List<SpecialKeywords> newSpecialKeywordsList) =>
            specialKeywordsList = newSpecialKeywordsList;
        public void EditAudioType(AudioActionType newAudioActionType) => audioType = newAudioActionType;
        public void EditPathType(PathType newPathType) => pathType = newPathType;
        public void EditBuildTag(BuildTag newBuildTag) => buildTag = newBuildTag;
        public void EditUnlockChapter(int newChapter) => unlockChapter = newChapter;
        public void EditPowerTier(int newTier) => powerTier = newTier;
        public void EditReplacesCardId(string newId) => replacesCardId = newId;
#endif

        #endregion

    }

    [Serializable]
    public class CardActionData
    {
        [SerializeField] private CardActionType cardActionType;
        [SerializeField] private ActionTargetType actionTargetType;
        [SerializeField] private float actionValue;
        [SerializeField] private float actionDelay;
        [Tooltip("打击次数，用于多段攻击（如打3次每次5伤害）")]
        [SerializeField] private int hitCount = 1;

        public ActionTargetType ActionTargetType => actionTargetType;
        public CardActionType CardActionType => cardActionType;
        public float ActionValue => actionValue;
        public float ActionDelay => actionDelay;
        public int HitCount => hitCount > 0 ? hitCount : 1;

        #region Editor

#if UNITY_EDITOR
        public void EditActionType(CardActionType newType) =>  cardActionType = newType;
        public void EditActionTarget(ActionTargetType newTargetType) => actionTargetType = newTargetType;
        public void EditActionValue(float newValue) => actionValue = newValue;
        public void EditActionDelay(float newValue) => actionDelay = newValue;

#endif


        #endregion
    }

    [Serializable]
    public class CardDescriptionData
    {
        [Header("Text")]
        [SerializeField] private string descriptionText;
        [SerializeField] private bool enableOverrideColor;
        [SerializeField] private Color overrideColor = Color.black;
       
        [Header("Modifer")]
        [SerializeField] private bool useModifier;
        [SerializeField] private int modifiedActionValueIndex;
        [SerializeField] private StatusType modiferStats;
        [SerializeField] private bool usePrefixOnModifiedValue;
        [SerializeField] private string modifiedValuePrefix = "*";
        [SerializeField] private bool overrideColorOnValueScaled;

        public string DescriptionText => descriptionText;
        public bool EnableOverrideColor => enableOverrideColor;
        public Color OverrideColor => overrideColor;
        public bool UseModifier => useModifier;
        public int ModifiedActionValueIndex => modifiedActionValueIndex;
        public StatusType ModiferStats => modiferStats;
        public bool UsePrefixOnModifiedValue => usePrefixOnModifiedValue;
        public string ModifiedValuePrefix => modifiedValuePrefix;
        public bool OverrideColorOnValueScaled => overrideColorOnValueScaled;
        
        private CombatManager CombatManager => CombatManager.Instance;

        public string GetDescription()
        {
            var str = new StringBuilder();
            
            str.Append(DescriptionText);
            
            if (EnableOverrideColor && !string.IsNullOrEmpty(str.ToString())) 
                str.Replace(str.ToString(),ColorExtentions.ColorString(str.ToString(),OverrideColor));
            
            return str.ToString();
        }

        public string GetModifiedValue(CardData cardData)
        {
            if (cardData.CardActionDataList.Count <= 0) return "";
            
            if (ModifiedActionValueIndex>=cardData.CardActionDataList.Count)
                modifiedActionValueIndex = cardData.CardActionDataList.Count - 1;

            if (ModifiedActionValueIndex<0)
                modifiedActionValueIndex = 0;
            
            var str = new StringBuilder();
            var value = cardData.CardActionDataList[ModifiedActionValueIndex].ActionValue;
            var modifer = 0;
            if (CombatManager)
            {
                var player = CombatManager.CurrentMainAlly;
               
                if (player)
                {
                    modifer = player.CharacterStats.StatusDict[ModiferStats].StatusValue;
                    value += modifer;

                    if (modifer != 0)
                    {
                        if (usePrefixOnModifiedValue)
                            str.Append(modifiedValuePrefix);
                    }
                }
            }
           
            str.Append(value);

            if (EnableOverrideColor)
            {
                if (OverrideColorOnValueScaled)
                {
                    if (modifer != 0)
                        str.Replace(str.ToString(),ColorExtentions.ColorString(str.ToString(),OverrideColor));
                }
                else
                {
                    str.Replace(str.ToString(),ColorExtentions.ColorString(str.ToString(),OverrideColor));
                }
               
            }
            
            return str.ToString();
        }

        #region Editor
#if UNITY_EDITOR
        
        public string GetDescriptionEditor()
        {
            var str = new StringBuilder();
            
            str.Append(DescriptionText);
            
            return str.ToString();
        }

        public string GetModifiedValueEditor(CardData cardData)
        {
            if (cardData.CardActionDataList.Count <= 0) return "";
            
            if (ModifiedActionValueIndex>=cardData.CardActionDataList.Count)
                modifiedActionValueIndex = cardData.CardActionDataList.Count - 1;

            if (ModifiedActionValueIndex<0)
                modifiedActionValueIndex = 0;
            
            var str = new StringBuilder();
            var value = cardData.CardActionDataList[ModifiedActionValueIndex].ActionValue;
            if (CombatManager)
            {
                var player = CombatManager.CurrentMainAlly;
                if (player)
                {
                    var modifer =player.CharacterStats.StatusDict[ModiferStats].StatusValue;
                    value += modifer;
                
                    if (modifer!= 0)
                        str.Append("*");
                }
            }
           
            str.Append(value);
          
            return str.ToString();
        }
        
        public void EditDescriptionText(string newText) => descriptionText = newText;
        public void EditEnableOverrideColor(bool newStatus) => enableOverrideColor = newStatus;
        public void EditOverrideColor(Color newColor) => overrideColor = newColor;
        public void EditUseModifier(bool newStatus) => useModifier = newStatus;
        public void EditModifiedActionValueIndex(int newIndex) => modifiedActionValueIndex = newIndex;
        public void EditModiferStats(StatusType newStatusType) => modiferStats = newStatusType;
        public void EditUsePrefixOnModifiedValues(bool newStatus) => usePrefixOnModifiedValue = newStatus;
        public void EditPrefixOnModifiedValues(string newText) => modifiedValuePrefix = newText;
        public void EditOverrideColorOnValueScaled(bool newStatus) => overrideColorOnValueScaled = newStatus;

#endif
        #endregion
    }
}