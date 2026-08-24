using QFramework;
using UnityEngine;
using NueGames.NueDeck.Scripts.Data.Characters;
using CardGame.UI;

namespace CardGame
{
    /// <summary>
    /// 格子交互处理器 — 挂在GridMapCanvas上，监听格子交互事件执行数据层效果
    /// 采集→加材料；敌人→战斗（待BattleLauncher接入）；NPC/剧情/建筑/撤离→占位提示
    /// </summary>
    public class GridInteractionHandler : MonoBehaviour, IController
    {
        void OnEnable()
        {
            GridInteractionEvents.OnCellInteract += HandleInteract;
        }

        void OnDisable()
        {
            GridInteractionEvents.OnCellInteract -= HandleInteract;
        }

        void HandleInteract(GridCell cell)
        {
            switch (cell.interactType)
            {
                case GridInteractType.Gather:
                    HandleGather(cell);
                    break;

                case GridInteractType.Enemy:
                    HandleEnemy(cell);
                    break;

                case GridInteractType.Npc:
                    Debug.Log($"[GridInteract] NPC对话(条件路由): {cell.interactTargetId}");
                    Story.StoryService.StartNpcDialogue(cell.interactTargetId);
                    GridInteractionEvents.TriggerInteractionComplete(cell, "npc");
                    break;

                case GridInteractType.StoryTrigger:
                    Debug.Log($"[GridInteract] 剧情触发: {cell.interactTargetId}");
                    Story.StoryService.StartDialogue(cell.interactTargetId);
                    GridInteractionEvents.TriggerInteractionComplete(cell, "story");
                    break;

                case GridInteractType.Building:
                    Debug.Log($"[GridInteract] 建筑调查: {cell.interactTargetId}");
                    FloatingTip.Show("调查建筑（待接入）");
                    GridInteractionEvents.TriggerInteractionComplete(cell, "building");
                    break;

                case GridInteractType.Exit:
                    Debug.Log("[GridInteract] 到达撤离点");
                    HandleExit(cell);
                    GridInteractionEvents.TriggerInteractionComplete(cell, "exit");
                    break;
            }
        }

        /// <summary>采集 → 获得材料</summary>
        void HandleGather(GridCell cell)
        {
            var materials = ResourceCache.GetMaterials();
            var material = materials.Find(m => m.name == cell.interactTargetId || m.ItemName == cell.interactTargetId);

            if (material != null)
            {
                var ok = this.GetSystem<IInventorySystem>().AddItem(material, 1);
                if (ok)
                {
                    Debug.Log($"[GridInteract] 采集成功: {material.ItemName}");
                    FloatingTip.ShowPurchaseSuccess(material.ItemName);
                }
                else
                {
                    Debug.LogWarning("[GridInteract] 采集失败：放入储物袋失败（可能超重）");
                    FloatingTip.ShowWarning("储物袋已满，采集失败");
                    return;   // 不消耗采集点
                }
            }
            else
            {
                Debug.LogWarning($"[GridInteract] 材料未找到: {cell.interactTargetId}");
                FloatingTip.ShowWarning("采集失败：材料未找到");
            }

            // 消耗采集点
            cell.interactType = GridInteractType.None;
            GridInteractionEvents.TriggerInteractionComplete(cell, "gather:" + (material != null ? material.ItemName : cell.interactTargetId));
        }

        /// <summary>敌人 → 进入战斗</summary>
        void HandleEnemy(GridCell cell)
        {
            var enemy = LoadEnemyById(cell.interactTargetId);
            var enemyName = enemy != null ? enemy.CharacterName : cell.interactTargetId;
            Debug.Log($"[GridInteract] 遭遇敌人: {enemyName} → 拉起战斗");

            var mapCtrl = GetComponent<GridMapUIController>();
            var launched = BattleLauncher.StartGridBattle(cell.interactTargetId, mapCtrl.MapData.mapId, new Vector2Int(cell.x, cell.y));
            if (!launched)
                FloatingTip.ShowWarning($"无法进入战斗：{enemyName} 未找到");

            GridInteractionEvents.TriggerInteractionComplete(cell, "enemy");
        }

        EnemyCharacterData LoadEnemyById(string enemyId)
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

        /// <summary>
        /// 撤离/出口 — 三种路由：
        /// 秘境内("next_floor"=下一层 / "realm_clear"=通关) / 普通(回世界地图)
        /// </summary>
        void HandleExit(GridCell cell)
        {
            var arch = CardGameArchitecture.Interface;
            var realmModel = arch.GetModel<ISecretRealmModel>();
            bool inRealm = !string.IsNullOrEmpty(realmModel.ActiveRealmId.Value);

            if (inRealm)
            {
                if (cell.interactTargetId == "next_floor")
                {
                    // 推进下一层：换图重载
                    var realmSys = arch.GetSystem<ISecretRealmSystem>();
                    if (realmSys.AdvanceFloor())
                    {
                        var mapId = realmSys.GetCurrentFloorMapId();
                        FloatingTip.ShowSuccess("深入下一层……");
                        GridMapSceneLoader.PendingGridMapId = mapId;
                        UnityEngine.SceneManagement.SceneManager.LoadScene("5- GridMap");
                        return;
                    }
                    // 已到顶：此出口应配realm_clear，走通关
                }

                if (cell.interactTargetId == "realm_clear")
                {
                    arch.GetSystem<ISecretRealmSystem>().ClearRealm();
                    return;
                }

                if (cell.interactTargetId == "retreat")
                {
                    arch.GetSystem<ISecretRealmSystem>().RetreatRealm();
                    return;
                }
            }

            // 普通撤离：回世界地图
            SaveSystem.Save();
            GridMapSceneLoader.ExitToWorldMap();
        }

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;
    }
}
