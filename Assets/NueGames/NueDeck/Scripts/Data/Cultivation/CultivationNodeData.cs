using System;
using System.Collections.Generic;
using CardGame;
using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Cultivation
{
    /// <summary>
    /// 功法修炼节点数据 — 功法树中的一个可解锁节点
    /// </summary>
    [Serializable]
    public class CultivationNodeData
    {
        [Header("Identity")]
        [SerializeField] private string nodeId;
        [SerializeField] private string nodeName;
        [SerializeField][TextArea] private string description;
        [Tooltip("神通图标。多阶段神通只需第一阶段设置，全阶段共用")]
        [SerializeField] private Sprite nodeIcon;

        [Header("Tree Position")]
        [SerializeField] private RealmLevel realm;
        [Tooltip("x=列号(从0开始)，y=该列内下标(从0开始)。用于控制 UI 排列顺序")]
        [SerializeField] private Vector2 gridIndex;

        [Header("Element")]
        [Tooltip("节点元素属性。None=使用功法主元素；设为具体元素则覆盖。用于无属性节点显示")]
        [SerializeField] private ElementType nodeElement = ElementType.None;

        [Header("Unlock")]
        [SerializeField] private NodeUnlockType unlockType;
        [SerializeField] private int comprehensionCost;
        [Tooltip("前置节点ID列表，全部解锁后此节点才可解锁。空=无前置，只要参悟点够就能解锁")]
        [SerializeField] private List<string> prerequisites;
        [Tooltip("互斥组ID，同组只能选一个节点。空=不互斥")]
        [SerializeField] private string mutexGroup;

        [Header("Reward")]
        [SerializeField] private NodeRewardType rewardType;
        [Tooltip("卡牌ID/配方ID/神通ID列表，取决于rewardType")]
        [SerializeField] private List<string> rewardIds;

        [Header("Passive Stat")]
        [SerializeField] private PassiveStatType passiveStat;
        [SerializeField] private int passiveValue;

        [Header("Craft Bonus")]
        [SerializeField] private CraftBonusType craftBonusType;
        [SerializeField] private float craftBonusValue;

        [Header("Advancement")]
        [Tooltip("可进阶次数，0=不可进阶。如3表示有3段，每段填充1颗星")]
        [SerializeField] private int maxAdvancementLevel = 0;

        /// <summary>当前进阶等级（运行时，不序列化）</summary>
        [System.NonSerialized] private int _currentAdvancementLevel = 0;

        #region Properties
        public string NodeId => nodeId;
        public string NodeName => nodeName;
        public string Description => description;
        public Sprite NodeIcon => nodeIcon;
        public RealmLevel Realm => realm;
        public Vector2 GridIndex => gridIndex;
        public ElementType NodeElement => nodeElement;
        public NodeUnlockType UnlockType => unlockType;
        public int ComprehensionCost => comprehensionCost;
        public List<string> Prerequisites => prerequisites;
        public string MutexGroup => mutexGroup;
        public NodeRewardType RewardType => rewardType;
        public List<string> RewardIds => rewardIds;
        public PassiveStatType PassiveStat => passiveStat;
        public int PassiveValue => passiveValue;
        public CraftBonusType CraftBonusType => craftBonusType;
        public float CraftBonusValue => craftBonusValue;
        public int MaxAdvancementLevel => maxAdvancementLevel;
        public int CurrentAdvancementLevel { get => _currentAdvancementLevel; set => _currentAdvancementLevel = value; }
        #endregion

        #region Editor
#if UNITY_EDITOR
        public void EditNodeId(string id) => nodeId = id;
        public void EditNodeName(string name) => nodeName = name;
        public void EditDescription(string desc) => description = desc;
        public void EditNodeIcon(Sprite icon) => nodeIcon = icon;
        public void EditRealm(RealmLevel r) => realm = r;
        public void EditGridIndex(Vector2 idx) => gridIndex = idx;
        public void EditNodeElement(ElementType el) => nodeElement = el;
        public void EditUnlockType(NodeUnlockType type) => unlockType = type;
        public void EditComprehensionCost(int cost) => comprehensionCost = cost;
        public void EditPrerequisites(List<string> pre) => prerequisites = pre;
        public void EditMutexGroup(string group) => mutexGroup = group;
        public void EditRewardType(NodeRewardType type) => rewardType = type;
        public void EditRewardIds(List<string> ids) => rewardIds = ids;
        public void EditPassiveStat(PassiveStatType type) => passiveStat = type;
        public void EditPassiveValue(int val) => passiveValue = val;
        public void EditCraftBonusType(CraftBonusType type) => craftBonusType = type;
        public void EditCraftBonusValue(float val) => craftBonusValue = val;
        public void EditMaxAdvancementLevel(int val) => maxAdvancementLevel = val;
#endif
        #endregion
    }
}
