using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace CardGame.Story
{
    /// <summary>
    /// 剧情服务 — 统一入口：StartDialogue(nodeName)启动Yarn节点
    /// 自动创建DialogueRunner+Presenter（DontDestroyOnLoad单例）
    ///
    /// Yarn指令对接游戏系统：
    ///   <<battle 敌人ID>>        拉起战斗（BattleLauncher）
    ///   <<give 材料名 数量>>      给材料
    ///   <<give_gold 数量>>        给灵石
    ///   <<set_flag 名字 值>>      剧情标记（Yarn变量，带$story_前缀）
    /// </summary>
    public class StoryService : MonoBehaviour
    {
        static StoryService _instance;
        public static StoryService Instance => _instance;

        DialogueRunner _runner;
        StoryDialoguePresenter _presenter;
        YarnProject _project;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>启动指定Yarn节点</summary>
        public static void StartDialogue(string nodeName)
        {
            var inst = EnsureInstance();
            if (inst == null)
            {
                Debug.LogError("[Story] StoryService创建失败");
                return;
            }
            inst.RunNode(nodeName);
        }

        /// <summary>设置Yarn变量（$前缀自动补全）</summary>
        public static void SetVariable(string name, bool value)
        {
            var inst = EnsureInstance();
            if (inst?._runner == null) return;
            if (!name.StartsWith("$")) name = "$" + name;
            (inst._runner.Dialogue.VariableStorage as InMemoryVariableStorage)
                ?.SetValue(name, value);
        }

        /// <summary>读取Yarn变量</summary>
        public static bool GetVariable(string name, bool fallback = false)
        {
            var inst = EnsureInstance();
            if (inst?._runner == null) return fallback;
            if (!name.StartsWith("$")) name = "$" + name;
            if (inst._runner.Dialogue.VariableStorage.TryGetValue(name, out bool v))
                return v;
            return fallback;
        }

        static StoryService EnsureInstance()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("StoryService");
            var inst = go.AddComponent<StoryService>();
            inst.Setup();
            return inst;
        }

        void Setup()
        {
            // 加载YarnProject（Assets/YarnStory/StoryProject.yarnproject，编辑器下直接加载）
#if UNITY_EDITOR
            _project = UnityEditor.AssetDatabase.LoadAssetAtPath<YarnProject>("Assets/YarnStory/StoryProject.yarnproject");
#else
            _project = Resources.Load<YarnProject>("YarnStory/StoryProject");
#endif
            if (_project == null)
            {
                Debug.LogWarning("[Story] 未找到YarnProject，剧情对话不可用");
                return;
            }

            _presenter = StoryDialoguePresenter.Create(transform);
            _runner = gameObject.AddComponent<DialogueRunner>();
            _runner.autoStart = false;
            _runner.SetProject(_project);
            _runner.DialoguePresenters = new[] { (DialoguePresenterBase)_presenter };

            RegisterCommands();
            Debug.Log("[Story] StoryService初始化完成");
        }

        void RunNode(string nodeName)
        {
            if (_runner == null)
            {
                Setup();
                if (_runner == null)
                {
                    Debug.LogError("[Story] YarnProject未配置，无法运行剧情");
                    return;
                }
            }
            _ = _runner.StartDialogue(nodeName);
        }

        // ===================== 游戏指令注册 =====================

        void RegisterCommands()
        {
            // <<battle s1_wolf>> — 拉起战斗（无格子来源，胜利回基地）
            _runner.AddCommandHandler<string>("battle", enemyId =>
            {
                Debug.Log($"[Story] 指令battle: {enemyId}");
                BattleLauncher.StartBattle(enemyId);
            });

            // <<give 材料名 数量>> — 给材料
            _runner.AddCommandHandler<string, int>("give", (materialName, count) =>
            {
                var materials = ResourceCache.GetMaterials();
                var mat = materials.Find(m => m.ItemName == materialName || m.name == materialName);
                if (mat != null)
                {
                    CardGameArchitecture.Interface.GetSystem<IInventorySystem>().AddItem(mat, count);
                    Debug.Log($"[Story] 指令give: {mat.ItemName} x{count}");
                }
                else
                    Debug.LogWarning($"[Story] 材料未找到: {materialName}");
            });

            // <<give_gold 数量>>
            _runner.AddCommandHandler<int>("give_gold", amount =>
            {
                var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
                var battleModel = CardGameArchitecture.Interface.GetModel<IBattleModel>();
                battleModel.CurrentGold.Value += amount;
                if (gm != null && gm.PersistentGameplayData != null)
                    gm.PersistentGameplayData.CurrentGold = battleModel.CurrentGold.Value;
                Debug.Log($"[Story] 指令give_gold: {amount}");
            });

            // <<set_flag 名字 值>> — 剧情标记
            _runner.AddCommandHandler<string, string>("set_flag", (name, value) =>
            {
                bool b = value == "true" || value == "1";
                if (!name.StartsWith("$")) name = "$story_" + name;
                (_runner.Dialogue.VariableStorage as InMemoryVariableStorage)?.SetValue(name, b);
                Debug.Log($"[Story] 指令set_flag: {name}={b}");
            });
        }
    }
}
