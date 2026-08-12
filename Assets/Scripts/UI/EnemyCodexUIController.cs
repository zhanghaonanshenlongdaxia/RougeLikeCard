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
    public class EnemyCodexUIController : MonoBehaviour, IController, LoopScrollDataSource
    {
        private LoopVerticalScrollRect _loopScroll;
        private LoopScrollPrefabSourceImpl _prefabSource;
        private Transform _listContent;
        private GameObject _itemTemplate;
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
            AutoBindReferences();
        }


        private void OnEnable()
        {
            RefreshList();
        }

        private List<EnemyCharacterData> _allEnemies = new List<EnemyCharacterData>();

        private void RefreshList()
        {
            // Load all enemy SOs
            var guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyCharacterData", new[]{"Assets/NueGames/NueDeck/Data/Enemies"});
            var codex = this.GetSystem<IEnemyCodexSystem>();
            _allEnemies.Clear();

            foreach (var g in guids)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var e = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyCharacterData>(p);
                if (e != null) _allEnemies.Add(e);
            }

            // Sort by region then tier then name
            _allEnemies.Sort((a, b) => {
                int ra = a.RegionId, rb = b.RegionId;
                if (ra != rb) return ra.CompareTo(rb);
                int ta = (int)a.EnemyTier, tb = (int)b.EnemyTier;
                if (ta != tb) return ta.CompareTo(tb);
                return a.name.CompareTo(b.name);
            });

            if (_loopScroll != null)
            {
                _loopScroll.totalCount = _allEnemies.Count;
                _loopScroll.RefillCells();
            }
        }

        public void ProvideData(Transform transform, int idx)
        {
            if (idx < 0 || idx >= _allEnemies.Count) return;
            var enemy = _allEnemies[idx];
            var codex = this.GetSystem<IEnemyCodexSystem>();
            bool unlocked = codex != null && codex.IsUnlocked(GetId(enemy));
            string displayName = unlocked ? GetName(enemy) : "???";
            string tierStr = unlocked ? $"[{enemy.EnemyTier}]" : "";

            var img = transform.GetComponent<Image>();
            if (img) img.color = unlocked ? new Color(0.1f, 0.15f, 0.25f, 1f) : new Color(0.05f, 0.05f, 0.05f, 0.5f);

            var tmps = transform.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmps.Length > 0)
            {
                tmps[0].text = $"{tierStr} {displayName}";
                tmps[0].color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            }

            var btn = transform.GetComponent<Button>();
            if (btn == null) btn = transform.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            var captured = enemy;
            var capturedUnlocked = unlocked;
            btn.onClick.AddListener(() => {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                ShowDetail(captured, capturedUnlocked);
            });
        }

        private GameObject CreateListItemTemplate()
        {
            var go = new GameObject("EnemyItem");
            go.AddComponent<RectTransform>();
            var btnImg = go.AddComponent<Image>();
            btnImg.color = new Color(0.1f, 0.15f, 0.25f, 1f);
            var layout = go.AddComponent<LayoutElement>(); layout.preferredHeight = 35;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(go.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10, 0); txtRt.offsetMax = new Vector2(-10, 0);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 16; tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            if (_font) tmp.font = _font;

            var btn = go.AddComponent<Button>();
            return go;
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
        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;
            _listContent = panel.Find("ListPanel/ScrollView/Viewport/Content");
            var detail = panel.Find("DetailPanel");
            if (detail != null)
            {
                _portraitImage = detail.Find("PortraitImage")?.GetComponent<Image>();
                _nameText = detail.Find("NameText")?.GetComponent<TMPro.TextMeshProUGUI>();
                _tierText = detail.Find("TierText")?.GetComponent<TMPro.TextMeshProUGUI>();
                _hpText = detail.Find("HPText")?.GetComponent<TMPro.TextMeshProUGUI>();
                _skillText = detail.Find("SkillText")?.GetComponent<TMPro.TextMeshProUGUI>();
                _storyText = detail.Find("StoryText")?.GetComponent<TMPro.TextMeshProUGUI>();
            }
            _closeButton = panel.Find("CloseButton")?.GetComponent<UnityEngine.UI.Button>();
        }

    }

}
