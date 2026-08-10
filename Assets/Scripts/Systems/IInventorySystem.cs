using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 背包系统 — 管理物品的添加/移除/转移/掉落
    /// </summary>
    public interface IInventorySystem : ISystem
    {
        /// <summary>添加物品到背包</summary>
        bool AddItem(IInventoryItem item, int count = 1);

        /// <summary>移除物品</summary>
        bool RemoveItem(string itemId, int count = 1);

        /// <summary>检查是否有足够数量</summary>
        bool HasItem(string itemId, int count = 1);

        /// <summary>获取指定类型的所有物品</summary>
        List<InventorySlot> GetItemsByType<T>() where T : class, IInventoryItem;

        /// <summary>获取指定物品数量</summary>
        int GetItemCount(string itemId);

        /// <summary>转移到安全箱</summary>
        bool TransferToSafeBox(string itemId, int count = 1);

        /// <summary>从安全箱取回</summary>
        bool TransferFromSafeBox(string itemId, int count = 1);

        /// <summary>直接添加到安全箱（存档恢复用）</summary>
        void AddToSafeBox(IInventoryItem item, int count);

        /// <summary>死亡掉落：清空背包（保留基础卡牌）</summary>
        void ClearOnDeath();

        /// <summary>排序背包</summary>
        void SortInventory();
    }
}
