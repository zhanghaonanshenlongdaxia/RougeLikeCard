using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 商店数据（运行时生成）
    /// </summary>
    public class ShopData
    {
        public List<NueGames.NueDeck.Scripts.Data.Collection.CardData> cardSlots = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>();
        public List<int> cardPrices = new List<int>();
        public List<RelicData> relicSlots = new List<RelicData>();
        public List<int> relicPrices = new List<int>();
        public List<PotionData> potionSlots = new List<PotionData>();
        public List<int> potionPrices = new List<int>();
        public int removeCardPrice = 75;
    }

    /// <summary>
    /// 商店系统接口
    /// </summary>
    public interface IShopSystem : QFramework.ISystem
    {
        ShopData GenerateShop();
        bool BuyCard(int slotIndex);
        bool BuyRelic(int slotIndex);
        bool BuyPotion(int slotIndex);
        bool RemoveCard(NueGames.NueDeck.Scripts.Data.Collection.CardData card);
        ShopData CurrentShop { get; }
    }
}
