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
            RegisterModel<ILoadoutModel>(new LoadoutModel());
            RegisterModel<IEnemyCodexModel>(new EnemyCodexModel());
            RegisterModel<IRealmModel>(new RealmModel());
            RegisterModel<IAdventureModel>(new AdventureModel());
            RegisterModel<IMetaModel>(new MetaModel());
            RegisterModel<IStoryModel>(new StoryModel());
            RegisterModel<ICultivationModel>(new CultivationModel());
            RegisterModel<IEncounterModel>(new EncounterModel());
            RegisterSystem<IBattleSystem>(new BattleSystem());
            RegisterSystem<ICardSystem>(new CardSystem());
            RegisterSystem<IRelicSystem>(new RelicSystem());
            RegisterSystem<IPotionSystem>(new PotionSystem());
            RegisterSystem<IShopSystem>(new ShopSystem());
            RegisterSystem<ICampfireSystem>(new CampfireSystem());
            RegisterSystem<IEventSystem>(new EventSystem());
            RegisterSystem<IInventorySystem>(new InventorySystem());
            RegisterSystem<ILoadoutSystem>(new LoadoutSystem());
            RegisterSystem<ICraftSystem>(new CraftSystem());
            RegisterSystem<IEvacuateSystem>(new EvacuateSystem());
            RegisterSystem<IEnemyCodexSystem>(new EnemyCodexSystem());
            RegisterSystem<IRealmSystem>(new RealmSystem());
            RegisterSystem<IMetaSystem>(new MetaSystem());
            RegisterSystem<IStorySystem>(new StorySystem());
            RegisterSystem<IRitualSystem>(new RitualSystem());
            RegisterSystem<ICultivationSystem>(new CultivationSystem());
        }
    }
}
