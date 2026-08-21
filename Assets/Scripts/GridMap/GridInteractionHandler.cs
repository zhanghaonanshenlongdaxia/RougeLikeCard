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
                    Debug.Log($"[GridInteract] NPC对话: {cell.interactTargetId}");
                    FloatingTip.Show("与NPC对话（剧情系统待接入）");
                    GridInteractionEvents.TriggerInteractionComplete(cell, "npc");
                    break;

                case GridInteractType.StoryTrigger:
                    Debug.Log($"[GridInteract] 剧情触发: {cell.interactTargetId}");
                    FloatingTip.Show("剧情触发（Yarn待接入）");
                    GridInteractionEvents.TriggerInteractionComplete(cell, "story");
                    break;

                case GridInteractType.Building:
                    Debug.Log($"[GridInteract] 建筑调查: {cell.interactTargetId}");
                    FloatingTip.Show("调查建筑（待接入）");
                    GridInteractionEvents.TriggerInteractionComplete(cell, "building");
                    break;

                case GridInteractType.Exit:
                    Debug.Log("[GridInteract] 到达撤离点");
                    FloatingTip.Show("到达撤离点（撤离功能待接入）");
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

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;
    }
}
