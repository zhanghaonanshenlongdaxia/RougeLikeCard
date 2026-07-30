using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    public class PotionModel : AbstractModel, IPotionModel
    {
        public int MaxPotionSlots { get; set; } = 3;
        public List<PotionData> OwnedPotions { get; } = new List<PotionData>();
        public BindableProperty<int> PotionCount { get; } = new BindableProperty<int>(0);

        protected override void OnInit()
        {
        }
    }
}
