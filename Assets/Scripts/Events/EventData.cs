using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 事件效果类型
    /// </summary>
    public enum EventEffectType
    {
        Heal,               // 回血
        TakeDamage,         // 受伤
        GainGold,           // 获得金币
        LoseGold,           // 失去金币
        GainMaxHP,          // 增加最大HP
        LoseMaxHP,          // 减少最大HP
        GainStrength,       // 获得力量（本局战斗）
        GainCard,           // 获得卡牌
        RemoveCard,         // 移除卡牌
        GainRelic,          // 获得遗物
        GainPotion,         // 获得药水
        UpgradeRandomCard,  // 随机升级一张卡
        Nothing             // 无效果
    }

    /// <summary>
    /// 事件选项
    /// </summary>
    [Serializable]
    public struct EventChoice
    {
        public string choiceText;
        public EventEffectType effectType;
        public int effectValue;
        [Tooltip("获得卡牌时指定的卡牌ID，留空则随机")]
        public string cardId;
    }

    /// <summary>
    /// 随机事件数据（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "NewEvent", menuName = "CardGame/Event")]
    public class EventData : ScriptableObject
    {
        [Header("基础信息")]
        public string eventId;
        public new string name;
        [TextArea(3, 6)] public string description;
        public Sprite eventImage;

        [Header("选项")]
        public List<EventChoice> choices = new List<EventChoice>();
    }
}
