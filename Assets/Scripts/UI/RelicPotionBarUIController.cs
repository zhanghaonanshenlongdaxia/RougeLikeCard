using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace CardGame.UI
{
    public class RelicPotionBarUIController : MonoBehaviour, IController
    {
        [SerializeField] private Transform relicBarRoot;
        [SerializeField] private Transform potionBarRoot;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private int maxRelicSlots = 6;
        [SerializeField] private int maxPotionSlots = 3;

        private IRelicModel _relicModel;
        private IPotionModel _potionModel;
        private IPotionSystem _potionSystem;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _relicModel = this.GetModel<IRelicModel>();
            _potionModel = this.GetModel<IPotionModel>();
            _potionSystem = this.GetSystem<IPotionSystem>();
        }

        private void OnEnable()
        {
            _relicModel.RelicCount.RegisterWithInitValue(_ => RefreshRelics()).UnRegisterWhenGameObjectDestroyed(gameObject);
            _potionModel.PotionCount.RegisterWithInitValue(_ => RefreshPotions()).UnRegisterWhenGameObjectDestroyed(gameObject);
            RefreshRelics();
            RefreshPotions();
        }

        private void RefreshRelics()
        {
            if (relicBarRoot == null) return;
            for (int i = relicBarRoot.childCount - 1; i >= 0; i--)
                Destroy(relicBarRoot.GetChild(i).gameObject);

            int count = 0;
            foreach (var relic in _relicModel.OwnedRelics)
            {
                if (count >= maxRelicSlots) break;
                var slot = CreateSlot(relicBarRoot, relic.data?.name ?? "?", relic.data?.relicIcon, false);
                count++;
            }

            // 濉厖绌烘Ы
            for (int i = count; i < maxRelicSlots; i++)
                CreateSlot(relicBarRoot, "", null, false);
        }

        private void RefreshPotions()
        {
            if (potionBarRoot == null) return;
            for (int i = potionBarRoot.childCount - 1; i >= 0; i--)
                Destroy(potionBarRoot.GetChild(i).gameObject);

            int count = 0;
            foreach (var potion in _potionModel.OwnedPotions)
            {
                if (count >= maxPotionSlots) break;
                var slot = CreateSlot(potionBarRoot, potion.name, potion.potionIcon, true);
                var btn = slot.GetComponent<Button>();
                if (btn == null) btn = slot.AddComponent<Button>();
                var index = count;
                btn.onClick.AddListener(() =>
                {
                    var cm = NueGames.NueDeck.Scripts.Managers.CombatManager.Instance;
                    if (cm != null && cm.CurrentMainAlly != null)
                    {
                        var context = new PotionUseContext(
                            player: cm.CurrentMainAlly,
                            enemies: cm.CurrentEnemiesList);
                        _potionSystem.UsePotion(index, context);
                        RefreshPotions();
                    }
                });
                count++;
            }

            for (int i = count; i < maxPotionSlots; i++)
                CreateSlot(potionBarRoot, "", null, false);
        }

        private GameObject CreateSlot(Transform parent, string label, Sprite icon, bool isPotion)
        {
            var go = new GameObject("Slot");
            go.transform.SetParent(parent);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
            var img = go.AddComponent<Image>();
            img.color = string.IsNullOrEmpty(label) ? new Color(0.1f, 0.1f, 0.15f, 0.5f) : (isPotion ? new Color(0.1f, 0.25f, 0.15f, 1) : new Color(0.3f, 0.25f, 0.1f, 1));
            if (icon) img.sprite = icon;

            if (!string.IsNullOrEmpty(label))
            {
                var textObj = new GameObject("Label");
                textObj.transform.SetParent(go.transform);
                var rt = textObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.text = label.Length > 2 ? label.Substring(0, 2) : label;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 12;
                tmp.color = Color.white;
                var libSans = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (libSans) tmp.font = libSans;
            }

            return go;
        }
    }
}
