using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    public class TreasureUIController : MonoBehaviour, IController
    {
        [SerializeField] private Image rewardImage;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform rewardSlotsRoot;
        [SerializeField] private Button confirmButton;
        private TMP_FontAsset _font;

        private bool _rewardClaimed;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
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
                canvas.sortingOrder = 50;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }
            else if (canvas.sortingOrder < 50)
            {
                canvas.sortingOrder = 50;
                if (GetComponent<GraphicRaycaster>() == null)
                    gameObject.AddComponent<GraphicRaycaster>();
            }

            // 清理prefab静态子物体
            Transform panel = transform.Find("Panel");
            if (panel != null)
            {
                for (int i = panel.childCount - 1; i >= 0; i--)
                    DestroyImmediate(panel.GetChild(i).gameObject);
                var vlg = panel.GetComponent<VerticalLayoutGroup>();
                if (vlg != null) DestroyImmediate(vlg);
                var csf = panel.GetComponent<ContentSizeFitter>();
                if (csf != null) DestroyImmediate(csf);
            }
            if (panel == null)
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
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.85f); titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "宝箱"; titleTmp.fontSize = 28; titleTmp.color = new Color(0.9f, 0.8f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            if (_font) titleTmp.font = _font;

            // 描述
            var dObj = new GameObject("DescriptionText");
            dObj.transform.SetParent(panel, false);
            var dRt = dObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.05f, 0.45f); dRt.anchorMax = new Vector2(0.95f, 0.85f);
            dRt.offsetMin = Vector2.zero; dRt.offsetMax = Vector2.zero;
            descriptionText = dObj.AddComponent<TextMeshProUGUI>();
            descriptionText.fontSize = 20; descriptionText.color = Color.white;
            descriptionText.alignment = TextAlignmentOptions.Center;
            if (_font) descriptionText.font = _font;

            // 奖励区
            var rewardObj = new GameObject("RewardArea");
            rewardObj.transform.SetParent(panel, false);
            var rRt = rewardObj.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.05f, 0.25f); rRt.anchorMax = new Vector2(0.95f, 0.45f);
            rRt.offsetMin = Vector2.zero; rRt.offsetMax = Vector2.zero;
            rewardObj.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.6f);
            rewardSlotsRoot = rewardObj.transform;

            // 确认按钮
            var cObj = new GameObject("ConfirmButton");
            cObj.transform.SetParent(panel, false);
            var cRt = cObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.3f, 0.05f); cRt.anchorMax = new Vector2(0.7f, 0.2f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            cObj.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 1f);

            var ctObj = new GameObject("Text");
            ctObj.transform.SetParent(cObj.transform, false);
            var ctRt = ctObj.AddComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero; ctRt.offsetMax = Vector2.zero;
            var ctTmp = ctObj.AddComponent<TextMeshProUGUI>();
            ctTmp.text = "确认拾取"; ctTmp.fontSize = 20; ctTmp.color = Color.white;
            ctTmp.alignment = TextAlignmentOptions.Center;
            if (_font) ctTmp.font = _font;

            confirmButton = cObj.AddComponent<Button>();
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                OnConfirm();
            });
        }

        public void ShowTreasure(string desc)
        {
            _rewardClaimed = false;
            if (descriptionText) descriptionText.text = desc;
            // 清理旧奖励显示
            if (rewardSlotsRoot != null)
            {
                for (int i = rewardSlotsRoot.childCount - 1; i >= 0; i--)
                    Destroy(rewardSlotsRoot.GetChild(i).gameObject);
            }
            // 按钮文字恢复
            var btnTmp = confirmButton?.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTmp) btnTmp.text = "确认拾取";
            gameObject.SetActive(true);
        }

        public void OnConfirm()
        {
            if (!_rewardClaimed)
            {
                // 发放宝箱奖励
                GrantTreasureReward();
                _rewardClaimed = true;
                // 更新按钮文字
                var btnTmp = confirmButton?.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTmp) btnTmp.text = "离开";
                return;
            }

            // 第二次点击关闭
            gameObject.SetActive(false);
            var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
            if (mapMgr != null) mapMgr.SendMessage("BuildMap");
        }

        private void GrantTreasureReward()
        {
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            var rewards = new System.Collections.Generic.List<string>();

            // 1. 灵石奖励
            int gold = Random.Range(30, 80);
            var arch = CardGameArchitecture.Interface;
            arch.GetModel<IBattleModel>().CurrentGold.Value += gold;
            gm.PersistentGameplayData.CurrentGold = arch.GetModel<IBattleModel>().CurrentGold.Value;
            rewards.Add($"+{gold}灵石");

            // 2. 随机灵材
            GrantRandomMaterial(rewards);

            // 3. 50%几率额外药水
            if (Random.value < 0.5f)
            {
                var potionDir = "Assets/NueGames/NueDeck/Data/Potions";
                var potionGuids = UnityEditor.AssetDatabase.FindAssets("t:PotionData", new[] { potionDir });
                if (potionGuids.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(potionGuids[Random.Range(0, potionGuids.Length)]);
                    var potion = UnityEditor.AssetDatabase.LoadAssetAtPath<PotionData>(path);
                    if (potion != null)
                    {
                        arch.GetSystem<IPotionSystem>().ObtainPotion(potion);
                        rewards.Add($"药水: {potion.name}");
                    }
                }
            }

            // 显示奖励在UI上
            if (rewardSlotsRoot != null)
            {
                var rewardObj = new GameObject("RewardText");
                rewardObj.transform.SetParent(rewardSlotsRoot, false);
                var rt = rewardObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(10, 5); rt.offsetMax = new Vector2(-10, -5);
                var tmp = rewardObj.AddComponent<TextMeshProUGUI>();
                tmp.text = string.Join("\n", rewards);
                tmp.fontSize = 20; tmp.color = new Color(1f, 0.85f, 0f);
                tmp.alignment = TextAlignmentOptions.Center;
                if (_font) tmp.font = _font;
            }

            FloatingTip.ShowSuccess("宝箱开启! " + string.Join(" ", rewards));
            Debug.Log($"[Treasure] 宝箱奖励: {string.Join(", ", rewards)}");
        }

        private void GrantRandomMaterial(System.Collections.Generic.List<string> rewards)
        {
            var matDir = "Assets/NueGames/NueDeck/Data/Materials";
            var guids = UnityEditor.AssetDatabase.FindAssets("t:MaterialData", new[] { matDir });
            if (guids.Length == 0) return;

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[Random.Range(0, guids.Length)]);
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialData>(path);
            if (mat != null)
            {
                this.GetSystem<IInventorySystem>().AddItem(mat, 1);
                rewards.Add($"灵材: {mat.name}");
            }
        }
    }
}
