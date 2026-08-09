using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Containers
{
    /// <summary>
    /// Buff数据库SO — 汇总所有BuffData，运行时统一查询。
    /// 引用路径：GameManager.BuffDatabase → CharacterStats / TooltipManager / CardBase / CharacterCanvas
    /// </summary>
    [CreateAssetMenu(fileName = "Buff Database", menuName = "NueDeck/Containers/Buff Database", order = 11)]
    public class BuffDatabase : ScriptableObject
    {
        [SerializeField] private List<BuffData> buffDataList;

        private Dictionary<StatusType, BuffData> _lookup;

        public List<BuffData> AllBuffs => buffDataList;

        /// <summary>运行时静态引用，由GameManager.Awake设置</summary>
        public static BuffDatabase Instance { get; private set; }

        /// <summary>初始化静态引用（GameManager.Awake调用）</summary>
        public void Init()
        {
            Instance = this;
            BuildLookup();
        }

        public BuffData GetBuff(StatusType type)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(type, out var buff) ? buff : null;
        }

        /// <summary>获取用于Tooltip的显示名，找不到时返回枚举名</summary>
        public string GetDisplayName(StatusType type)
        {
            var buff = GetBuff(type);
            return buff != null ? buff.DisplayName : type.ToString();
        }

        /// <summary>获取用于Tooltip的说明文字，找不到时返回空</summary>
        public string GetDescription(StatusType type)
        {
            var buff = GetBuff(type);
            return buff != null ? buff.Description : "";
        }

        /// <summary>获取图标，找不到时返回null</summary>
        public Sprite GetIcon(StatusType type)
        {
            var buff = GetBuff(type);
            return buff != null ? buff.Icon : null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<StatusType, BuffData>();
            if (buffDataList == null) return;
            foreach (var buff in buffDataList)
                if (buff != null && !_lookup.ContainsKey(buff.StatusType))
                    _lookup[buff.StatusType] = buff;
        }

        #region Editor
#if UNITY_EDITOR
        public void EditBuffList(List<BuffData> list) => buffDataList = list;
#endif
        #endregion
    }
}
