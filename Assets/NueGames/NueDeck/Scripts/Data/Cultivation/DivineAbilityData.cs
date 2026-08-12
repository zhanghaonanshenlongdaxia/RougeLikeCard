using System;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Cultivation
{
    /// <summary>
    /// 神通数据SO — 独立书籍，冒险中获得，撤离带出后学习转化为卡牌。
    /// 神通本身不包含功法树内的位置/前置关系，这些由功法节点(CultivationNodeData)决定。
    /// </summary>
    [CreateAssetMenu(fileName = "Divine Ability", menuName = "NueDeck/Cultivation/Divine Ability", order = 21)]
    public class DivineAbilityData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string abilityId;
        [SerializeField] private string abilityName;
        [SerializeField][TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Attributes")]
        [SerializeField] private ElementType element;
        [Tooltip("神通品质")]
        [SerializeField] private ItemQuality quality = ItemQuality.LianQi_T1;

        [Header("Card")]
        [Tooltip("学习后转化为的卡牌ID")]
        [SerializeField] private string cardId;
        [Tooltip("带入冒险的能量消耗")]
        [SerializeField] private int energyCost;

        [Header("Unlock")]
        [Tooltip("解锁所需参悟点")]
        [SerializeField] private int comprehensionCost = 5;

        #region Properties
        public string AbilityId => abilityId;
        public string AbilityName => abilityName;
        public string Description => description;
        public Sprite Icon => icon;
        public ElementType Element => element;
        public ItemQuality Quality => quality;
        public string CardId => cardId;
        public int EnergyCost => energyCost;
        public int ComprehensionCost => comprehensionCost;
        #endregion

        #region Editor
#if UNITY_EDITOR
        public void EditAbilityId(string id) => abilityId = id;
        public void EditAbilityName(string name) => abilityName = name;
        public void EditDescription(string desc) => description = desc;
        public void EditIcon(Sprite sprite) => icon = sprite;
        public void EditElement(ElementType el) => element = el;
        public void EditQuality(ItemQuality q) => quality = q;
        public void EditCardId(string id) => cardId = id;
        public void EditEnergyCost(int cost) => energyCost = cost;
        public void EditComprehensionCost(int cost) => comprehensionCost = cost;
#endif
        #endregion
    }
}
