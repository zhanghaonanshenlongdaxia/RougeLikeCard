using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 功法系统数据模型
    /// </summary>
    public interface ICultivationModel : IModel
    {
        /// <summary>已学习功法ID</summary>
        HashSet<string> LearnedMethodIds { get; }
        /// <summary>当前装备功法ID</summary>
        BindableProperty<string> ActiveMethodId { get; }
        /// <summary>所有功法的已解锁节点ID</summary>
        HashSet<string> UnlockedNodeIds { get; }
        /// <summary>参悟点</summary>
        BindableProperty<int> ComprehensionPoints { get; }
        /// <summary>已学习神通ID</summary>
        HashSet<string> LearnedAbilityIds { get; }
        /// <summary>当前冒险装备的神通ID</summary>
        List<string> EquippedAbilityIds { get; }
        /// <summary>神通能量上限</summary>
        int MaxAbilityEnergy { get; set; }
        /// <summary>互斥组选择记录 (mutexGroup → 选中的nodeId)</summary>
        Dictionary<string, string> SelectedMutexChoices { get; }
        /// <summary>已获取的事件神通书籍ID (防重复)</summary>
        HashSet<string> AcquiredAbilityBookIds { get; }
        /// <summary>已获取的功法残篇ID (防重复)</summary>
        HashSet<string> AcquiredMethodFragmentIds { get; }
    }

    public class CultivationModel : AbstractModel, ICultivationModel
    {
        public HashSet<string> LearnedMethodIds { get; } = new HashSet<string>();
        public BindableProperty<string> ActiveMethodId { get; } = new BindableProperty<string>("");
        public HashSet<string> UnlockedNodeIds { get; } = new HashSet<string>();
        public BindableProperty<int> ComprehensionPoints { get; } = new BindableProperty<int>(0);
        public HashSet<string> LearnedAbilityIds { get; } = new HashSet<string>();
        public List<string> EquippedAbilityIds { get; } = new List<string>();
        public int MaxAbilityEnergy { get; set; } = 10;
        public Dictionary<string, string> SelectedMutexChoices { get; } = new Dictionary<string, string>();
        public HashSet<string> AcquiredAbilityBookIds { get; } = new HashSet<string>();
        public HashSet<string> AcquiredMethodFragmentIds { get; } = new HashSet<string>();

        protected override void OnInit() { }
    }
}
