using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 境界突破系统：检查条件、消耗资源、提升境界
    /// </summary>
    public interface IRealmSystem : ISystem
    {
        /// <summary>获取当前境界信息</summary>
        RealmBreakthroughRequirement GetCurrentRealmInfo();
        /// <summary>获取下一次突破的需求</summary>
        RealmBreakthroughRequirement GetNextBreakthrough();
        /// <summary>检查是否可以突破</summary>
        bool CanBreakthrough();
        /// <summary>执行突破（消耗资源+提升属性）</summary>
        bool DoBreakthrough();
        /// <summary>获取当前境界名称</summary>
        string GetCurrentRealmName();
    }

    public class RealmSystem : AbstractSystem, IRealmSystem
    {
        private IRealmModel _model;
        private IBattleModel _battleModel;
        private IInventoryModel _inventoryModel;
        private ILoadoutModel _loadoutModel;

        protected override void OnInit()
        {
            _model = this.GetModel<IRealmModel>();
            _battleModel = this.GetModel<IBattleModel>();
            _inventoryModel = this.GetModel<IInventoryModel>();
            _loadoutModel = this.GetModel<ILoadoutModel>();
        }

        public RealmBreakthroughRequirement GetCurrentRealmInfo()
        {
            int current = _model.CurrentRealm.Value;
            if (current == 0) return null; // 练气期没有前置信息
            return _model.RealmTable[current - 1];
        }

        public RealmBreakthroughRequirement GetNextBreakthrough()
        {
            int next = _model.CurrentRealm.Value + 1;
            if (next >= _model.RealmTable.Count + 1) return null; // 已满级
            return _model.RealmTable[next - 1];
        }

        public bool CanBreakthrough()
        {
            var req = GetNextBreakthrough();
            if (req == null) return false;

            // 检查灵石
            if (_battleModel.CurrentGold.Value < req.goldCost) return false;

            // 检查材料（按品阶统计库存中该品阶的材料数量）
            int materialCount = CountMaterialsByRarity(req.materialRarity);
            if (materialCount < req.materialCount) return false;

            // 检查丹药（检查背包中是否有对应ID的物品）
            // TODO: 丹药系统接入后检查 requiredPotionId

            return true;
        }

        public bool DoBreakthrough()
        {
            if (!CanBreakthrough()) return false;

            var req = GetNextBreakthrough();

            // 消耗灵石
            _battleModel.CurrentGold.Value -= req.goldCost;

            // 消耗材料（从背包中移除对应品阶的材料）
            RemoveMaterialsByRarity(req.materialRarity, req.materialCount);

            // 提升境界
            _model.CurrentRealm.Value += 1;
            _model.RealmHpBonus.Value += req.hpBonus;
            _model.RealmShenShiBonus.Value += req.shenShiBonus;

            // 提升神识上限
            _loadoutModel.MaxShenShi.Value += req.shenShiBonus;

            Debug.Log($"[境界] 突破成功！当前境界: {GetCurrentRealmName()} HP+{req.hpBonus} 神识+{req.shenShiBonus}");
            return true;
        }

        public string GetCurrentRealmName()
        {
            int level = _model.CurrentRealm.Value;
            return level switch
            {
                0 => "练气期",
                1 => "筑基期",
                2 => "金丹期",
                3 => "元婴期",
                4 => "化神期",
                5 => "渡劫期",
                _ => "未知"
            };
        }

        private int CountMaterialsByRarity(string rarity)
        {
            // 遍历背包中所有材料，统计指定品阶的数量
            if (rarity == "HeavenlyTreasure")
            {
                // 天材地宝按类型统计
                return _inventoryModel.Slots.Count(s => !s.IsEmpty && s.item is MaterialData m && m.materialType == MaterialType.HeavenlyTreasure);
            }
            else
            {
                var targetRarity = ParseRarity(rarity);
                int count = 0;
                foreach (var slot in _inventoryModel.Slots)
                {
                    if (!slot.IsEmpty && slot.item is MaterialData m && m.rarity == targetRarity)
                        count += slot.count;
                }
                return count;
            }
        }

        private void RemoveMaterialsByRarity(string rarity, int count)
        {
            if (rarity == "HeavenlyTreasure")
            {
                int remaining = count;
                for (int i = _inventoryModel.Slots.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    var slot = _inventoryModel.Slots[i];
                    if (!slot.IsEmpty && slot.item is MaterialData m && m.materialType == MaterialType.HeavenlyTreasure)
                    {
                        _inventoryModel.Slots.RemoveAt(i);
                        remaining--;
                    }
                }
            }
            else
            {
                var targetRarity = ParseRarity(rarity);
                int remaining = count;
                for (int i = _inventoryModel.Slots.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    var slot = _inventoryModel.Slots[i];
                    if (!slot.IsEmpty && slot.item is MaterialData m && m.rarity == targetRarity)
                    {
                        int toRemove = Mathf.Min(slot.count, remaining);
                        slot.count -= toRemove;
                        remaining -= toRemove;
                        if (slot.count <= 0) _inventoryModel.Slots.RemoveAt(i);
                    }
                }
            }
            _inventoryModel.UpdateWeight();
        }

        private MaterialRarity ParseRarity(string s)
        {
            return s switch
            {
                "FanPin" => MaterialRarity.FanPin,
                "LingPin" => MaterialRarity.LingPin,
                "XuanPin" => MaterialRarity.XuanPin,
                "XianPin" => MaterialRarity.XianPin,
                _ => MaterialRarity.FanPin
            };
        }
    }
}
