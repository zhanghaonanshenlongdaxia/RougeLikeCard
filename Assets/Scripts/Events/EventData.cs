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
        // ── 血量相关 (6) ──
        Heal,               // 回血
        FullHeal,           // 完全回血
        TakeDamage,         // 受伤
        GainMaxHP,          // 增加最大HP
        LoseMaxHP,          // 减少最大HP
        CleanseAll,         // 清除所有负面状态
        // ── 资源相关 (9) ──
        GainGold,           // 获得金币
        LoseGold,           // 失去金币
        GainMaterial,        // 获得灵材（随机品阶）
        LoseMaterial,        // 失去一个随机灵材
        UnlockRecipe,        // 解锁随机配方
        GainGoldByMaterial,  // 按持有灵材数获灵石
        TradeHpForGold,     // 用HP换灵石
        TradeGoldForHp,     // 用灵石回血
        RandomGold,         // 随机灵石收益
        // ── 神识/法力 (6) ──
        GainShenShi,        // 增加神识上限（永久）
        LoseShenShi,        // 减少神识上限（永久）
        DrawBonus,          // 下场多抽N张牌
        ManaBonus,          // 下场多N点法力
        GainMaxMana,        // 永久增加法力上限
        LoseMaxMana,        // 永久减少法力上限
        // ── 战斗状态 (9) ──
        GainStrength,       // 获得力量（下场战斗生效）
        GainDexterity,      // 获得敏捷（下场战斗生效）
        GainWeak,           // 下场给敌人施虚弱
        GainFrail,          // 下场给敌人施脆弱
        GainVulnerable,     // 下场给敌人施易伤
        GainThorn,          // 下场自身获得反伤
        GainBlockStart,     // 下场开局获得格挡
        EnemyHpReduce,      // 下场敌人血量降低
        RandomDamage,       // 随机受伤
        // ── 卡牌相关 (10) ──
        GainCard,           // 获得卡牌
        RemoveCard,         // 移除卡牌
        DuplicateCard,      // 复制一张随机卡
        TransformCard,      // 随机变形一张卡
        GainPathCard,       // 获得指定路径的随机卡
        GainRarityCard,     // 获得指定品阶的随机卡
        UpgradeRandomCard,  // 随机升级一张卡
        DowngradeRandomCard,// 随机降级一张卡
        ExhaustCard,        // 永久消耗一张卡
        TradeCardForMaterial,// 用一张卡换灵材
        // ── 道具 (4) ──
        GainRelic,          // 获得遗物
        LoseRelic,          // 失去遗物
        GainPotion,         // 获得药水
        LosePotion,         // 失去药水
        // ── 赌博 (1) ──
        DoubleOrNothing,    // 全或无赌注
        // ── 小游戏 (10) ──
        MiniSlot,           // 灵石机（老虎机）
        MiniDice,           // 掷骰问运
        MiniPinball,        // 灵珠弹射
        MiniRingToss,       // 套灵兽
        MiniBalloon,        // 灵气泡
        MiniLottery,        // 仙缘抽签
        MiniWheel,          // 命运转盘
        MiniCoinFlip,       // 灵币翻面
        MiniCardGuess,      // 猜牌大小
        MiniTreasureHunt,   // 寻宝迷踪
        // ── 无效果 ──
        Nothing,            // 无效果
        // ── 功法系统 (3) ──
        GainComprehension,   // 获得参悟点
        GainAbilityBook,     // 获得神通书籍 (不可重复)
        GainMethodFragment   // 获得功法残篇 (不可重复)
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

        [Header("境界限制")]
        [Tooltip("该事件所属境界（事件只在对应境界出现）")]
        public RealmLevel requiredRealm = RealmLevel.LianQi;

        [Header("选项")]
        public List<EventChoice> choices = new List<EventChoice>();
    }
}
