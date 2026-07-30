using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 遗物数据模型
    /// </summary>
    public interface IRelicModel : IModel
    {
        /// <summary>当前拥有的遗物列表</summary>
        List<RelicInstance> OwnedRelics { get; }

        /// <summary>遗物数量（可绑定监听）</summary>
        BindableProperty<int> RelicCount { get; }
    }

    /// <summary>
    /// 运行时遗物实例
    /// </summary>
    [System.Serializable]
    public class RelicInstance
    {
        public string relicId;
        public RelicData data;
        public int counter;
        public bool isUsed;

        public static RelicInstance FromData(RelicData d)
        {
            return new RelicInstance
            {
                relicId = d.relicId,
                data = d,
                counter = 0,
                isUsed = false
            };
        }
    }
}
