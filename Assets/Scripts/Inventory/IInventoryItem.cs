using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 背包物品类型
    /// </summary>
    public enum InventoryItemType
    {
        Material,  // 材料
        Card,      // 卡牌
        Relic,     // 遗物/法宝
        Potion,    // 药水/丹药
        Special    // 特殊物品
    }

    /// <summary>
    /// 可放入背包的物品统一接口
    /// </summary>
    public interface IInventoryItem
    {
        string ItemId { get; }
        string ItemName { get; }
        string ItemDescription { get; }
        Sprite ItemIcon { get; }
        int ItemWeight { get; }
        bool IsStackable { get; }
        int MaxStack { get; }
    }

    /// <summary>
    /// 背包中的物品堆叠实例
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        public IInventoryItem item;
        public int count;

        public InventorySlot(IInventoryItem item, int count = 1)
        {
            this.item = item;
            this.count = count;
        }

        public int Weight => (item?.ItemWeight ?? 0) * count;
        public bool IsEmpty => item == null || count <= 0;

        public bool CanAdd(int amount)
        {
            if (item == null || !item.IsStackable) return false;
            return count + amount <= item.MaxStack;
        }

        public void Add(int amount) => count += amount;
        public void Remove(int amount) => count = Mathf.Max(0, count - amount);
    }
}
