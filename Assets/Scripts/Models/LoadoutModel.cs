using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    public class LoadoutModel : AbstractModel, ILoadoutModel
    {
        public List<string> BasicCardIds { get; } = new List<string> { "1_attack_basic", "3_block_basic" };
        public List<string> SelectedCardIds { get; } = new List<string>();
        public BindableProperty<int> CurrentShenShi { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxShenShi { get; } = new BindableProperty<int>(20);
        public int MinShenShiRequired { get; set; } = 10;

        protected override void OnInit()
        {
        }
    }
}
