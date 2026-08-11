using System.Collections.Generic;
using CardGame;
using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Cultivation
{
    /// <summary>
    /// 功法数据SO — 一本完整的功法书，包含修炼节点树
    /// </summary>
    [CreateAssetMenu(fileName = "Cultivation Method", menuName = "NueDeck/Cultivation/Method", order = 20)]
    public class CultivationMethodData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string methodId;
        [SerializeField] private string methodName;
        [SerializeField][TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Attributes")]
        [SerializeField] private ElementType element;
        [SerializeField] private CultivationMethodGrade grade;
        [Tooltip("残篇最高可修境界，Complete=渡劫")]
        [SerializeField] private RealmLevel maxRealm;
        [Tooltip("功法品质")]
        [SerializeField] private ItemQuality quality = ItemQuality.LianQi_T1;

        [Header("Nodes")]
        [SerializeField] private List<CultivationNodeData> nodes;

        #region Properties
        public string MethodId => methodId;
        public string MethodName => methodName;
        public string Description => description;
        public Sprite Icon => icon;
        public ElementType Element => element;
        public CultivationMethodGrade Grade => grade;
        public RealmLevel MaxRealm => maxRealm;
        public ItemQuality Quality => quality;
        public List<CultivationNodeData> Nodes => nodes;
        #endregion

        #region Helpers
        public CultivationNodeData GetNode(string nodeId)
        {
            if (nodes == null) return null;
            return nodes.Find(n => n.NodeId == nodeId);
        }

        public List<CultivationNodeData> GetNodesByRealm(RealmLevel realm)
        {
            if (nodes == null) return new List<CultivationNodeData>();
            return nodes.FindAll(n => n.Realm == realm);
        }

        public List<RealmLevel> GetAvailableRealms()
        {
            var realms = new List<RealmLevel>();
            if (nodes == null) return realms;
            foreach (var node in nodes)
            {
                if (!realms.Contains(node.Realm))
                    realms.Add(node.Realm);
            }
            return realms;
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        public void EditMethodId(string id) => methodId = id;
        public void EditMethodName(string name) => methodName = name;
        public void EditDescription(string desc) => description = desc;
        public void EditIcon(Sprite sprite) => icon = sprite;
        public void EditElement(ElementType el) => element = el;
        public void EditGrade(CultivationMethodGrade g) => grade = g;
        public void EditMaxRealm(RealmLevel r) => maxRealm = r;
        public void EditQuality(ItemQuality q) => quality = q;
        public void EditNodes(List<CultivationNodeData> n) => nodes = n;
#endif
        #endregion
    }
}
