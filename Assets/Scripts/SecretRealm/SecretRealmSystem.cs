using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 秘境系统 — 爬塔流程控制
    /// 进入秘境→逐层推进（格子地图）→Boss层通关→奖励+回世界地图
    /// 层推进：格子地图Exit格 targetId="next_floor" → 下一层地图
    /// </summary>
    public interface ISecretRealmSystem : ISystem
    {
        /// <summary>进入秘境（校验开放窗口/已通关）。返回false=不可进</summary>
        bool EnterRealm(string realmId);

        /// <summary>推进到下一层。返回false=已在顶层（应通关）</summary>
        bool AdvanceFloor();

        /// <summary>通关当前秘境：发奖励+记录+回世界地图</summary>
        void ClearRealm();

        /// <summary>中途撤离：无奖励回世界地图</summary>
        void RetreatRealm();

        /// <summary>当前层的格子地图ID（进入秘境后用）</summary>
        string GetCurrentFloorMapId();

        /// <summary>秘境是否开放</summary>
        bool IsRealmOpen(string realmId);

        /// <summary>获取配置</summary>
        SecretRealmConfig GetConfig(string realmId);
    }

    public class SecretRealmSystem : AbstractSystem, ISecretRealmSystem
    {
        ISecretRealmModel _model;
        readonly Dictionary<string, SecretRealmConfig> _configs = new Dictionary<string, SecretRealmConfig>();

        protected override void OnInit()
        {
            _model = this.GetModel<ISecretRealmModel>();
            LoadConfigs();
        }

        void LoadConfigs()
        {
            _configs.Clear();
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:SecretRealmConfig");
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<SecretRealmConfig>(path);
                if (config != null && !string.IsNullOrEmpty(config.realmId))
                    _configs[config.realmId] = config;
            }
#else
            var loaded = Resources.LoadAll<SecretRealmConfig>("SecretRealms");
            foreach (var c in loaded)
                if (!string.IsNullOrEmpty(c.realmId))
                    _configs[c.realmId] = c;
#endif
            Debug.Log($"[SecretRealm] 已加载 {_configs.Count} 个秘境配置");
        }

        public bool IsRealmOpen(string realmId)
        {
            var config = GetConfig(realmId);
            if (config == null) return false;

            // 一次性秘境：通关后不再开放
            if (_model.ClearedRealmIds.Contains(realmId)) return false;

            int days = this.GetSystem<IGameTimeSystem>().GetTotalDays();
            return config.IsOpen(days);
        }

        public bool EnterRealm(string realmId)
        {
            var config = GetConfig(realmId);
            if (config == null)
            {
                Debug.LogWarning($"[SecretRealm] 配置不存在: {realmId}");
                return false;
            }
            if (!IsRealmOpen(realmId))
            {
                UI.FloatingTip.ShowWarning("秘境尚未开启，或已被探索殆尽");
                return false;
            }

            _model.ActiveRealmId.Value = realmId;
            _model.CurrentFloor.Value = 1;
            Debug.Log($"[SecretRealm] 进入 {config.realmName} 第1层");
            return true;
        }

        public bool AdvanceFloor()
        {
            var config = GetConfig(_model.ActiveRealmId.Value);
            if (config == null) return false;

            int next = _model.CurrentFloor.Value + 1;
            if (next > config.floors.Count)
                return false; // 已到顶

            _model.CurrentFloor.Value = next;
            Debug.Log($"[SecretRealm] 推进至 {config.realmName} 第{next}层");
            return true;
        }

        public void ClearRealm()
        {
            var realmId = _model.ActiveRealmId.Value;
            var config = GetConfig(realmId);
            if (config == null) return;

            // 通关奖励
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            var battleModel = this.GetModel<IBattleModel>();
            battleModel.CurrentGold.Value += config.clearGold;
            if (gm != null && gm.PersistentGameplayData != null)
                gm.PersistentGameplayData.CurrentGold = battleModel.CurrentGold.Value;

            _model.ClearedRealmIds.Add(realmId);
            UI.FloatingTip.ShowSuccess($"通关{config.realmName}！获得{config.clearGold}灵石");

            Debug.Log($"[SecretRealm] 通关 {config.realmName}");
            ExitRealm();
        }

        public void RetreatRealm()
        {
            UI.FloatingTip.Show("御剑撤离秘境（未获通关奖励）");
            ExitRealm();
        }

        void ExitRealm()
        {
            _model.ActiveRealmId.Value = "";
            _model.CurrentFloor.Value = 0;
            SaveSystem.Save();
            GridMapSceneLoader.ExitToWorldMap();
        }

        public string GetCurrentFloorMapId()
        {
            var config = GetConfig(_model.ActiveRealmId.Value);
            if (config == null) return null;

            var floor = config.GetFloor(_model.CurrentFloor.Value - 1);
            return floor?.gridMapId;
        }

        public SecretRealmConfig GetConfig(string realmId)
        {
            if (string.IsNullOrEmpty(realmId)) return null;
            _configs.TryGetValue(realmId, out var config);
            return config;
        }
    }
}
