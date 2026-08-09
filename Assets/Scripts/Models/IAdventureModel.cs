using QFramework;

namespace CardGame
{
    /// <summary>
    /// 冒险选点数据模型
    /// </summary>
    public interface IAdventureModel : IModel
    {
        /// <summary>当前选中的地图ID</summary>
        string SelectedMapId { get; set; }
        /// <summary>当前选中的难度</summary>
        DifficultyType SelectedDifficulty { get; set; }
        /// <summary>当前编队卡牌总值（所有已选卡牌的神识消耗总和）</summary>
        BindableProperty<int> CurrentDeckValue { get; }
    }

    public class AdventureModel : AbstractModel, IAdventureModel
    {
        public string SelectedMapId { get; set; } = "";
        public DifficultyType SelectedDifficulty { get; set; } = DifficultyType.Normal;
        public BindableProperty<int> CurrentDeckValue { get; } = new BindableProperty<int>(0);

        protected override void OnInit()
        {
        }
    }
}
