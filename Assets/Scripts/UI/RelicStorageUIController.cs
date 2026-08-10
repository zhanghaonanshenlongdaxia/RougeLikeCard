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
        private IRelicModel _relicModel;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _relicModel = this.GetModel<IRelicModel>();
            BuildUI();
        }

        private void OnEnable()
        {
            RefreshList();
        }

        private void BuildUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 70;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // 背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.9f);
            var bgBtn = bg.AddComponent<Button>();
            bgBtn.onClick.AddListener(() => gameObject.SetActive(false));

            // 主面板
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.1f, 0.1f); pRt.anchorMax = new Vector2(0.9f, 0.9f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childControlWidth = true; vlg.childForceExpandHeight = false;

            // 标题
            var titleObj = CreateText(panel.transform, "法宝仓", 26, new Color(0.9f, 0.8f, 0.3f));
            titleObj.gameObject.AddComponent<LayoutElement>().preferredHeight = 35;

            // 滚动列表
            var scrollObj = new GameObject("Scroll");
            scrollObj.transform.SetParent(panel.transform, false);
            scrollObj.AddComponent<RectTransform>();
            var scrollLe = scrollObj.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollObj.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            vp.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.spacing = 5; contentVLG.childControlWidth = true;
            contentVLG.childForceExpandHeight = false;
            scroll.content = cRt;

            _contentRoot = content.transform;

            // 关闭按钮
            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(panel.transform, false);
            closeBtn.AddComponent<RectTransform>();
            closeBtn.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.1f, 1f);
            closeBtn.AddComponent<LayoutElement>().preferredHeight = 40;
            var cBtn = closeBtn.AddComponent<Button>();
            cBtn.onClick.AddListener(() => gameObject.SetActive(false));
            var cTxt = CreateText(closeBtn.transform, "关闭", 20, Color.white);
            cTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            cTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            cTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            cTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
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
    }
}
