using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using QFramework;
using NueGames.NueDeck.Scripts.Data.Characters;
using CardGame.Audio;

namespace CardGame
{
    /// <summary>
    /// 战斗启动器 — 任何系统（格子地图/剧情对话/事件）都可直接拉起一场战斗
    /// 不依赖MapManager的节点流程。
    ///
    /// 用法（格子地图）：
    ///   BattleLauncher.StartGridBattle(enemyId, mapId, cellPos)
    /// 战斗胜利 → RewardCanvas"继续" → 自动回到格子地图场景并清除敌人格子
    /// 战斗失败 → 走原有死亡流程（死亡惩罚+回基地）
    /// </summary>
    public static class BattleLauncher
    {
        public const string GridMapSceneName = "5- GridMap";

        // ===== 待处理战斗状态（static，跨场景存活）=====
        public static bool HasPendingBattle { get; private set; }
        public static string GridMapId { get; private set; }
        public static Vector2Int EnemyCell { get; private set; }
        public static string EnemyId { get; private set; }

        /// <summary>战斗胜利且需要格子地图消费的结果</summary>
        static bool _winResultPending;

        // ===================== 发起战斗 =====================

        /// <summary>
        /// 从格子地图发起战斗
        /// </summary>
        public static bool StartGridBattle(string enemyId, string mapId, Vector2Int cell)
        {
            var enemy = FindEnemyById(enemyId);
            if (enemy == null)
            {
                Debug.LogError($"[BattleLauncher] 敌人未找到: {enemyId}");
                return false;
            }

            // 记录战斗来源
            HasPendingBattle = true;
            _winResultPending = false;
            GridMapId = mapId;
            EnemyCell = cell;
            EnemyId = enemyId;

            // 强制指定敌人
            EnemyTestMode.Enabled = true;
            EnemyTestMode.ForcedEnemies = new List<EnemyCharacterData> { enemy };

            // 格子战斗不是Boss战：重置撤离标记（防上一场残留）
            CardGameArchitecture.Interface.GetModel<IBattleModel>().IsFinalEncounter = false;
            var gmData = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gmData != null && gmData.PersistentGameplayData != null)
                gmData.PersistentGameplayData.IsFinalEncounter = false;

            // 确保卡组非空（编辑器直连场景时兜底）
            EnsureDeck();

            // 进战斗场景（复刻SceneChanger.OpenCombatScene的Canvas处理）
            var gm2 = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm2 == null || gm2.SceneData == null)
            {
                Debug.LogError("[BattleLauncher] GameManager未初始化（需从主菜单进入或场景含CoreLoader）");
                return false;
            }

            if (GameAudioManager.Instance != null)
                GameAudioManager.Instance.PlayBGM(SceneBGM.Battle);

            Debug.Log($"[BattleLauncher] 格子战斗: {enemyId} @ {mapId}({cell.x},{cell.y})");
            var ui = NueGames.NueDeck.Scripts.Managers.UIManager.Instance;
            if (ui != null)
            {
                ui.SetCanvas(ui.CombatCanvas, false, true);
                ui.SetCanvas(ui.InformationCanvas, true, false);
                ui.SetCanvas(ui.RewardCanvas, false, true);
                ui.ChangeScene(gm2.SceneData.combatSceneIndex);
            }
            else
            {
                Debug.LogError("[BattleLauncher] UIManager未初始化");
                return false;
            }
            return true;
        }

        /// <summary>通用战斗（无格子来源，如剧情战斗）：胜利后回基地</summary>
        public static bool StartBattle(string enemyId)
        {
            var enemy = FindEnemyById(enemyId);
            if (enemy == null)
            {
                Debug.LogError($"[BattleLauncher] 敌人未找到: {enemyId}");
                return false;
            }

            HasPendingBattle = false;
            _winResultPending = false;

            EnemyTestMode.Enabled = true;
            EnemyTestMode.ForcedEnemies = new List<EnemyCharacterData> { enemy };
            EnsureDeck();

            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null || gm.SceneData == null) return false;

            if (GameAudioManager.Instance != null)
                GameAudioManager.Instance.PlayBGM(SceneBGM.Battle);

            var ui = NueGames.NueDeck.Scripts.Managers.UIManager.Instance;
            if (ui == null) return false;
            ui.SetCanvas(ui.CombatCanvas, false, true);
            ui.SetCanvas(ui.InformationCanvas, true, false);
            ui.SetCanvas(ui.RewardCanvas, false, true);
            ui.ChangeScene(gm.SceneData.combatSceneIndex);
            return true;
        }

        // ===================== 战斗结束回调（由RewardCanvas/CombatManager调用） =====================

        /// <summary>
        /// 战斗胜利，RewardCanvas点击"继续"时调用：
        /// 有格子来源 → 回格子地图；否则回原地图场景
        /// 返回true表示已处理返回（调用方无需再OpenMapScene）
        /// </summary>
        public static bool HandleBattleWinContinue()
        {
            if (!HasPendingBattle)
            {
                ResetEnemyTestMode();
                return false;
            }

            _winResultPending = true;   // 格子地图加载后消费
            ResetEnemyTestMode();

            // 隐藏跨场景存活的战斗Canvas（复刻SceneChanger场景切换的Canvas处理）
            var ui = NueGames.NueDeck.Scripts.Managers.UIManager.Instance;
            if (ui != null)
            {
                ui.SetCanvas(ui.CombatCanvas, false, true);
                ui.SetCanvas(ui.InformationCanvas, false, true);
                ui.SetCanvas(ui.RewardCanvas, false, true);
            }

            SceneManager.LoadScene(GridMapSceneName);
            return true;
        }

        /// <summary>
        /// 战斗失败（死亡流程）：清除待处理状态，走原有死亡流程
        /// </summary>
        public static void HandleBattleLose()
        {
            HasPendingBattle = false;
            _winResultPending = false;
            ResetEnemyTestMode();
        }

        /// <summary>
        /// 格子地图初始化时消费胜利结果：返回敌人格子坐标（玩家战后应站的位置），
        /// 无待消费结果返回null
        /// </summary>
        public static Vector2Int? ConsumeGridWin(string mapId)
        {
            if (!_winResultPending || mapId != GridMapId)
                return null;

            _winResultPending = false;
            HasPendingBattle = false;
            return EnemyCell;
        }

        // ===================== 内部工具 =====================

        static void ResetEnemyTestMode()
        {
            EnemyTestMode.Enabled = false;
        }

        static void EnsureDeck()
        {
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null || gm.PersistentGameplayData == null) return;
            if (gm.PersistentGameplayData.CurrentCardsList.Count > 0) return;

            // 卡组为空 → 构建出征卡组（含初始牌兜底）
            CardGameArchitecture.Interface.GetSystem<ILoadoutSystem>().StartAdventure();
        }

        static EnemyCharacterData FindEnemyById(string enemyId)
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:EnemyCharacterData {enemyId}");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyCharacterData>(path);
                if (enemy != null && enemy.name == enemyId)
                    return enemy;
            }
#endif
            return null;
        }
    }
}
