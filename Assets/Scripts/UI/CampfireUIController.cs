using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Collection;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    public class CampfireUIController : MonoBehaviour, IController
    {
        [SerializeField] private Button restButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button backButton;
        private Transform _upgradeCardListRoot;
        private TextMeshProUGUI _statusText;
        private TMP_FontAsset _font;

        private ICampfireSystem _system;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _system = this.GetSystem<ICampfireSystem>();
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI();
        }

        private void BuildUI()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 60;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }
            else if (canvas.sortingOrder < 60)
            {
                canvas.sortingOrder = 60;
                if (GetComponent<GraphicRaycaster>() == null)
                    gameObject.AddComponent<GraphicRaycaster>();
            }

            // 清理prefab静态子物体和布局组件
            Transform panel = transform.Find("Panel");
            if (panel != null)
            {
                for (int i = panel.childCount - 1; i >= 0; i--)
                    DestroyImmediate(panel.GetChild(i).gameObject);
                // 移除prefab上的布局组件，子物体用锚点定位
                var pVLG = panel.GetComponent<VerticalLayoutGroup>();
                if (pVLG != null) DestroyImmediate(pVLG);
                var pCSF = panel.GetComponent<ContentSizeFitter>();
                if (pCSF != null) DestroyImmediate(pCSF);
                var pHLG = panel.GetComponent<HorizontalLayoutGroup>();
                if (pHLG != null) DestroyImmediate(pHLG);
            }
            else
            {
                var panelObj = new GameObject("Panel");
                panelObj.transform.SetParent(transform, false);
                var panelRt = panelObj.AddComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.25f, 0.15f); panelRt.anchorMax = new Vector2(0.75f, 0.85f);
                panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
                panelObj.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.95f);
                panel = panelObj.transform;
            }

            // 标题
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.88f); tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "休息处"; tTmp.fontSize = 28; tTmp.color = new Color(0.9f, 0.8f, 0.3f);
            tTmp.alignment = TextAlignmentOptions.Center;
            if (_font) tTmp.font = _font;

            // 描述
            var descObj = new GameObject("Description");
            descObj.transform.SetParent(panel, false);
            var dRt = descObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.05f, 0.75f); dRt.anchorMax = new Vector2(0.95f, 0.88f);
            dRt.offsetMin = Vector2.zero; dRt.offsetMax = Vector2.zero;
            var dTmp = descObj.AddComponent<TextMeshProUGUI>();
            dTmp.text = "你找到一处安静的修炼之地。\n是休息恢复体力，还是精进功法？";
            dTmp.fontSize = 18; dTmp.color = Color.white;
            dTmp.alignment = TextAlignmentOptions.Center;
            if (_font) dTmp.font = _font;

            // 按钮行
            var btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(panel, false);
            var bRt = btnRow.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.1f, 0.55f); bRt.anchorMax = new Vector2(0.9f, 0.72f);
            bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            // 休息按钮
            restButton = CreateActionButton(btnRow.transform, "休息", "回复30%最大生命", new Color(0.15f, 0.3f, 0.5f, 1f));
            restButton.onClick.AddListener(OnRest);

            // 精进按钮
            upgradeButton = CreateActionButton(btnRow.transform, "精进", "升级一张卡牌", new Color(0.3f, 0.2f, 0.1f, 1f));
            upgradeButton.onClick.AddListener(ShowUpgradeCards);

            // 状态文字
            var statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(panel, false);
            var sRt = statusObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.05f, 0.42f); sRt.anchorMax = new Vector2(0.95f, 0.55f);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;
            _statusText = statusObj.AddComponent<TextMeshProUGUI>();
            _statusText.fontSize = 18; _statusText.color = new Color(0.3f, 1f, 0.3f);
            _statusText.alignment = TextAlignmentOptions.Center;
            if (_font) _statusText.font = _font;

            // 升级卡牌列表区域
            var listObj = new GameObject("UpgradeCardList");
            listObj.transform.SetParent(panel, false);
            var lRt = listObj.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.05f, 0.05f); lRt.anchorMax = new Vector2(0.85f, 0.42f);
            lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
            listObj.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.6f);
            var vlg = listObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5; vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlWidth = true; vlg.childForceExpandHeight = false;
            var csf = listObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _upgradeCardListRoot = listObj.transform;
            listObj.SetActive(false);

            // 离开按钮
            var closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(panel, false);
            var cRt = closeObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.35f, 0.05f); cRt.anchorMax = new Vector2(0.65f, 0.15f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            closeObj.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 1f);
            var cTxt = CreateTextObject(closeObj.transform, "离开", 20, Color.white);
            cTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            cTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            cTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            cTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            backButton = closeObj.AddComponent<Button>();
            backButton.onClick.AddListener(() => { if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick); OnBack(); });
        }

        private Button CreateActionButton(Transform parent, string label, string sub, Color color)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3; vlg.padding = new RectOffset(5, 5, 5, 5);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            CreateTextObject(go.transform, label, 22, Color.white);
            CreateTextObject(go.transform, sub, 14, new Color(0.8f, 0.8f, 0.8f));
            return go.AddComponent<Button>();
        }

        private void OnRest()
        {
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
            _system.Rest();
            FloatingTip.ShowSuccess("休息恢复30%生命");
            gameObject.SetActive(false);
            NotifyMapRefresh();
        }

        private void ShowUpgradeCards()
        {
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            var upgradeable = new List<CardData>();
            foreach (var card in gm.PersistentGameplayData.CurrentCardsList)
            {
                if (card.HasUpgradeData && !card.IsUpgraded && !upgradeable.Contains(card))
                    upgradeable.Add(card);
            }

            if (upgradeable.Count == 0)
            {
                _statusText.text = "没有可升级的卡牌";
                _upgradeCardListRoot.gameObject.SetActive(false);
                FloatingTip.ShowWarning("没有可升级的卡牌");
                return;
            }

            // 清理旧列表
            for (int i = _upgradeCardListRoot.childCount - 1; i >= 0; i--)
                Destroy(_upgradeCardListRoot.GetChild(i).gameObject);

            _statusText.text = "选择一张卡牌进行升级：";
            _upgradeCardListRoot.gameObject.SetActive(true);

            foreach (var card in upgradeable)
            {
                var go = new GameObject("Card_" + card.Id);
                go.transform.SetParent(_upgradeCardListRoot, false);
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10; hlg.padding = new RectOffset(5, 5, 5, 5);
                hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
                go.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1f);
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = 50;

                CreateTextObject(go.transform, card.CardName, 16, Color.white);
                CreateTextObject(go.transform, "→ " + card.CardName + "+", 16, new Color(0.3f, 0.8f, 0.3f));

                var btn = go.AddComponent<Button>();
                var captured = card;
                btn.onClick.AddListener(() =>
                {
                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                    _system.UpgradeCard(captured);
                    FloatingTip.ShowSuccess($"升级卡牌: {captured.CardName} → {captured.CardName}+");
                    gameObject.SetActive(false);
                    NotifyMapRefresh();
                });
            }
        }

        public void OnBack()
        {
            gameObject.SetActive(false);
            NotifyMapRefresh();
        }

        private void NotifyMapRefresh()
        {
            var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
            if (mapMgr != null) mapMgr.SendMessage("BuildMap");
        }

        private TextMeshProUGUI CreateTextObject(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = size; tmp.color = color;
            if (_font) tmp.font = _font;
            return tmp;
        }
    }
}
