using QFramework;

namespace CardGame
{
    /// <summary>
    /// 卡牌游戏架构入口，注册所有 Model / System / Utility
    /// </summary>
    public class CardGameArchitecture : Architecture<CardGameArchitecture>
    {
        protected override void Init()
        {
            RegisterModel<IBattleModel>(new BattleModel());
            RegisterModel<ICardModel>(new CardModel());
            RegisterSystem<IBattleSystem>(new BattleSystem());
            RegisterSystem<ICardSystem>(new CardSystem());
        }
    }
}
