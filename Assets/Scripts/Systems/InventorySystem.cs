using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;

namespace CardGame
{
    public class InventorySystem : AbstractSystem, IInventorySystem
    {
        protected IInventoryModel InventoryModel => this.GetModel<IInventoryModel>();

        protected override void OnInit()
        {
        }

        public bool AddItem(IInventoryItem item, int count = 1)
        {
            if (item == null || count <= 0) return false;

            var model = InventoryModel;

            // 检查负重
            int newWeight = model.CurrentWeight.Value + item.ItemWeight * count;
            if (newWeight > model.MaxWeight.Value)
            {
                Debug.Log($"[Inventory] 负重不足: {model.CurrentWeight.Value}+{item.ItemWeight * count} > {model.MaxWeight.Value}");
                return false;
            }

            // 可堆叠物品：尝试找已有槽位
            if (item.IsStackable)
            {
                foreach (var slot in model.Slots)
                {
                    if (slot != null && !slot.IsEmpty && slot.item.ItemId == item.ItemId)
                    {
                        int canAdd = Mathf.Min(count, slot.item.MaxStack - slot.count);
                        if (canAdd > 0)
                        {
                            slot.Add(canAdd);
                            count -= canAdd;
                            if (count <= 0)
                            {
                                model.UpdateWeight();
                                return true;
                            }
                        }
                    }
                }
            }

            // 创建新槽位
            while (count > 0)
            {
                int addCount = item.IsStackable ? Mathf.Min(count, item.MaxStack) : 1;
                model.Slots.Add(new InventorySlot(item, addCount));
                count -= addCount;
            }

            model.UpdateWeight();
            return true;
        }

        public bool RemoveItem(string itemId, int count = 1)
        {
            if (!HasItem(itemId, count)) return false;

            var model = InventoryModel;
            for (int i = model.Slots.Count - 1; i >= 0 && count > 0; i--)
            {
                var slot = model.Slots[i];
                if (slot != null && !slot.IsEmpty && slot.item.ItemId == itemId)
                {
                    int remove = Mathf.Min(count, slot.count);
                    slot.Remove(remove);
                    count -= remove;
                    if (slot.IsEmpty)
                        model.Slots.RemoveAt(i);
                }
            }

            model.UpdateWeight();
            return true;
        }

        public bool HasItem(string itemId, int count = 1)
        {
            return GetItemCount(itemId) >= count;
        }

        public List<InventorySlot> GetItemsByType<T>() where T : class, IInventoryItem
        {
            var model = InventoryModel;
            var result = new List<InventorySlot>();
            foreach (var slot in model.Slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item is T)
                    result.Add(slot);
            }
            return result;
        }

        public int GetItemCount(string itemId)
        {
            var model = InventoryModel;
            int total = 0;
            foreach (var slot in model.Slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item.ItemId == itemId)
                    total += slot.count;
            }
            return total;
        }

        public bool TransferToSafeBox(string itemId, int count = 1)
        {
            if (!HasItem(itemId, count)) return false;

            var model = InventoryModel;

            // 检查安全箱容量
            int safeBoxCount = 0;
            foreach (var slot in model.SafeBoxSlots)
                if (slot != null && !slot.IsEmpty) safeBoxCount++;

            // 安全箱按槽位计，不按堆叠
            // 这里简化：直接转移
            RemoveItem(itemId, count);

            // 添加到安全箱
            // 找到要转移的item引用
            IInventoryItem item = null;
            foreach (var slot in model.Slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item.ItemId == itemId)
                {
                    item = slot.item;
                    break;
                }
            }

            // 如果背包里没有了，从安全箱已有槽位找
            if (item == null)
            {
                foreach (var slot in model.SafeBoxSlots)
                {
                    if (slot != null && !slot.IsEmpty && slot.item.ItemId == itemId)
                    {
                        item = slot.item;
                        break;
                    }
                }
            }

            if (item == null) return false;

            // 可堆叠：尝试合并
            if (item.IsStackable)
            {
                foreach (var slot in model.SafeBoxSlots)
                {
                    if (slot != null && !slot.IsEmpty && slot.item.ItemId == itemId)
                    {
                        int canAdd = Mathf.Min(count, item.MaxStack - slot.count);
                        if (canAdd > 0)
                        {
                            slot.Add(canAdd);
                            count -= canAdd;
                            if (count <= 0) return true;
                        }
                    }
                }
            }

            // 新槽位
            while (count > 0)
            {
                int addCount = item.IsStackable ? Mathf.Min(count, item.MaxStack) : 1;
                model.SafeBoxSlots.Add(new InventorySlot(item, addCount));
                count -= addCount;
            }

            return true;
        }

        public bool TransferFromSafeBox(string itemId, int count = 1)
        {
            var model = InventoryModel;

            // 检查安全箱是否有
            int safeCount = 0;
            IInventoryItem item = null;
            foreach (var slot in model.SafeBoxSlots)
            {
                if (slot != null && !slot.IsEmpty && slot.item.ItemId == itemId)
                {
                    safeCount += slot.count;
                    if (item == null) item = slot.item;
                }
            }

            if (safeCount < count || item == null) return false;

            // 从安全箱移除
            for (int i = model.SafeBoxSlots.Count - 1; i >= 0 && count > 0; i--)
            {
                var slot = model.SafeBoxSlots[i];
                if (slot != null && !slot.IsEmpty && slot.item.ItemId == itemId)
                {
                    int remove = Mathf.Min(count, slot.count);
                    slot.Remove(remove);
                    count -= remove;
                    if (slot.IsEmpty)
                        model.SafeBoxSlots.RemoveAt(i);
                }
            }

            // 添加到背包
            AddItem(item, safeCount - count + count); // 简化：重新添加全部取出的
            return true;
        }

        public void ClearOnDeath()
        {
            var model = InventoryModel;
            // 清空背包（基础卡牌由卡牌系统管理，不在背包中）
            model.Slots.Clear();
            model.UpdateWeight();
            Debug.Log("[Inventory] 背包已清空（死亡掉落）");
        }

        public void SortInventory()
        {
            var model = InventoryModel;
            model.Slots.Sort((a, b) =>
            {
                if (a == null || a.IsEmpty) return 1;
                if (b == null || b.IsEmpty) return -1;
                return string.Compare(a.item.ItemId, b.item.ItemId, System.StringComparison.Ordinal);
            });
        }
    }
}
