using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame;
using CardGame.Audio;
using QFramework;

namespace CardGame.UI
{
    /// <summary>
    /// 主菜单按钮逻辑绑定
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private void Start()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null) { Debug.LogError("[MainMenu] MainMenuCanvas not found"); return; }

            Transform uiRoot = canvas.transform.Find("MenuUI");
            if (uiRoot == null) { Debug.LogError("[MainMenu] MenuUI not found"); return; }

            // 继续游戏
            BindButton(uiRoot, "ButtonArea/btn_continue", OnContinueGame);
            // 新游戏
            BindButton(uiRoot, "ButtonArea/btn_newgame", OnNewGame);
            // 图鉴
            BindButton(uiRoot, "ButtonArea/btn_codex", OnCodex);
            // 设置
            BindButton(uiRoot, "ButtonArea/btn_settings", OnSettings);
            // 退出
            BindButton(uiRoot, "btn_exit", OnExit);

            // 如果没有存档，禁用"继续游戏"
            if (!SaveSystem.HasSave())
            {
                var continueBtn = uiRoot.Find("ButtonArea/btn_continue");
                if (continueBtn != null)
                {
                    var btn = continueBtn.GetComponent<Button>();
                    if (btn != null) btn.interactable = false;
                    var img = continueBtn.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    var txt = continueBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }

            Debug.Log("[MainMenu] 按钮绑定完成");
        }

        void BindButton(Transform root, string path, UnityEngine.Events.UnityAction action)
        {
            var t = root.Find(path);
            if (t == null) { Debug.LogWarning($"[MainMenu] Button not found: {path}"); return; }
            var btn = t.GetComponent<Button>();
            if (btn == null) { Debug.LogWarning($"[MainMenu] Button component missing: {path}"); return; }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                action?.Invoke();
            });
        }

        void OnContinueGame()
        {
            if (SaveSystem.HasSave())
            {
                SaveSystem.Load();
                FloatingTip.ShowSuccess("存档加载成功");
                // 进入基地
                var sceneChanger = FindObjectOfType<NueGames.NueDeck.Scripts.Utils.SceneChanger>();
                if (sceneChanger != null) sceneChanger.OpenBaseScene();
                else UnityEngine.SceneManagement.SceneManager.LoadScene(2);
            }
            else
            {
                FloatingTip.ShowWarning("没有存档");
            }
        }

        void OnNewGame()
        {
            SaveSystem.DeleteSave();
            // 初始化新游戏数据
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm != null) gm.InitGameplayData();

            // 给玩家一本初始功法 (火法) 并自动设为当前
            var arch = CardGameArchitecture.Interface;
            var cultSystem = arch.GetSystem<ICultivationSystem>();
            cultSystem.LearnMethod("sy_method"); // 三阳三昧丙丁炼火诀
            cultSystem.LearnMethod("th_method"); // 太和十六洞天
            cultSystem.SetActiveMethod("sy_method");
            // 给一些初始参悟点
            arch.GetModel<ICultivationModel>().ComprehensionPoints.Value = 20;

            FloatingTip.ShowSuccess("开始新游戏，已获得《三阳三昧丙丁炼火诀》");
            var sceneChanger = FindObjectOfType<NueGames.NueDeck.Scripts.Utils.SceneChanger>();
            if (sceneChanger != null) sceneChanger.OpenBaseScene();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(2);
        }

        void OnCodex()
        {
            // 进入图鉴 — 在主菜单场景打开图鉴面板
            var baseCtrl = FindObjectOfType<BaseUIController>();
            if (baseCtrl != null)
            {
                baseCtrl.OnCodex();
            }
            else
            {
                // 直接加载Base场景再打开图鉴
                FloatingTip.Show("图鉴功能将在洞府中可用");
            }
        }

        void OnSettings()
        {
            var existing = GameObject.Find("SettingsCanvas");
            if (existing != null) { existing.SetActive(!existing.activeSelf); return; }

            var go = BaseSceneInitializer.InstantiateCanvas("SettingsCanvas");
            if (go != null) Debug.Log("[MainMenu] 设置面板已打开");
        }

        void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
