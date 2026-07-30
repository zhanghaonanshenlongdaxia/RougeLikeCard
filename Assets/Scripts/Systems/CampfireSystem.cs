using QFramework;
using UnityEngine;

namespace CardGame
{
    public class CampfireSystem : AbstractSystem, ICampfireSystem
    {
        protected override void OnInit()
        {
        }

        public void Rest()
        {
            var ally = NueGames.NueDeck.Scripts.Managers.CombatManager.Instance?.CurrentMainAlly;
            if (ally == null)
            {
                // 非战斗场景，直接修改 PersistentGameplayData
                var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
                if (gm == null) return;
                foreach (var allyBase in gm.PersistentGameplayData.AllyList)
                {
                    var maxHp = allyBase.AllyCharacterData.MaxHealth;
                    var healAmount = Mathf.RoundToInt(maxHp * 0.3f);
                    gm.PersistentGameplayData.SetAllyHealthData(
                        allyBase.AllyCharacterData.CharacterID,
                        Mathf.Min(allyBase.CharacterStats.CurrentHealth + healAmount, maxHp),
                        maxHp);
                }
                Debug.Log("[Campfire] Rested, healed 30% HP");
                return;
            }

            // 战斗场景
            var maxHealth = ally.CharacterStats.MaxHealth;
            var heal = Mathf.RoundToInt(maxHealth * 0.3f);
            ally.CharacterStats.Heal(heal);
            Debug.Log($"[Campfire] Rested, healed {heal} HP");
        }

        public bool UpgradeCard(NueGames.NueDeck.Scripts.Data.Collection.CardData card)
        {
            if (card == null || card.IsUpgraded) return false;
            if (!card.HasUpgradeData) return false;

            card.Upgrade();
            Debug.Log($"[Campfire] Upgraded card: {card.CardName}");
            return true;
        }
    }
}
