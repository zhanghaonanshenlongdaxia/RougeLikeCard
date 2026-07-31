using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace CardGame.UI
{
    public class EvacuateUIController : MonoBehaviour, IController
    {
        [SerializeField] private Transform inventoryGridRoot;
        [SerializeField] private Transform safeBoxGridRoot;
        [SerializeField] private Button evacuateButton;
        [SerializeField] private Button continueButton;

        private IInventoryModel _model;
        private IInventorySystem _system;
        private IEvacuateSystem _evacSystem;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _model = this.GetModel<IInventoryModel>();
            _system = this.GetSystem<IInventorySystem>();
            _evacSystem = this.GetSystem<IEvacuateSystem>();
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            RefreshGrid(inventoryGridRoot, _model.Slots, true);
            RefreshGrid(safeBoxGridRoot, _model.SafeBoxSlots, false);
        }

        private void RefreshGrid(Transform root, System.Collections.Generic.List<InventorySlot> slots, bool canTransfer)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);

            foreach (var slot in slots)
            {
                if (slot == null || slot.IsEmpty) continue;
                var go = new GameObject("Item");
                go.transform.SetParent(root);
                go.AddComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
                go.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 1);
                CreateText(go, $"{slot.item.ItemName}\n脳{slot.count}", 16, Color.white);

                if (canTransfer)
                {
                    var btn = go.AddComponent<Button>();
                    var itemId = slot.item.ItemId;
                    var count = slot.count;
                    btn.onClick.AddListener(() =>
                    {
                        _system.TransferToSafeBox(itemId, count);
                        Refresh();
                    });
                }
            }
        }

        public void OnEvacuate()
        {
            _evacSystem.Evacuate();
            gameObject.SetActive(false);
        }

        public void OnContinue()
        {
            _evacSystem.ContinueAdventure();
            gameObject.SetActive(false);
        }

        private void CreateText(GameObject parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent.transform);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = size;
            tmp.color = color;
            var libSans = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (libSans) tmp.font = libSans;
        }
    }
}
