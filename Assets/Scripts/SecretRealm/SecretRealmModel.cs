using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 秘境状态模型（爬塔进度）
    /// </summary>
    public interface ISecretRealmModel : IModel
    {
        /// <summary>当前秘境ID（未在秘境中为空）</summary>
        BindableProperty<string> ActiveRealmId { get; }

        /// <summary>当前层数（1起；0=未进入）</summary>
        BindableProperty<int> CurrentFloor { get; }

        /// <summary>已通关的秘境ID集合（一次性秘境通关后锁定）</summary>
        HashSet<string> ClearedRealmIds { get; }
    }

    public class SecretRealmModel : AbstractModel, ISecretRealmModel
    {
        public BindableProperty<string> ActiveRealmId { get; } = new BindableProperty<string>("");
        public BindableProperty<int> CurrentFloor { get; } = new BindableProperty<int>(0);
        public HashSet<string> ClearedRealmIds { get; } = new HashSet<string>();

        protected override void OnInit() { }
    }
}
