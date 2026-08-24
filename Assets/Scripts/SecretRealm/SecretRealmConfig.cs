using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 秘境配置（ScriptableObject）— 爬塔玩法
    /// 每个秘境N层，每层是一个格子地图（复用GridMap系统）
    /// 层间通过格子地图的"下一层"出口（Exit格 targetId="next_floor"）推进
    /// 定期开放：openDayStart~openDayEnd（游戏总天数窗口）
    /// </summary>
    [CreateAssetMenu(fileName = "SecretRealmConfig", menuName = "CardGame/SecretRealmConfig")]
    public class SecretRealmConfig : ScriptableObject
    {
        [Header("秘境ID/名称")]
        public string realmId;
        public string realmName;
        [TextArea] public string description;

        [Header("开放窗口（游戏总天数；-1=常驻）")]
        public int openDayStart = -1;
        public int openDayEnd = -1;

        [Header("层数配置（从第1层起）")]
        public List<RealmFloor> floors = new List<RealmFloor>();

        [Header("通关奖励（灵石/材料品质）")]
        public int clearGold = 100;

        /// <summary>秘境当前是否开放</summary>
        public bool IsOpen(int totalDays)
        {
            if (openDayStart < 0 || openDayEnd < 0) return true;
            return totalDays >= openDayStart && totalDays <= openDayEnd;
        }

        public RealmFloor GetFloor(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= floors.Count) return null;
            return floors[floorIndex];
        }
    }

    /// <summary>秘境单层</summary>
    [Serializable]
    public class RealmFloor
    {
        [Header("层名（如：落星秘境·一层）")]
        public string floorName;

        [Header("该层格子地图ID")]
        public string gridMapId;

        [Header("层类型")]
        public RealmFloorType floorType = RealmFloorType.Combat;
    }

    public enum RealmFloorType
    {
        Combat = 0,     // 战斗层
        Event = 1,      // 事件/解谜层
        Treasure = 2,   // 宝箱层
        Boss = 3,       // Boss层（通关层）
        Rest = 4        // 休整层
    }
}
