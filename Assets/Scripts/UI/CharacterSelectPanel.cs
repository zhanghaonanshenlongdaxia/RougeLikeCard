using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace CardGame.UI
{
    /// <summary>
    /// 新游戏角色选择面板 — 选择立绘后进入游戏。
    /// 从MainMenuController.OnNewGame调用Show()。
    /// </summary>
    public class CharacterSelectPanel : MonoBehaviour
    {
        public static CharacterSelectPanel Instance { get; private set; }

        [SerializeField] private Transform portraitContainer;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI descriptionText;

        private int _selectedIndex = -1;
        private List<Sprite> _portraits = new List<Sprite>();
        private List<GameObject> _portraitButtons = new List<GameObject>();
        private List<string> _characterNames = new List<string>();
        private List<string> _characterDescs = new List<string>();
        private static GameObject _prefab;

        private void Awake()
        {
            AutoBindReferences();
            if (cancelButton != null) cancelButton.onClick.AddListener(() => gameObject.SetActive(false));
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        }

        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            var scrollArea = panel.Find("ScrollArea");
            if (scrollArea != null)
            {
                var viewport = scrollArea.Find("Viewport");
                if (viewport != null)
                {
                    var content = viewport.Find("Content");
                    if (content != null) portraitContainer = content;
                }
            }

            if (descriptionText == null) descriptionText = panel.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            if (confirmButton == null) confirmButton = panel.Find("ConfirmButton")?.GetComponent<Button>();
            if (cancelButton == null) cancelButton = panel.Find("CancelButton")?.GetComponent<Button>();
        }

        public void Show()
        {
            LoadPortraits();
            RefreshPortraitList();
            if (descriptionText != null) descriptionText.text = "选择你的修仙者形象";
            if (confirmButton != null) confirmButton.interactable = false;
            _selectedIndex = -1;
            gameObject.SetActive(true);
        }

        private void LoadPortraits()
        {
            _portraits.Clear();
            _characterNames.Clear();
            _characterDescs.Clear();

#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/NueGames/NueDeck/Sprites/PlayerPortraits" });
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
                {
                    importer.textureType = UnityEditor.TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }

                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    _portraits.Add(sprite);
                    _characterNames.Add(System.IO.Path.GetFileNameWithoutExtension(path));
                    _characterDescs.Add("");
                }
            }
#else
            var sprites = Resources.LoadAll<Sprite>("PlayerPortraits");
            foreach (var s in sprites) { _portraits.Add(s); _characterNames.Add(s.name); _characterDescs.Add(""); }
#endif
        }

        private void RefreshPortraitList()
        {
            // 清除旧按钮（保留模板）
            for (int i = _portraitButtons.Count - 1; i >= 0; i--)
                if (_portraitButtons[i] != null) Destroy(_portraitButtons[i]);
            _portraitButtons.Clear();

            if (portraitContainer == null)
            {
                var panel = transform.Find("Panel");
                var content = panel?.Find("ScrollArea/Viewport/Content");
                if (content != null) portraitContainer = content;
            }
            if (portraitContainer == null) { Debug.LogError("[CharacterSelect] portraitContainer is null!"); return; }

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            Debug.Log($"[CharacterSelect] Creating {_portraits.Count} portrait buttons");

            for (int i = 0; i < _portraits.Count; i++)
            {
                var go = new GameObject($"Portrait_{i}");
                go.transform.SetParent(portraitContainer, false);

                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 280);

                var img = go.AddComponent<Image>();
                img.sprite = _portraits[i];
                img.preserveAspect = true;
                img.color = i == _selectedIndex ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);

                var btn = go.AddComponent<Button>();
                var captured = i;
                btn.onClick.AddListener(() => SelectPortrait(captured));

                // 名字
                var nameGo = new GameObject("NameText");
                nameGo.transform.SetParent(go.transform, false);
                var nameRt = nameGo.AddComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0, 0);
                nameRt.anchorMax = new Vector2(1, 0);
                nameRt.pivot = new Vector2(0.5f, 0);
                nameRt.anchoredPosition = new Vector2(0, 5);
                nameRt.sizeDelta = new Vector2(0, 25);
                var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
                nameTmp.text = _characterNames[i];
                nameTmp.fontSize = 16;
                nameTmp.alignment = TextAlignmentOptions.Center;
                nameTmp.color = Color.white;
                if (font) nameTmp.font = font;

                _portraitButtons.Add(go);
                Debug.Log($"  Created portrait button {i}: {_characterNames[i]}");
            }
        }

        private void SelectPortrait(int index)
        {
            _selectedIndex = index;

            // 更新高亮
            for (int i = 0; i < _portraitButtons.Count; i++)
            {
                var img = _portraitButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == index ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            }

            if (descriptionText != null && index < _characterDescs.Count)
                descriptionText.text = $"已选择：{_characterNames[index]}";

            if (confirmButton != null) confirmButton.interactable = true;
        }

        private void OnConfirm()
        {
            if (_selectedIndex < 0) return;

            // 保存选择到PlayerPrefs
            PlayerPrefs.SetInt("SelectedPortraitIndex", _selectedIndex);
            PlayerPrefs.Save();

            gameObject.SetActive(false);

            // 调用新游戏初始化
            StartNewGame();
        }

        private void StartNewGame()
        {
            SaveSystem.DeleteSave();
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm != null) gm.InitGameplayData();

            var arch = CardGameArchitecture.Interface;
            var cultSystem = arch.GetSystem<ICultivationSystem>();
            cultSystem.LearnMethod("sy_method");
            cultSystem.LearnMethod("th_method");
            cultSystem.LearnMethod("cc_method");
            cultSystem.SetActiveMethod("cc_method");
            arch.GetModel<ICultivationModel>().ComprehensionPoints.Value = 20;

            // 世界地图+时间初始化（新玩法：冒险RPG从世界地图开始）
            arch.GetSystem<IWorldMapSystem>().InitNewGame();

            FloatingTip.ShowSuccess("开始新游戏，已获得三本初始功法");
            // 进入世界地图（青石镇起点）
            UnityEngine.SceneManagement.SceneManager.LoadScene("6- WorldMap");
        }

        public static CharacterSelectPanel GetOrCreate()
        {
            if (Instance != null) return Instance;

            var existing = GameObject.Find("CharacterSelectPanel");
            if (existing != null)
            {
                Instance = existing.GetComponent<CharacterSelectPanel>();
                if (Instance != null) return Instance;
            }

            // 从Prefab加载
            GameObject go = null;
#if UNITY_EDITOR
            go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/MainMenu/CharacterSelectPanel.prefab");
#endif
            if (go == null)
                go = Resources.Load<GameObject>("Prefabs/UI/MainMenu/CharacterSelectPanel");

            if (go != null)
            {
                var instance = Instantiate(go);
                instance.name = "CharacterSelectPanel";
                Instance = instance.GetComponent<CharacterSelectPanel>();
                if (Instance == null) Instance = instance.AddComponent<CharacterSelectPanel>();
            }
            else
            {
                // 兜底：运行时创建
                go = new GameObject("CharacterSelectPanel");
                Instance = go.AddComponent<CharacterSelectPanel>();
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 250;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                go.AddComponent<GraphicRaycaster>();
                BuildUI(go.transform);
            }

            DontDestroyOnLoad(Instance.gameObject);
            return Instance;
        }

        private static void BuildUI(Transform parent)
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // BG
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(parent, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            Stretch(bgRt);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.9f);

            // Panel
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(parent, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.15f, 0.1f);
            panelRt.anchorMax = new Vector2(0.85f, 0.9f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.06f, 0.09f, 0.14f, 0.98f);

            // Title
            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0.9f);
            titleRt.anchorMax = new Vector2(1, 1);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "选择修仙者";
            titleTmp.fontSize = 28;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.85f, 0.3f);
            if (font) titleTmp.font = font;

            // ScrollArea
            var scrollGo = new GameObject("ScrollArea");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.05f, 0.2f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.85f);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewportGo.AddComponent<RectTransform>();
            Stretch(vpRt);
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.01f);
            viewportGo.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(0, 1);
            contentRt.pivot = new Vector2(0, 0.5f);
            var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;

            // Description
            var descGo = new GameObject("DescriptionText");
            descGo.transform.SetParent(panelGo.transform, false);
            var descRt = descGo.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.05f, 0.1f);
            descRt.anchorMax = new Vector2(0.55f, 0.18f);
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = "选择你的修仙者形象";
            descTmp.fontSize = 18;
            descTmp.alignment = TextAlignmentOptions.Left;
            descTmp.color = new Color(0.85f, 0.85f, 0.9f);
            if (font) descTmp.font = font;

            // Confirm Button
            var confirmGo = new GameObject("ConfirmButton");
            confirmGo.transform.SetParent(panelGo.transform, false);
            var confirmRt = confirmGo.AddComponent<RectTransform>();
            confirmRt.anchorMin = new Vector2(0.6f, 0.1f);
            confirmRt.anchorMax = new Vector2(0.78f, 0.18f);
            var confirmImg = confirmGo.AddComponent<Image>();
            confirmImg.color = new Color(0.08f, 0.5f, 0.12f, 0.9f);
            var confirmBtn = confirmGo.AddComponent<Button>();
            var confirmTxtGo = new GameObject("Text");
            confirmTxtGo.transform.SetParent(confirmGo.transform, false);
            confirmTxtGo.AddComponent<RectTransform>();
            var confirmRt2 = confirmTxtGo.GetComponent<RectTransform>();
            Stretch(confirmRt2);
            var confirmTxt = confirmTxtGo.AddComponent<TextMeshProUGUI>();
            confirmTxt.text = "确认";
            confirmTxt.fontSize = 22;
            confirmTxt.alignment = TextAlignmentOptions.Center;
            confirmTxt.color = Color.white;
            if (font) confirmTxt.font = font;

            // Cancel Button
            var cancelGo = new GameObject("CancelButton");
            cancelGo.transform.SetParent(panelGo.transform, false);
            var cancelRt = cancelGo.AddComponent<RectTransform>();
            cancelRt.anchorMin = new Vector2(0.82f, 0.1f);
            cancelRt.anchorMax = new Vector2(0.95f, 0.18f);
            var cancelImg = cancelGo.AddComponent<Image>();
            cancelImg.color = new Color(0.5f, 0.15f, 0.15f, 0.9f);
            var cancelBtn = cancelGo.AddComponent<Button>();
            var cancelTxtGo = new GameObject("Text");
            cancelTxtGo.transform.SetParent(cancelGo.transform, false);
            cancelTxtGo.AddComponent<RectTransform>();
            var cancelRt2 = cancelTxtGo.GetComponent<RectTransform>();
            Stretch(cancelRt2);
            var cancelTxt = cancelTxtGo.AddComponent<TextMeshProUGUI>();
            cancelTxt.text = "返回";
            cancelTxt.fontSize = 22;
            cancelTxt.alignment = TextAlignmentOptions.Center;
            cancelTxt.color = Color.white;
            if (font) cancelTxt.font = font;
        }

        private static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
