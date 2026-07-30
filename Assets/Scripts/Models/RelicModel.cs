using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    public class RelicModel : AbstractModel, IRelicModel
    {
        public List<RelicInstance> OwnedRelics { get; } = new List<RelicInstance>();
        public BindableProperty<int> RelicCount { get; } = new BindableProperty<int>(0);

        protected override void OnInit()
        {
        }
    }
}
