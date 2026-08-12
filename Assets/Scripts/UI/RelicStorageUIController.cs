using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace CardGame.UI
{
    /// <summary>
    /// 洞府法宝仓UI：显示所有已拥有的法宝及其耐久度。
    /// </summary>
    public class RelicStorageUIController : MonoBehaviour, IController
    {
        private TMP_FontAsset _font;
        private UnityEngine.UI.Button _closeButton;
        private IRelicModel _relicModel;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _relicModel = this.GetModel<IRelicModel>();
            AutoBindReferences();
        }

        private void OnEnable()
        {
            RefreshList();
        }


        private Transform _contentRoot;

        private void RefreshList()
        {
            if (_contentRoot == null || _relicModel == null) return;

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                Destroy(_contentRoot.GetChild(i).gameObject);

            if (_relicModel.OwnedRelics.Count == 0)
            {
                CreateText(_contentRoot, "暂无法宝", 18, new Color(0.5f, 0.5f, 0.55f));
                return;
            }

            foreach (var relic in _relicModel.OwnedRelics)
            {
                CreateRelicEntry(relic);
            }
        }

        private void CreateRelicEntry(RelicInstance relic)
        {
            var entry = new GameObject($"Relic_{relic.relicId}");
            entry.transform.SetParent(_contentRoot, false);
            entry.AddComponent<RectTransform>();
            var img = entry.AddComponent<Image>();

            bool broken = relic.IsBroken;
            img.color = broken ? new Color(0.15f, 0.1f, 0.1f, 0.8f) : new Color(0.1f, 0.15f, 0.2f, 0.9f);
            entry.AddComponent<LayoutElement>().preferredHeight = 60;

            var hlg = entry.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.childControlWidth = false; hlg.childForceExpandWidth = false;

            // 图标
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(entry.transform, false);
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(50, 50);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = new Color(0.3f, 0.25f, 0.1f, 1);
            if (relic.data?.relicIcon != null)
                iconImg.sprite = relic.data.relicIcon;
            if (broken) iconImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            // 名称+描述
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(entry.transform, false);
            infoObj.AddComponent<RectTransform>();
            var infoLe = infoObj.AddComponent<LayoutElement>();
            infoLe.flexibleWidth = 1;
            var infoVLG = infoObj.AddComponent<VerticalLayoutGroup>();
            infoVLG.spacing = 2; infoVLG.childControlWidth = true;

            var nameStr = relic.data?.name ?? relic.relicId;
            if (broken) nameStr += " [已损坏]";
            var nameTxt = CreateText(infoObj.transform, nameStr, 16,
                broken ? new Color(0.5f, 0.3f, 0.3f) : new Color(0.9f, 0.8f, 0.3f));
            nameTxt.alignment = TextAlignmentOptions.Left;

            var descTxt = CreateText(infoObj.transform, relic.data?.description ?? "", 13, new Color(0.6f, 0.6f, 0.65f));
            descTxt.alignment = TextAlignmentOptions.Left;

            // 耐久度
            var durStr = broken ? "已损坏" : $"{relic.currentDurability}/{relic.data?.maxDurability ?? 0}";
            var durColor = broken ? Color.red :
                relic.currentDurability <= 2 ? new Color(1f, 0.4f, 0.2f) : new Color(0.4f, 0.8f, 0.4f);
            var durTxt = CreateText(entry.transform, durStr, 16, durColor);
            durTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 50);
            durTxt.alignment = TextAlignmentOptions.Center;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return tmp;
        }
        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;
            var scroll = panel.Find("Scroll");
            if (scroll != null)
            {
                var content = scroll.Find("Viewport/Content");
                _contentRoot = content;
            }
            _closeButton = panel.Find("CloseButton")?.GetComponent<UnityEngine.UI.Button>();
        }

    }

}

