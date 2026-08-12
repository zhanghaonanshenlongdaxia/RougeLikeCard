using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI
{
    /// <summary>
    /// LoopScrollRect 的 PrefabSource 实现，用于运行时创建的模板。
    /// 通过构造函数传入模板 GameObject，GetObject 时 Instantiate，ReturnObject 时入池回收。
    /// </summary>
    public class LoopScrollPrefabSourceImpl : LoopScrollPrefabSource
    {
        private readonly GameObject _prefab;
        private readonly Transform _poolParent;
        private readonly Stack<Transform> _pool = new Stack<Transform>();

        /// <param name="prefab">模板</param>
        /// <param name="poolParent">回收池父节点（通常传 LoopScrollRect 所在的 transform，不能为 null）</param>
        public LoopScrollPrefabSourceImpl(GameObject prefab, Transform poolParent)
        {
            _prefab = prefab;
            _poolParent = poolParent;
        }

        public GameObject GetObject(int index)
        {
            if (_pool.Count > 0)
            {
                var go = _pool.Pop().gameObject;
                go.SetActive(true);
                return go;
            }
            return Object.Instantiate(_prefab);
        }

        public void ReturnObject(Transform trans)
        {
            trans.gameObject.SetActive(false);
            trans.SetParent(_poolParent, false);
            _pool.Push(trans);
        }
    }
}
