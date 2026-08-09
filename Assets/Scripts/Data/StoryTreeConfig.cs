using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 故事节点类型
    /// </summary>
    public enum StoryNodeType
    {
        Root,       // 根节点（自动解锁）
        Story,      // 剧情节点
        Combat,     // 战斗节点
        Reward,     // 奖励节点
        Choice      // 选择节点（多选一）
    }

    /// <summary>
    /// 分支奖励类型
    /// </summary>
    public enum StoryRewardType
    {
        None,
        CardUnlock,     // 解锁卡牌
        PotionUnlock,   // 解锁药水
        RelicUnlock,    // 解锁法宝
        MaterialUnlock, // 解锁灵材
        GoldReward,     // 灵石奖励
        CardReward,     // 直接获得卡牌
        RecipeUnlock,   // 解锁配方
        MultiReward     // 多种奖励
    }

    /// <summary>
    /// 单个故事节点
    /// </summary>
    [Serializable]
    public class StoryNodeData
    {
        public string nodeId;
        public string nodeName;
        [TextArea(2, 4)] public string description;
        public StoryNodeType nodeType = StoryNodeType.Story;
        public StoryRewardType rewardType = StoryRewardType.None;
        public List<string> prerequisites = new List<string>();
        public List<string> rewardIds = new List<string>();
        public int goldReward;
        public int chapter;          // 所属章节(0-5)
        public Vector2 position;    // 树状图UI坐标
        public string iconText;     // 节点图标文字(如"丹"/"剑"/"体")
        public string colorHex;     // 节点颜色(#RRGGBB)
    }

    /// <summary>
    /// 全局故事树配置（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "StoryTreeConfig", menuName = "CardGame/StoryTreeConfig")]
    public class StoryTreeConfig : ScriptableObject
    {
        public List<StoryNodeData> nodes = new List<StoryNodeData>();

        public List<StoryNodeData> GetRootNodes()
            => nodes.FindAll(n => n.nodeType == StoryNodeType.Root || n.prerequisites.Count == 0);

        public List<StoryNodeData> GetAvailableNodes(HashSet<string> unlockedIds)
            => nodes.FindAll(n => !unlockedIds.Contains(n.nodeId) &&
                n.prerequisites.TrueForAll(p => unlockedIds.Contains(p)));

        public StoryNodeData GetNode(string id)
            => nodes.Find(n => n.nodeId == id);
    }
}
