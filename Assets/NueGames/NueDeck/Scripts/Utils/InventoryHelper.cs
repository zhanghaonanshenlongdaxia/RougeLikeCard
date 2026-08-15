using System;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.UI;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Utils
{
    public class InventoryHelper : MonoBehaviour
    {
        [SerializeField] private InventoryTypes inventoryType;
        
        public void OpenInventory()
        {
            if (CollectionManager.Instance == null) return;

            var pileView = PileViewCanvas.GetOrCreate();

            switch (inventoryType)
            {
                case InventoryTypes.CurrentDeck:
                    pileView.Show(GameManager.Instance.PersistentGameplayData.CurrentCardsList, "当前卡组");
                    break;
                case InventoryTypes.DrawPile:
                    pileView.Show(CollectionManager.Instance.DrawPile, "抽牌堆");
                    break;
                case InventoryTypes.DiscardPile:
                    pileView.Show(CollectionManager.Instance.DiscardPile, "弃牌堆");
                    break;
                case InventoryTypes.ExhaustPile:
                    pileView.Show(CollectionManager.Instance.ExhaustPile, "消耗堆");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
    }
}
