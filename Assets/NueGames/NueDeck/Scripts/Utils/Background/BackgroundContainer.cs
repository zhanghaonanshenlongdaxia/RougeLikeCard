using System;
using System.Collections.Generic;
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

            var encounter = CombatManager != null ? CombatManager.CurrentEncounter : null;
            foreach (var backgroundRoot in BackgroundRootList)
            {
                if (backgroundRoot == null) continue;
                var targetBg = encounter != null ? encounter.TargetBackgroundType : NueGames.NueDeck.Scripts.Enums.BackgroundTypes.Profile1;
                backgroundRoot.gameObject.SetActive(targetBg == backgroundRoot.BackgroundType);
            }

            // 确保至少一个背景激活（如果全没匹配上，激活第一个）
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