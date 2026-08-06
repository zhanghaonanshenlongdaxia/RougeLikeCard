using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using QFramework;
using CardGame.Audio;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Enums;
using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;

namespace CardGame.UI
{
    /// <summary>
    /// 敌人图鉴UI控制器。运行时动态创建，不需要预制体。
    /// 左侧列表（按区域/品阶分类），右侧详情（名字/品阶/HP/技能/背景故事）。
    /// 未解锁的敌人显示???。
    /// </summary>
    public class EnemyCodexUIController : MonoBehaviour, IController
    {
        private ScrollRect _scrollRect;
        private Transform _listContent;
        private Image _portraitImage;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _tierText;
        private TextMeshProUGUI _hpText;
        private TextMeshProUGUI _skillText;
        private TextMeshProUGUI _storyText;
        private Button _closeButton;
        private TMP_FontAsset _font;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI();
        }

        private void BuildUI()
        {
            // Canvas
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            // BG
            var bg = CreateImage("BG", transform, new Color(0.05f, 0.05f, 0.08f, 0.95f));
            SetStretch(bg.rectTransform);

            // Main panel
            var panel = CreateImage("Panel", transform, new Color(0.08f, 0.1f, 0.15f, 1f));
            panel.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
            panel.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;

            // Title
            var title = CreateText("Title", panel.transform, "敌人图鉴", 32, new Color(0.9f, 0.8f, 0.3f));
            title.alignment = TextAlignmentOptions.Center;
            title.rectTransform.anchorMin = new Vector2(0f, 0.93f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(0, 0); title.rectTransform.offsetMax = new Vector2(0, -5);

            // Close button
            var closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(panel.transform, false);
            var closeRt = closeObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.92f, 0.93f); closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.offsetMin = new Vector2(5, 5); closeRt.offsetMax = new Vector2(-5, -5);
            var closeImg = closeObj.AddComponent<Image>(); closeImg.color = new Color(0.6f, 0.15f, 0.15f, 1f);
            var closeTxtObj = new GameObject("Text"); closeTxtObj.transform.SetParent(closeObj.transform, false);
            var closeTxtRt = closeTxtObj.AddComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero; closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.offsetMin = Vector2.zero; closeTxtRt.offsetMax = Vector2.zero;
            var closeTmp = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "关闭"; closeTmp.alignment = TextAlignmentOptions.Center; closeTmp.fontSize = 18; closeTmp.color = Color.white;
            if (_font) closeTmp.font = _font;
            _closeButton = closeObj.AddComponent<Button>();
            _closeButton.onClick.AddListener(() => {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                gameObject.SetActive(false);
            });

            // Left list (scroll)
            var listPanel = CreateImage("ListPanel", panel.transform, new Color(0.05f, 0.08f, 0.12f, 0.8f));
            listPanel.rectTransform.anchorMin = new Vector2(0.02f, 0.05f);
            listPanel.rectTransform.anchorMax = new Vector2(0.35f, 0.9f);
            listPanel.rectTransform.offsetMin = Vector2.zero; listPanel.rectTransform.offsetMax = Vector2.zero;

            var scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(listPanel.transform, false);
            var scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(5, 5); scrollRt.offsetMax = new Vector2(-5, -5);
            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;

            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRt = viewportObj.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero; viewportRt.offsetMax = Vector2.zero;
            viewportRt.pivot = new Vector2(0, 1);
            var viewportImg = viewportObj.AddComponent<Image>(); viewportImg.color = new Color(0, 0, 0, 0.01f);
            _scrollRect.viewport = viewportImg.rectTransform;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0, 1);
            contentRt.offsetMin = Vector2.zero; contentRt.offsetMax = Vector2.zero;
            var contentFitter = contentObj.AddComponent<VerticalLayoutGroup>();
            contentFitter.spacing = 2; contentFitter.childAlignment = TextAnchor.UpperCenter;
            contentFitter.childControlWidth = true; contentFitter.childControlHeight = false;
            var fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scrollRect.content = contentRt;
            _listContent = contentObj.transform;

            // Right detail
            var detailPanel = CreateImage("DetailPanel", panel.transform, new Color(0.05f, 0.08f, 0.12f, 0.8f));
            detailPanel.rectTransform.anchorMin = new Vector2(0.37f, 0.05f);
            detailPanel.rectTransform.anchorMax = new Vector2(0.98f, 0.9f);
            detailPanel.rectTransform.offsetMin = Vector2.zero; detailPanel.rectTransform.offsetMax = Vector2.zero;

            _nameText = CreateText("NameText", detailPanel.transform, "", 28, new Color(0.9f, 0.8f, 0.3f));
            _nameText.rectTransform.anchorMin = new Vector2(0.05f, 0.85f); _nameText.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            _nameText.rectTransform.offsetMin = Vector2.zero; _nameText.rectTransform.offsetMax = Vector2.zero;

            _tierText = CreateText("TierText", detailPanel.transform, "", 20, new Color(0.6f, 0.7f, 0.9f));
            _tierText.rectTransform.anchorMin = new Vector2(0.05f, 0.78f); _tierText.rectTransform.anchorMax = new Vector2(0.95f, 0.85f);
            _tierText.rectTransform.offsetMin = Vector2.zero; _tierText.rectTransform.offsetMax = Vector2.zero;

            _hpText = CreateText("HPText", detailPanel.transform, "", 20, new Color(0.9f, 0.4f, 0.4f));
            _hpText.rectTransform.anchorMin = new Vector2(0.05f, 0.72f); _hpText.rectTransform.anchorMax = new Vector2(0.95f, 0.78f);
            _hpText.rectTransform.offsetMin = Vector2.zero; _hpText.rectTransform.offsetMax = Vector2.zero;

            _skillText = CreateText("SkillText", detailPanel.transform, "", 16, Color.white);
            _skillText.rectTransform.anchorMin = new Vector2(0.05f, 0.4f); _skillText.rectTransform.anchorMax = new Vector2(0.95f, 0.72f);
            _skillText.rectTransform.offsetMin = Vector2.zero; _skillText.rectTransform.offsetMax = Vector2.zero;

            _storyText = CreateText("StoryText", detailPanel.transform, "", 16, new Color(0.7f, 0.75f, 0.8f));
            _storyText.rectTransform.anchorMin = new Vector2(0.05f, 0.02f); _storyText.rectTransform.anchorMax = new Vector2(0.95f, 0.4f);
            _storyText.rectTransform.offsetMin = Vector2.zero; _storyText.rectTransform.offsetMax = Vector2.zero;
        }

        private void OnEnable()
        {
            RefreshList();
        }

        private void RefreshList()
        {
            // Clear
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            // Load all enemy SOs
            var guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyCharacterData", new[]{"Assets/NueGames/NueDeck/Data/Enemies"});
            var codex = this.GetSystem<IEnemyCodexSystem>();
            var allEnemies = new List<EnemyCharacterData>();

            foreach (var g in guids)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var e = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyCharacterData>(p);
                if (e != null) allEnemies.Add(e);
            }

            // Sort by region then tier then name
            allEnemies.Sort((a, b) => {
                int ra = a.RegionId, rb = b.RegionId;
                if (ra != rb) return ra.CompareTo(rb);
                int ta = (int)a.EnemyTier, tb = (int)b.EnemyTier;
                if (ta != tb) return ta.CompareTo(tb);
                return a.name.CompareTo(b.name);
            });

            string currentRegion = "";
            foreach (var enemy in allEnemies)
            {
                string regionName = enemy.RegionId switch {
                    0 => "山野荒原", 1 => "幽冥秘境", 2 => "万蛊沼泽", 3 => "天魔裂隙", _ => "未知"
                };
                if (regionName != currentRegion)
                {
                    currentRegion = regionName;
                    var header = CreateText($"Header_{regionName}", _listContent, $"--- {regionName} ---", 18, new Color(0.5f, 0.6f, 0.8f));
                    header.alignment = TextAlignmentOptions.Center;
                }

                bool unlocked = codex != null && codex.IsUnlocked(GetId(enemy));
                string displayName = unlocked ? GetName(enemy) : "???";
                string tierStr = unlocked ? $"[{enemy.EnemyTier}]" : "";

                var btnObj = new GameObject($"Enemy_{GetId(enemy)}");
                btnObj.transform.SetParent(_listContent, false);
                var btnRt = btnObj.AddComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(0, 35);
                var btnImg = btnObj.AddComponent<Image>();
                btnImg.color = unlocked ? new Color(0.1f, 0.15f, 0.25f, 1f) : new Color(0.05f, 0.05f, 0.05f, 0.5f);
                var layout = btnObj.AddComponent<LayoutElement>(); layout.preferredHeight = 35;

                var txtObj = new GameObject("Text");
                txtObj.transform.SetParent(btnObj.transform, false);
                var txtRt = txtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(10, 0); txtRt.offsetMax = new Vector2(-10, 0);
                var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = $"{tierStr} {displayName}";
                tmp.fontSize = 16; tmp.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f);
                tmp.alignment = TextAlignmentOptions.Left;
                if (_font) tmp.font = _font;

                var btn = btnObj.AddComponent<Button>();
                var captured = enemy;
                var capturedUnlocked = unlocked;
                btn.onClick.AddListener(() => {
                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                    ShowDetail(captured, capturedUnlocked);
                });
            }
        }

        private void ShowDetail(EnemyCharacterData enemy, bool unlocked)
        {
            if (!unlocked)
            {
                _nameText.text = "???";
                _tierText.text = "";
                _hpText.text = "";
                _skillText.text = "尚未解锁此敌人信息";
                _storyText.text = "遇见此敌人后可解锁图鉴信息";
                return;
            }

            _nameText.text = GetName(enemy);
            _tierText.text = $"品阶: {TierName(enemy.EnemyTier)}  |  区域: {RegionName(enemy.RegionId)}";
            _hpText.text = $"生命: {enemy.MaxHealth}";

            // Skills
            string skills = "技能:\n";
            if (enemy.EnemyAbilityList != null)
            {
                foreach (var ab in enemy.EnemyAbilityList)
                    skills += $"  • {ab.Name}\n";
            }
            if (enemy.HasPhases)
            {
                skills += "\n多阶段:\n";
                foreach (var ph in enemy.PhaseList)
                    skills += $"  • HP≤{ph.HealthThreshold*100:F0}%: {ph.PhaseEnterName}\n";
            }
            _skillText.text = skills;

            _storyText.text = string.IsNullOrEmpty(enemy.EnemyDescription) ? "" : enemy.EnemyDescription;
        }

        // Helpers
        private string GetId(EnemyCharacterData e) => (string)typeof(EnemyCharacterData).BaseType.GetField("characterID", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(e);
        private string GetName(EnemyCharacterData e) => (string)typeof(EnemyCharacterData).BaseType.GetField("characterName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(e);
        private string TierName(EnemyTier t) => t switch { EnemyTier.Normal => "普通", EnemyTier.Elite => "精英", EnemyTier.Boss => "Boss", _ => "?" };
        private string RegionName(int r) => r switch { 0 => "山野荒原", 1 => "幽冥秘境", 2 => "万蛊沼泽", 3 => "天魔裂隙", _ => "未知" };

        private Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.richText = true;
            if (_font) tmp.font = _font;
            return tmp;
        }

        private void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
