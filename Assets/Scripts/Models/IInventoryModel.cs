using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 背包数据模型（储物袋）
    /// </summary>
    public interface IInventoryModel : IModel
    {
        /// <summary>背包物品槽位列表</summary>
        List<InventorySlot> Slots { get; }

        /// <summary>安全箱物品槽位列表（乾坤袋）</summary>
        List<InventorySlot> SafeBoxSlots { get; }

        /// <summary>当前负重</summary>
        BindableProperty<int> CurrentWeight { get; }

        /// <summary>最大负重</summary>
        BindableProperty<int> MaxWeight { get; }

        /// <summary>安全箱最大容量</summary>
        int SafeBoxCapacity { get; set; }

        /// <summary>重新计算负重</summary>
        void UpdateWeight();
    }
}
