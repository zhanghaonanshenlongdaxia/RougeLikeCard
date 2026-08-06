using System;
using System.Collections;
using CardGame.Audio;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.ThirdParty.NueTooltip.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NueGames.NueDeck.Scripts.Utils
{
    public class SceneChanger : MonoBehaviour
    {
        private GameManager GameManager => GameManager.Instance;
        private UIManager UIManager => UIManager.Instance;
        
        private enum SceneType
        {
            MainMenu,
            Base,
            Map,
            Combat
        }
        public void OpenMainMenuScene()
        {
            StartCoroutine(DelaySceneChange(SceneType.MainMenu));
        }
        private IEnumerator DelaySceneChange(SceneType type)
        {
            if (UIManager.Instance.InventoryCanvas)
                UIManager.SetCanvas(UIManager.Instance.InventoryCanvas,false,true);
            yield return StartCoroutine(UIManager.Instance.Fade(true));

            switch (type)
            {
                case SceneType.MainMenu:
                    PlaySceneBGM(SceneBGM.Menu);
                    UIManager.ChangeScene(GameManager.SceneData.mainMenuSceneIndex);
                    UIManager.SetCanvas(UIManager.CombatCanvas,false,true);
                    UIManager.SetCanvas(UIManager.InformationCanvas,false,true);
                    UIManager.SetCanvas(UIManager.RewardCanvas,false,true);
                   
                    GameManager.InitGameplayData();
                    GameManager.SetInitalHand();
                    break;
                case SceneType.Base:
                    PlaySceneBGM(SceneBGM.Base);
                    // 基地场景索引 = mapSceneIndex - 1 (Base is before Map in build settings)
                    UIManager.ChangeScene(GameManager.SceneData.mapSceneIndex - 1);
                    break;
                case SceneType.Map:
                    PlaySceneBGM(SceneBGM.Map);
                    UIManager.ChangeScene(GameManager.SceneData.mapSceneIndex);
                    UIManager.SetCanvas(UIManager.CombatCanvas,false,true);
                    UIManager.SetCanvas(UIManager.InformationCanvas,true,false);
                    UIManager.SetCanvas(UIManager.RewardCanvas,false,true);
                    break;
                case SceneType.Combat:
                    PlaySceneBGM(SceneBGM.Battle);
                    UIManager.ChangeScene(GameManager.SceneData.combatSceneIndex);
                    UIManager.SetCanvas(UIManager.CombatCanvas,false,true);
                    UIManager.SetCanvas(UIManager.InformationCanvas,true,false);
                    UIManager.SetCanvas(UIManager.RewardCanvas,false,true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        public void OpenMapScene()
        {
            StartCoroutine(DelaySceneChange(SceneType.Map));
        }
        public void OpenBaseScene()
        {
            StartCoroutine(DelaySceneChange(SceneType.Base));
        }
        public void OpenCombatScene()
        {
            StartCoroutine(DelaySceneChange(SceneType.Combat));
        }
        public void ChangeScene(int sceneId)
        {
            if (sceneId == GameManager.SceneData.mainMenuSceneIndex)
                OpenMainMenuScene();
            else if (sceneId == GameManager.SceneData.mapSceneIndex - 1)
                OpenBaseScene();
            else if (sceneId == GameManager.SceneData.mapSceneIndex)
                OpenMapScene();
            else if (sceneId == GameManager.SceneData.combatSceneIndex)
                OpenCombatScene();
            else
                SceneManager.LoadScene(sceneId);
            
            TooltipManager.Instance.HideTooltip();
        }
        public void ExitApp()
        {
            GameManager.OnExitApp();
            Application.Quit();
        }

        private void PlaySceneBGM(SceneBGM bgm)
        {
            Debug.Log($"[Audio] PlaySceneBGM({bgm}) called, Instance={(GameAudioManager.Instance != null ? "exists" : "NULL")}");
            if (GameAudioManager.Instance != null)
                GameAudioManager.Instance.PlayBGM(bgm);
        }
    }
}
