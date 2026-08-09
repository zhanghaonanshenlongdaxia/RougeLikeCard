using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    public interface IStoryModel : IModel
    {
        HashSet<string> UnlockedNodeIds { get; }
    }

    public class StoryModel : AbstractModel, IStoryModel
    {
        public HashSet<string> UnlockedNodeIds { get; } = new HashSet<string>();
        protected override void OnInit() { }
    }

    public interface IStorySystem : ISystem
    {
        void UnlockNode(string nodeId);
        List<StoryNodeData> GetAvailableNodes();
        bool IsNodeUnlocked(string nodeId);
        void OnEvacuateComplete();
        void InitRootNodes();
        StoryTreeConfig GetConfig();
    }

    public class StorySystem : AbstractSystem, IStorySystem
    {
        private IStoryModel _model;
        private StoryTreeConfig _config;

        protected override void OnInit()
        {
            _model = this.GetModel<IStoryModel>();
#if UNITY_EDITOR
            _config = UnityEditor.AssetDatabase.LoadAssetAtPath<StoryTreeConfig>(
                "Assets/NueGames/NueDeck/Data/StoryTree/StoryTreeConfig.asset");
#else
            _config = Resources.Load<StoryTreeConfig>("StoryTreeConfig");
#endif
            if (_config == null)
                Debug.LogWarning("[Story] StoryTreeConfig not found!");
        }

        public StoryTreeConfig GetConfig() => _config;

        public void InitRootNodes()
        {
            if (_config == null) return;
            // 自动解锁所有Root节点
            foreach (var node in _config.GetRootNodes())
            {
                if (!_model.UnlockedNodeIds.Contains(node.nodeId))
                {
                    _model.UnlockedNodeIds.Add(node.nodeId);
                    ApplyRewards(node);
                    Debug.Log($"[Story] Root unlocked: {node.nodeName}");
                }
            }
        }

        public void UnlockNode(string nodeId)
        {
            if (_config == null || _model.UnlockedNodeIds.Contains(nodeId)) return;

            var node = _config.GetNode(nodeId);
            if (node == null) return;

            // 检查前置条件
            foreach (var pre in node.prerequisites)
                if (!_model.UnlockedNodeIds.Contains(pre)) return;

            _model.UnlockedNodeIds.Add(nodeId);
            ApplyRewards(node);
            Debug.Log($"[Story] Node unlocked: {node.nodeName} ({nodeId})");
        }

        void ApplyRewards(StoryNodeData node)
        {
            var metaSystem = this.GetSystem<IMetaSystem>();
            var arch = CardGameArchitecture.Interface;
            var battleModel = arch.GetModel<IBattleModel>();
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;

            switch (node.rewardType)
            {
                case StoryRewardType.CardUnlock:
                    foreach (var id in node.rewardIds)
                        metaSystem.UnlockCardDirect(id);
                    break;

                case StoryRewardType.PotionUnlock:
                    foreach (var id in node.rewardIds)
                        metaSystem.UnlockPotionDirect(id);
                    break;

                case StoryRewardType.RelicUnlock:
                    foreach (var id in node.rewardIds)
                        metaSystem.UnlockRelicDirect(id);
                    break;

                case StoryRewardType.GoldReward:
                    if (node.goldReward > 0)
                    {
                        battleModel.CurrentGold.Value += node.goldReward;
                        if (gm != null)
                            gm.PersistentGameplayData.CurrentGold = battleModel.CurrentGold.Value;
                    }
                    break;

                case StoryRewardType.CardReward:
                    // 直接加入卡组
                    if (gm != null)
                        foreach (var id in node.rewardIds)
                        {
                            var card = gm.GameplayData.AllCardsList.Find(c => c.Id == id);
                            if (card != null)
                                gm.PersistentGameplayData.CurrentCardsList.Add(card);
                        }
                    break;

                case StoryRewardType.MultiReward:
                    // rewardIds中混合多种类型，用前缀区分: card:xxx / potion:xxx / relic:xxx
                    foreach (var id in node.rewardIds)
                    {
                        if (id.StartsWith("card:")) metaSystem.UnlockCardDirect(id.Substring(5));
                        else if (id.StartsWith("potion:")) metaSystem.UnlockPotionDirect(id.Substring(7));
                        else if (id.StartsWith("relic:")) metaSystem.UnlockRelicDirect(id.Substring(6));
                    }
                    if (node.goldReward > 0)
                    {
                        battleModel.CurrentGold.Value += node.goldReward;
                        if (gm != null) gm.PersistentGameplayData.CurrentGold = battleModel.CurrentGold.Value;
                    }
                    break;
            }
        }

        public List<StoryNodeData> GetAvailableNodes()
        {
            if (_config == null) return new List<StoryNodeData>();
            return _config.GetAvailableNodes(_model.UnlockedNodeIds);
        }

        public bool IsNodeUnlocked(string nodeId)
            => _model.UnlockedNodeIds.Contains(nodeId);

        public void OnEvacuateComplete()
        {
            // 撤离后刷新可解锁节点（不需要额外操作，GetAvailableNodes自动计算）
            var available = GetAvailableNodes();
            Debug.Log($"[Story] Evacuate complete. Available nodes: {available.Count}");
            foreach (var n in available)
                Debug.Log($"  Available: {n.nodeName} ({n.nodeId})");
        }
    }

    // IMetaSystem扩展方法（直接解锁，不检查章节）
    public static class MetaSystemExtensions
    {
        public static void UnlockCardDirect(this IMetaSystem system, string cardId)
        {
            var model = CardGameArchitecture.Interface.GetModel<IMetaModel>();
            if (!model.UnlockedCardIds.Contains(cardId))
                model.UnlockedCardIds.Add(cardId);
        }

        public static void UnlockPotionDirect(this IMetaSystem system, string potionId)
        {
            var model = CardGameArchitecture.Interface.GetModel<IMetaModel>();
            if (!model.UnlockedPotionIds.Contains(potionId))
                model.UnlockedPotionIds.Add(potionId);
        }

        public static void UnlockRelicDirect(this IMetaSystem system, string relicId)
        {
            var model = CardGameArchitecture.Interface.GetModel<IMetaModel>();
            if (!model.UnlockedRelicIds.Contains(relicId))
                model.UnlockedRelicIds.Add(relicId);
        }
    }
}
