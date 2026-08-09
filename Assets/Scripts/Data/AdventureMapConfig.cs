using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 难度类型
    /// </summary>
    public enum DifficultyType
    {
        Normal = 0,     // 普通
        Hard = 1,       // 困难
        Extreme = 2,    // 极难
        Hell = 3        // 地狱
    }

    /// <summary>
    /// 单个难度配置
    /// </summary>
    [Serializable]
    public class AdventureDifficulty
    {
        public DifficultyType difficultyType;
        public string difficultyName;           // 普通/困难/极难/地狱
        [TextArea] public string description;   // 难度描述
        public int requiredShenShi;             // 出发所需神识总值
        public int mapFloors;                   // 地图层数
        public int mapColumns;                  // 地图列数
        public float enemyHpMultiplier = 1f;    // 敌人HP倍率
        public float enemyDamageMultiplier = 1f;// 敌人攻击倍率
        public float eliteChance = 0.2f;        // 精英出现率
        public int bossPhaseCount = 1;          // Boss阶段数
        public float lootMultiplier = 1f;       // 掉落数量倍率
        public int lootRarityBonus = 0;         // 掉落品阶提升(0=凡品,1=灵品,2=玄品,3=仙品)
        public int goldRewardMultiplier = 1;    // 金币奖励倍率
    }

    /// <summary>
    /// 单个冒险地图数据
    /// </summary>
    [Serializable]
    public class AdventureMapData
    {
        public string mapId;
        public string mapName;                  // 山野荒原/幽冥秘境/万蛊沼泽/天魔裂隙
        [TextArea] public string description;
        public int regionId;                    // 对应敌人regionId
        public int unlockRealmLevel = 0;        // 解锁所需境界(RealmLevel的int值)
        public List<AdventureDifficulty> difficulties = new List<AdventureDifficulty>();
    }

    /// <summary>
    /// 全局冒险地图配置表（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "AdventureMapConfig", menuName = "CardGame/AdventureMapConfig")]
    public class AdventureMapConfig : ScriptableObject
    {
        public List<AdventureMapData> maps = new List<AdventureMapData>();

        /// <summary>根据mapId获取地图数据</summary>
        public AdventureMapData GetMap(string mapId)
        {
            return maps.Find(m => m.mapId == mapId);
        }

        /// <summary>获取当前境界解锁的所有地图</summary>
        public List<AdventureMapData> GetUnlockedMaps(int currentRealm)
        {
            return maps.FindAll(m => currentRealm >= m.unlockRealmLevel);
        }
    }
}
