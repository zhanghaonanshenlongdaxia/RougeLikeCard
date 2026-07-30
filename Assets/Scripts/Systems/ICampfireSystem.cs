using QFramework;

namespace CardGame
{
    public enum CampfireAction
    {
        Rest,       // 休息：回复30%最大HP
        Upgrade,    // 升级一张卡牌
        Dig,        // 挖掘：随机获得遗物/药水（需要特定遗物）
    }

    public interface ICampfireSystem : ISystem
    {
        void Rest();
        bool UpgradeCard(NueGames.NueDeck.Scripts.Data.Collection.CardData card);
    }
}
