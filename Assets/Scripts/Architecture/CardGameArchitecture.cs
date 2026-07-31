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
            RegisterModel<IRelicModel>(new RelicModel());
            RegisterModel<IPotionModel>(new PotionModel());
            RegisterModel<IMapModel>(new MapModel());
            RegisterModel<IInventoryModel>(new InventoryModel());
            RegisterSystem<IBattleSystem>(new BattleSystem());
            RegisterSystem<ICardSystem>(new CardSystem());
            RegisterSystem<IRelicSystem>(new RelicSystem());
            RegisterSystem<IPotionSystem>(new PotionSystem());
            RegisterSystem<IShopSystem>(new ShopSystem());
            RegisterSystem<ICampfireSystem>(new CampfireSystem());
            RegisterSystem<IEventSystem>(new EventSystem());
            RegisterSystem<IInventorySystem>(new InventorySystem());
        }
    }
}
