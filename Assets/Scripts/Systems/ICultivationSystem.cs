using System.Collections.Generic;
using System.Linq;
using NueGames.NueDeck.Scripts.Data.Cultivation;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using QFramework;
using UnityEngine;

namespace CardGame
{
    public interface ICultivationSystem : ISystem
    {
        // 功法
        void LearnMethod(string methodId);
        void SetActiveMethod(string methodId);
        CultivationMethodData GetMethodConfig(string methodId);
        List<CultivationMethodData> GetAllMethodConfigs();
        List<CultivationMethodData> GetLearnedMethods();

        // 节点
        bool CanUnlockNode(string nodeId);
        void UnlockNode(string nodeId);
        bool IsNodeUnlocked(string nodeId);

        // 当前功法查询
        List<string> GetActiveMethodCards();
        Dictionary<PassiveStatType, int> GetActivePassiveStats();
        Dictionary<CraftBonusType, float> GetActiveCraftBonuses();

        // 神通
        void LearnAbility(string abilityId);
        bool EquipAbility(string abilityId);
        bool UnequipAbility(string abilityId);
        int GetTotalEquippedEnergy();
        List<DivineAbilityData> GetLearnedAbilities();
        List<DivineAbilityData> GetEquippedAbilities();
        DivineAbilityData GetAbilityConfig(string abilityId);

        // 参悟点
        void AddComprehensionPoints(int amount);
        bool SpendComprehensionPoints(int amount);

        // 掉落
        bool TryAcquireAbilityBook(string abilityId);
        bool TryAcquireMethodFragment(string methodId);
    }

    public class CultivationSystem : AbstractSystem, ICultivationSystem
    {
        private ICultivationModel _model;
        private List<CultivationMethodData> _allMethods;
        private List<DivineAbilityData> _allAbilities;

        protected override void OnInit()
        {
            _model = this.GetModel<ICultivationModel>();
            LoadConfigs();
        }

        private void LoadConfigs()
        {
#if UNITY_EDITOR
            _allMethods = new List<CultivationMethodData>();
            var methodGuids = UnityEditor.AssetDatabase.FindAssets("t:CultivationMethodData");
            foreach (var guid in methodGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var m = UnityEditor.AssetDatabase.LoadAssetAtPath<CultivationMethodData>(path);
                if (m != null) _allMethods.Add(m);
            }

            _allAbilities = new List<DivineAbilityData>();
            var abilityGuids = UnityEditor.AssetDatabase.FindAssets("t:DivineAbilityData");
            foreach (var guid in abilityGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var a = UnityEditor.AssetDatabase.LoadAssetAtPath<DivineAbilityData>(path);
                if (a != null) _allAbilities.Add(a);
            }
#else
            _allMethods = new List<CultivationMethodData>();
            _allAbilities = new List<DivineAbilityData>();
            // Build mode: load from Resources
            var methods = Resources.LoadAll<CultivationMethodData>("Cultivation/Methods");
            if (methods != null) _allMethods.AddRange(methods);
            var abilities = Resources.LoadAll<DivineAbilityData>("Cultivation/Abilities");
            if (abilities != null) _allAbilities.AddRange(abilities);
#endif
            Debug.Log($"[Cultivation] Loaded {_allMethods.Count} methods, {_allAbilities.Count} abilities.");
        }

        #region 功法
        public void LearnMethod(string methodId)
        {
            if (_model.LearnedMethodIds.Contains(methodId)) return;
            _model.LearnedMethodIds.Add(methodId);
            Debug.Log($"[Cultivation] Learned method: {methodId}");

            // 第一本功法自动设为当前
            if (string.IsNullOrEmpty(_model.ActiveMethodId.Value))
                _model.ActiveMethodId.Value = methodId;
        }

        public void SetActiveMethod(string methodId)
        {
            if (!_model.LearnedMethodIds.Contains(methodId))
            {
                Debug.LogWarning($"[Cultivation] Cannot set active method {methodId}, not learned.");
                return;
            }
            _model.ActiveMethodId.Value = methodId;
            Debug.Log($"[Cultivation] Active method set to: {methodId}");
        }

        public CultivationMethodData GetMethodConfig(string methodId)
            => _allMethods?.Find(m => m.MethodId == methodId);

        public List<CultivationMethodData> GetAllMethodConfigs() => _allMethods ?? new List<CultivationMethodData>();

        public List<CultivationMethodData> GetLearnedMethods()
        {
            return _allMethods?.FindAll(m => _model.LearnedMethodIds.Contains(m.MethodId))
                ?? new List<CultivationMethodData>();
        }
        #endregion

        #region 节点
        public bool CanUnlockNode(string nodeId)
        {
            var (method, node) = FindNode(nodeId);
            if (node == null || method == null) return false;
            if (_model.UnlockedNodeIds.Contains(nodeId)) return false;

            // 检查功法是否已学习
            if (!_model.LearnedMethodIds.Contains(method.MethodId)) return false;

            // 检查前置
            if (node.Prerequisites != null)
                foreach (var pre in node.Prerequisites)
                    if (!_model.UnlockedNodeIds.Contains(pre)) return false;

            // 检查互斥 — 如果同组已选了其他节点
            if (!string.IsNullOrEmpty(node.MutexGroup))
            {
                if (_model.SelectedMutexChoices.TryGetValue(node.MutexGroup, out var selected))
                    if (selected != nodeId) return false; // 同组选了别的
            }

            // 参悟点检查
            if (node.UnlockType == NodeUnlockType.Comprehension && node.ComprehensionCost > 0)
                if (_model.ComprehensionPoints.Value < node.ComprehensionCost) return false;

            return true;
        }

        public void UnlockNode(string nodeId)
        {
            if (!CanUnlockNode(nodeId)) return;

            var (method, node) = FindNode(nodeId);
            if (node == null) return;

            // 扣参悟点
            if (node.UnlockType == NodeUnlockType.Comprehension && node.ComprehensionCost > 0)
                _model.ComprehensionPoints.Value -= node.ComprehensionCost;

            _model.UnlockedNodeIds.Add(nodeId);

            // 记录互斥选择
            if (!string.IsNullOrEmpty(node.MutexGroup))
                _model.SelectedMutexChoices[node.MutexGroup] = nodeId;

            ApplyNodeRewards(node);
            Debug.Log($"[Cultivation] Node unlocked: {node.NodeName} ({nodeId})");
        }

        public bool IsNodeUnlocked(string nodeId) => _model.UnlockedNodeIds.Contains(nodeId);

        private void ApplyNodeRewards(CultivationNodeData node)
        {
            switch (node.RewardType)
            {
                case NodeRewardType.Recipe:
                    if (node.RewardIds != null)
                        foreach (var id in node.RewardIds)
                            this.GetSystem<ICraftSystem>().UnlockRecipe(id);
                    break;

                case NodeRewardType.DivineAbility:
                    if (node.RewardIds != null)
                        foreach (var id in node.RewardIds)
                            LearnAbility(id);
                    break;

                // Card, PassiveStat, CraftBonus, SpecialSkill 都是被动查询，不需要在解锁时执行
            }
        }
        #endregion

        #region 当前功法查询
        public List<string> GetActiveMethodCards()
        {
            var cards = new List<string>();
            var activeId = _model.ActiveMethodId.Value;
            if (string.IsNullOrEmpty(activeId)) return cards;

            var method = GetMethodConfig(activeId);
            if (method?.Nodes == null) return cards;

            foreach (var node in method.Nodes)
            {
                if (!_model.UnlockedNodeIds.Contains(node.NodeId)) continue;
                if (node.RewardType == NodeRewardType.Card && node.RewardIds != null)
                    cards.AddRange(node.RewardIds);
            }
            return cards;
        }

        public Dictionary<PassiveStatType, int> GetActivePassiveStats()
        {
            var stats = new Dictionary<PassiveStatType, int>();
            var activeId = _model.ActiveMethodId.Value;
            if (string.IsNullOrEmpty(activeId)) return stats;

            var method = GetMethodConfig(activeId);
            if (method?.Nodes == null) return stats;

            foreach (var node in method.Nodes)
            {
                if (!_model.UnlockedNodeIds.Contains(node.NodeId)) continue;
                if (node.RewardType != NodeRewardType.PassiveStat) continue;
                if (node.PassiveStat == PassiveStatType.None) continue;

                if (!stats.ContainsKey(node.PassiveStat))
                    stats[node.PassiveStat] = 0;
                stats[node.PassiveStat] += node.PassiveValue;
            }
            return stats;
        }

        public Dictionary<CraftBonusType, float> GetActiveCraftBonuses()
        {
            var bonuses = new Dictionary<CraftBonusType, float>();
            var activeId = _model.ActiveMethodId.Value;
            if (string.IsNullOrEmpty(activeId)) return bonuses;

            var method = GetMethodConfig(activeId);
            if (method?.Nodes == null) return bonuses;

            foreach (var node in method.Nodes)
            {
                if (!_model.UnlockedNodeIds.Contains(node.NodeId)) continue;
                if (node.RewardType != NodeRewardType.CraftBonus) continue;
                if (node.CraftBonusType == CraftBonusType.None) continue;

                if (!bonuses.ContainsKey(node.CraftBonusType))
                    bonuses[node.CraftBonusType] = 0;
                bonuses[node.CraftBonusType] += node.CraftBonusValue;
            }
            return bonuses;
        }
        #endregion

        #region 神通
        public void LearnAbility(string abilityId)
        {
            if (_model.LearnedAbilityIds.Contains(abilityId)) return;
            _model.LearnedAbilityIds.Add(abilityId);
            Debug.Log($"[Cultivation] Learned ability: {abilityId}");
        }

        public bool EquipAbility(string abilityId)
        {
            if (!_model.LearnedAbilityIds.Contains(abilityId)) return false;
            if (_model.EquippedAbilityIds.Contains(abilityId)) return false;

            var ability = GetAbilityConfig(abilityId);
            if (ability == null) return false;

            if (GetTotalEquippedEnergy() + ability.EnergyCost > _model.MaxAbilityEnergy)
                return false;

            _model.EquippedAbilityIds.Add(abilityId);
            Debug.Log($"[Cultivation] Equipped ability: {abilityId} (cost {ability.EnergyCost})");
            return true;
        }

        public bool UnequipAbility(string abilityId)
        {
            var removed = _model.EquippedAbilityIds.Remove(abilityId);
            if (removed) Debug.Log($"[Cultivation] Unequipped ability: {abilityId}");
            return removed;
        }

        public int GetTotalEquippedEnergy()
        {
            int total = 0;
            foreach (var id in _model.EquippedAbilityIds)
            {
                var ability = GetAbilityConfig(id);
                if (ability != null) total += ability.EnergyCost;
            }
            return total;
        }

        public List<DivineAbilityData> GetLearnedAbilities()
            => _allAbilities?.FindAll(a => _model.LearnedAbilityIds.Contains(a.AbilityId))
                ?? new List<DivineAbilityData>();

        public List<DivineAbilityData> GetEquippedAbilities()
            => _allAbilities?.FindAll(a => _model.EquippedAbilityIds.Contains(a.AbilityId))
                ?? new List<DivineAbilityData>();

        public DivineAbilityData GetAbilityConfig(string abilityId)
            => _allAbilities?.Find(a => a.AbilityId == abilityId);
        #endregion

        #region 参悟点
        public void AddComprehensionPoints(int amount)
        {
            if (amount <= 0) return;
            _model.ComprehensionPoints.Value += amount;
            Debug.Log($"[Cultivation] +{amount} comprehension (total: {_model.ComprehensionPoints.Value})");
        }

        public bool SpendComprehensionPoints(int amount)
        {
            if (_model.ComprehensionPoints.Value < amount) return false;
            _model.ComprehensionPoints.Value -= amount;
            return true;
        }
        #endregion

        #region 掉落
        public bool TryAcquireAbilityBook(string abilityId)
        {
            if (_model.AcquiredAbilityBookIds.Contains(abilityId)) return false;
            _model.AcquiredAbilityBookIds.Add(abilityId);
            LearnAbility(abilityId);
            Debug.Log($"[Cultivation] Acquired ability book: {abilityId}");
            return true;
        }

        public bool TryAcquireMethodFragment(string methodId)
        {
            if (_model.AcquiredMethodFragmentIds.Contains(methodId)) return false;
            _model.AcquiredMethodFragmentIds.Add(methodId);
            LearnMethod(methodId);
            Debug.Log($"[Cultivation] Acquired method fragment: {methodId}");
            return true;
        }
        #endregion

        #region Helpers
        private (CultivationMethodData, CultivationNodeData) FindNode(string nodeId)
        {
            if (_allMethods == null) return (null, null);
            foreach (var m in _allMethods)
            {
                var n = m.GetNode(nodeId);
                if (n != null) return (m, n);
            }
            return (null, null);
        }
        #endregion
    }
}
