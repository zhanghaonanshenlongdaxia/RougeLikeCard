using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace CardGame.UI
{
    public class InventoryUIController : MonoBehaviour, IController
    {
        [Header("References")]
        [SerializeField] private Transform itemGridRoot;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private Button backButton;
        [SerializeField] private Transform[] tabButtons;

        private IInventoryModel _model;
        private IInventorySystem _system;
        private MaterialType? _currentFilter;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _model = this.GetModel<IInventoryModel>();
            _system = this.GetSystem<IInventorySystem>();
        }

        private void OnEnable()
        {
            _model.CurrentWeight.RegisterWithInitValue(UpdateWeight).UnRegisterWhenGameObjectDestroyed(gameObject);
            Refresh();
        }

        private void UpdateWeight(int weight)
        {
            if (weightText) weightText.text = $"璐熼噸: {weight}/{_model.MaxWeight.Value}";
        }

        public void Refresh()
        {
            if (itemGridRoot == null) return;

            foreach (Transform child in itemGridRoot)
                Destroy(child.gameObject);

            foreach (var slot in _model.Slots)
            {
                if (slot == null || slot.IsEmpty) continue;

                if (_currentFilter.HasValue && slot.item is MaterialData mat)
                {
                    if (mat.materialType != _currentFilter.Value) continue;
                }

                var slotGo = itemSlotPrefab ? Instantiate(itemSlotPrefab, itemGridRoot) : CreateSimpleSlot(itemGridRoot);
                var tmps = slotGo.GetComponentsInChildren<TextMeshProUGUI>();
                if (tmps.Length > 0) tmps[0].text = $"{slot.item.ItemName}\n脳{slot.count}";

                var btn = slotGo.GetComponentInChildren<Button>();
                if (btn == null) btn = slotGo.AddComponent<Button>();
            }
        }

        private GameObject CreateSimpleSlot(Transform parent)
        {
            var go = new GameObject("ItemSlot");
            go.transform.SetParent(parent);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(120, 120);
            go.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 1);
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform);
            var rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 16;
            tmp.color = Color.white;
            var libSans = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (libSans) tmp.font = libSans;
            return go;
        }

        public void OnTabSelected(int tabIndex)
        {
            _currentFilter = tabIndex switch
            {
                0 => null,
                1 => MaterialType.SpiritHerb,
                2 => MaterialType.Ore,
                3 => MaterialType.DemonCore,
                4 => MaterialType.SoulStone,
                5 => MaterialType.SpiritWater,
                6 => MaterialType.Fragment,
                _ => null
            };
            Refresh();
        }

        public void OnBackButton()
        {
            gameObject.SetActive(false);
            var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
            if (mapMgr != null) mapMgr.SendMessage("BuildMap");
        }
    }
}
