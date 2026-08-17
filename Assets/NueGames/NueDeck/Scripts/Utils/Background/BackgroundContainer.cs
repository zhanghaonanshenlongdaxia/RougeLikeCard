using System;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Utils.Background
{
    public class BackgroundContainer : MonoBehaviour
    {
        [SerializeField] private List<BackgroundRoot> backgroundRootList;
        public List<BackgroundRoot> BackgroundRootList => backgroundRootList;
        
        private CombatManager CombatManager => CombatManager.Instance;
        
        public void OpenSelectedBackground()
        {
            if (backgroundRootList == null || backgroundRootList.Count == 0) return;

            // 根据AdventureModel的regionId决定背景类型
            BackgroundTypes targetBg = BackgroundTypes.Profile1;
            try
            {
                var advModel = CardGame.CardGameArchitecture.Interface.GetModel<CardGame.IAdventureModel>();
                var config = advModel?.SelectedMapId;
                if (!string.IsNullOrEmpty(config))
                {
                    // 通过ResourceCache获取regionId
                    var encounterData = CardGame.ResourceCache.GetEncounterData(0);
                    // 直接从AdventureMapConfig获取regionId
                    var mapConfig = advModel?.SelectedMapId;
                    if (!string.IsNullOrEmpty(mapConfig))
                    {
                        // 简单映射: mapId → regionId → BackgroundType
                        // map_shanye=0→Profile1, map_youming=1→Profile2, map_wangu=2→Profile3, map_tianmo=3→Profile1
                        if (mapConfig.Contains("shanye")) targetBg = BackgroundTypes.Profile1;
                        else if (mapConfig.Contains("youming")) targetBg = BackgroundTypes.Profile2;
                        else if (mapConfig.Contains("wangu")) targetBg = BackgroundTypes.Profile3;
                        else if (mapConfig.Contains("tianmo")) targetBg = BackgroundTypes.Profile1;
                    }
                }
            }
            catch { /* QFramework not ready, use default */ }

            // 如果上面没设置成功，用旧的encounter方式
            if (targetBg == BackgroundTypes.Profile1)
            {
                var encounter = CombatManager != null ? CombatManager.CurrentEncounter : null;
                if (encounter != null)
                    targetBg = encounter.TargetBackgroundType;
            }

            foreach (var backgroundRoot in BackgroundRootList)
            {
                if (backgroundRoot == null) continue;
                backgroundRoot.gameObject.SetActive(targetBg == backgroundRoot.BackgroundType);
            }

            // 确保至少一个背景激活
            bool anyActive = false;
            foreach (var bg in BackgroundRootList)
            {
                if (bg != null && bg.gameObject.activeSelf) { anyActive = true; break; }
            }
            if (!anyActive && BackgroundRootList.Count > 0 && BackgroundRootList[0] != null)
                BackgroundRootList[0].gameObject.SetActive(true);
        }
    }
}