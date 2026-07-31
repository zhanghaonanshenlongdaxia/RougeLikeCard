using System.Collections.Generic;
using QFramework;
using NueGames.NueDeck.Scripts.Data.Collection;

namespace CardGame
{
    /// <summary>
    /// 编队数据模型（出征选卡）
    /// </summary>
    public interface ILoadoutModel : IModel
    {
        /// <summary>基础卡牌列表（本命功法，不占神识）</summary>
        List<string> BasicCardIds { get; }

        /// <summary>已选自选卡牌列表</summary>
        List<string> SelectedCardIds { get; }

        /// <summary>当前神识消耗</summary>
        BindableProperty<int> CurrentShenShi { get; }

        /// <summary>神识上限</summary>
        BindableProperty<int> MaxShenShi { get; }

        /// <summary>地图最低神识要求</summary>
        int MinShenShiRequired { get; set; }
    }
}
