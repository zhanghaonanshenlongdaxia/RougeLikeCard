using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 世界地图状态模型
    /// </summary>
    public interface IWorldMapModel : IModel
    {
        /// <summary>玩家当前所在地点ID（移动中=目的地ID）</summary>
        BindableProperty<string> CurrentLocationId { get; }

        /// <summary>是否正在移动（沿路径旅行中）</summary>
        BindableProperty<bool> IsTraveling { get; }

        /// <summary>已解锁的地点ID集合</summary>
        HashSet<string> UnlockedLocationIds { get; }

        /// <summary>玩家是否拥有御剑飞行能力（身法类功法解锁后置true）</summary>
        BindableProperty<bool> CanFly { get; }
    }

    public class WorldMapModel : AbstractModel, IWorldMapModel
    {
        public BindableProperty<string> CurrentLocationId { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> IsTraveling { get; } = new BindableProperty<bool>(false);
        public HashSet<string> UnlockedLocationIds { get; } = new HashSet<string>();
        public BindableProperty<bool> CanFly { get; } = new BindableProperty<bool>(false);

        protected override void OnInit()
        {
        }
    }
}
