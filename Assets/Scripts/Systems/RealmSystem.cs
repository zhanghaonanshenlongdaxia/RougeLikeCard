using System.Collections.Generic;
using System.Linq;
using NueGames.NueDeck.Scripts.Enums;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 境界突破系统：检查条件、消耗资源、提升境界
    /// </summary>
    public interface IRealmSystem : ISystem
    {
        RealmBreakthroughRequirement GetCurrentRealmInfo();
        RealmBreakthroughRequirement GetNextBreakthrough();
        bool CanBreakthrough();
        bool DoBreakthrough();
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
            if (current == 0) return null;
            return _model.RealmTable[current - 1];
        }

        public RealmBreakthroughRequirement GetNextBreakthrough()
        {
            int next = _model.CurrentRealm.Value + 1;
            if (next >= _model.RealmTable.Count + 1) return null;
            return _model.RealmTable[next - 1];
        }

        public bool CanBreakthrough()
        {
            var req = GetNextBreakthrough();
            if (req == null) return false;
            if (_battleModel.CurrentGold.Value < req.goldCost) return false;
            int materialCount = CountMaterialsByQuality(req.requiredQuality);
            if (materialCount < req.materialCount) return false;
            return true;
        }

        public bool DoBreakthrough()
        {
            if (!CanBreakthrough()) return false;
            var req = GetNextBreakthrough();
            _battleModel.CurrentGold.Value -= req.goldCost;
            RemoveMaterialsByQuality(req.requiredQuality, req.materialCount);
            _model.CurrentRealm.Value += 1;
            _model.RealmHpBonus.Value += req.hpBonus;
            _model.RealmShenShiBonus.Value += req.shenShiBonus;
            _loadoutModel.MaxShenShi.Value += req.shenShiBonus;
            Debug.Log($"[境界] 突破成功！当前境界: {GetCurrentRealmName()} HP+{req.hpBonus} 神识+{req.shenShiBonus}");
            return true;
        }

        public string GetCurrentRealmName()
        {
            int level = _model.CurrentRealm.Value;
            return level switch
            {
                0 => "练气期", 1 => "筑基期", 2 => "金丹期",
                3 => "元婴期", 4 => "化神期", 5 => "渡劫期", _ => "未知"
            };
        }

        private int CountMaterialsByQuality(ItemQuality quality)
        {
            int count = 0;
            foreach (var slot in _inventoryModel.Slots)
            {
                if (!slot.IsEmpty && slot.item is MaterialData m && m.quality == quality)
                    count += slot.count;
            }
            return count;
        }

        private void RemoveMaterialsByQuality(ItemQuality quality, int count)
        {
            int remaining = count;
            for (int i = _inventoryModel.Slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = _inventoryModel.Slots[i];
                if (!slot.IsEmpty && slot.item is MaterialData m && m.quality == quality)
                {
                    int toRemove = Mathf.Min(slot.count, remaining);
                    slot.count -= toRemove;
                    remaining -= toRemove;
                    if (slot.count <= 0) _inventoryModel.Slots.RemoveAt(i);
                }
            }
            _inventoryModel.UpdateWeight();
        }
    }
}
