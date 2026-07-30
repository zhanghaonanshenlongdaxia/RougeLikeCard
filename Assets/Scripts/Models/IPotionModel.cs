using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 药水数据模型
    /// </summary>
    public interface IPotionModel : IModel
    {
        /// <summary>最大药水槽位数</summary>
        int MaxPotionSlots { get; set; }

        /// <summary>当前拥有的药水列表（null表示空槽）</summary>
        List<PotionData> OwnedPotions { get; }

        /// <summary>药水数量</summary>
        BindableProperty<int> PotionCount { get; }
    }
}
