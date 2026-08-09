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
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            var pd = gm.PersistentGameplayData;

            // 优先使用 AllyHealthDataList（非战斗场景的标准数据源）
            if (pd.AllyHealthDataList != null && pd.AllyHealthDataList.Count > 0)
            {
                // 复制一份避免遍历时SetAllyHealthData修改原List
                var snapshot = new System.Collections.Generic.List<NueGames.NueDeck.Scripts.Characters.AllyHealthData>(pd.AllyHealthDataList);
                foreach (var hd in snapshot)
                {
                    var heal = Mathf.RoundToInt(hd.MaxHealth * 0.3f);
                    hd.CurrentHealth = Mathf.Min(hd.CurrentHealth + heal, hd.MaxHealth);
                    pd.SetAllyHealthData(hd.CharacterId, hd.CurrentHealth, hd.MaxHealth);
                }
                Debug.Log("[Campfire] Rested, healed 30% HP (via AllyHealthDataList)");
                return;
            }

            // fallback: 战斗场景用 CombatManager
            var ally = NueGames.NueDeck.Scripts.Managers.CombatManager.Instance?.CurrentMainAlly;
            if (ally != null)
            {
                var maxHealth = ally.CharacterStats.MaxHealth;
                var heal = Mathf.RoundToInt(maxHealth * 0.3f);
                ally.CharacterStats.Heal(heal);
                Debug.Log($"[Campfire] Rested, healed {heal} HP (via CombatManager)");
                return;
            }

            // fallback: AllyList（prefab数据，CharacterStats可能为null）
            foreach (var allyBase in pd.AllyList)
            {
                if (allyBase == null || allyBase.AllyCharacterData == null) continue;
                var maxHp = allyBase.AllyCharacterData.MaxHealth;
                var currentHp = allyBase.CharacterStats?.CurrentHealth ?? maxHp;
                var healAmount = Mathf.RoundToInt(maxHp * 0.3f);
                pd.SetAllyHealthData(
                    allyBase.AllyCharacterData.CharacterID,
                    Mathf.Min(currentHp + healAmount, maxHp),
                    maxHp);
            }
            Debug.Log("[Campfire] Rested, healed 30% HP (via AllyList)");
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
