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

        [Header("Tree Position")]
        [SerializeField] private RealmLevel realm;
        [SerializeField] private Vector2 position;

        [Header("Unlock")]
        [SerializeField] private NodeUnlockType unlockType;
        [SerializeField] private int comprehensionCost;
        [Tooltip("前置节点ID列表，全部解锁后此节点才可解锁。空=该层首个可解锁")]
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

        #region Properties
        public string NodeId => nodeId;
        public string NodeName => nodeName;
        public string Description => description;
        public RealmLevel Realm => realm;
        public Vector2 Position => position;
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
        #endregion

        #region Editor
#if UNITY_EDITOR
        public void EditNodeId(string id) => nodeId = id;
        public void EditNodeName(string name) => nodeName = name;
        public void EditDescription(string desc) => description = desc;
        public void EditRealm(RealmLevel r) => realm = r;
        public void EditPosition(Vector2 pos) => position = pos;
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
#endif
        #endregion
    }
}
