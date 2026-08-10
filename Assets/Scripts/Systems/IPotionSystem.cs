using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 药水系统 — 管理药水获取/使用/丢弃
    /// </summary>
    public interface IPotionSystem : ISystem
    {
        bool ObtainPotion(PotionData potion);
        void UsePotion(int slotIndex, PotionUseContext context);
        void DiscardPotion(int slotIndex);
        /// <summary>清空所有药水（存档恢复用）</summary>
        void ClearPotions();
    }
}
