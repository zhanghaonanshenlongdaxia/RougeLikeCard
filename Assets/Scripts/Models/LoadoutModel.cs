using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    public class LoadoutModel : AbstractModel, ILoadoutModel
    {
        public List<string> BasicCardIds { get; } = new List<string> {
            "1_attack_basic",    // 基础攻击 1费6伤
            "3_block_basic",     // 基础格挡 1费5盾
            "2_attack_fast",     // 迅捷斩 0费4伤
            "5_heal_basic",      // 调息 1费回5HP
            "4_draw_basic",      // 凝神 1费抽2
            "8_skill_earnMana",  // 聚灵 0费+2灵力
        };
        public List<string> SelectedCardIds { get; } = new List<string>();
        public BindableProperty<int> CurrentShenShi { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxShenShi { get; } = new BindableProperty<int>(20);
        public int MinShenShiRequired { get; set; } = 10;

        protected override void OnInit()
        {
        }
    }
}
