using QFramework;
using UnityEngine;

namespace CardGame
{
    public class EvacuateSystem : AbstractSystem, IEvacuateSystem
    {
        protected IInventoryModel InventoryModel => this.GetModel<IInventoryModel>();
        protected IInventorySystem InventorySystem => this.GetSystem<IInventorySystem>();

        protected override void OnInit()
        {
        }

        public void OnCheckpointCleared()
        {
            Debug.Log("[Evacuate] 大关卡通关! 可选择撤离或继续");
        }

        public void Evacuate()
        {
            // 将背包物品全部转入安全箱
            var model = InventoryModel;
            var slotsCopy = new System.Collections.Generic.List<InventorySlot>(model.Slots);
            foreach (var slot in slotsCopy)
            {
                if (slot == null || slot.IsEmpty) continue;
                InventorySystem.TransferToSafeBox(slot.item.ItemId, slot.count);
            }
            Debug.Log("[Evacuate] 御剑归去! 物品已转入乾坤袋");
        }

        public void ContinueAdventure()
        {
            Debug.Log("[Evacuate] 继续历练! 背包物品保留");
        }

        public void OnDeath()
        {
            InventorySystem.ClearOnDeath();
            Debug.Log("[Evacuate] 玩家陨落! 背包物品全部丢失，乾坤袋物品保留");
        }
    }
}
