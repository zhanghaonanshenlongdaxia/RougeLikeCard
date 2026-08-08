using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 小游戏统一面板。根据类型生成不同交互UI。
    /// 玩家完成交互后调用 ExecuteChoice 应用结果。
    /// </summary>
    public class MiniGamePanel : MonoBehaviour
    {
        private EventEffectType _gameType;
        private int _cost;
        private EventData _currentEvent;
        private int _choiceIndex;
        private TMP_FontAsset _font;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _resultText;
        private Transform _gameArea;
        private Button _confirmButton;
        private Button _cancelButton;

        // 小游戏结果回调
        private System.Action<int> _onGameComplete; // int = 最终effectValue

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        public void Init(EventEffectType gameType, int cost, EventData eventData, int choiceIndex)
        {
            _gameType = gameType;
            _cost = cost;
            _currentEvent = eventData;
            _choiceIndex = choiceIndex;
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI();
            BuildGameContent();
        }

        void BuildUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // 背景
            var bg = CreateImage("BG", transform, new Color(0, 0, 0, 0.85f));
            SetFullStretch(bg);

            // 主面板
            var panel = CreateImage("Panel", transform, new Color(0.08f, 0.1f, 0.15f, 0.98f));
            var pRt = panel.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.15f, 0.1f);
            pRt.anchorMax = new Vector2(0.85f, 0.9f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;

            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            // 标题
            var titleObj = CreateText("Title", panel.transform, GetGameTitle(_gameType), 30, new Color(0.9f, 0.8f, 0.3f));
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 40;

            // 消耗提示
            var costText = CreateText("Cost", panel.transform, $"消耗: {_cost}灵石", 20, new Color(1f, 0.85f, 0f));
            var costLe = costText.AddComponent<LayoutElement>();
            costLe.preferredHeight = 25;

            // 游戏区域
            var areaObj = new GameObject("GameArea");
            areaObj.transform.SetParent(panel.transform, false);
            var areaRt = areaObj.AddComponent<RectTransform>();
            areaObj.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.5f);
            var areaLe = areaObj.AddComponent<LayoutElement>();
            areaLe.flexibleHeight = 1;
            _gameArea = areaObj.transform;

            // 结果文字
            var resObj = CreateText("Result", panel.transform, "", 22, Color.white);
            _resultText = resObj.GetComponent<TextMeshProUGUI>();
            _resultText.alignment = TextAlignmentOptions.Center;
            var resLe = resObj.AddComponent<LayoutElement>();
            resLe.preferredHeight = 35;

            // 按钮行
            var btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(panel.transform, false);
            var bRt = btnRow.AddComponent<RectTransform>();
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;
            var bLe = btnRow.AddComponent<LayoutElement>();
            bLe.preferredHeight = 50;

            // 开始/确认按钮
            var confirmObj = new GameObject("ConfirmButton");
            confirmObj.transform.SetParent(btnRow.transform, false);
            confirmObj.AddComponent<Image>().color = new Color(0.15f, 0.4f, 0.2f, 1f);
            var cTmp = CreateTextObject(confirmObj.transform, "开始!", 22, Color.white);
            var cRt = cTmp.GetComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            _confirmButton = confirmObj.AddComponent<Button>();
            var cLe = confirmObj.AddComponent<LayoutElement>();
            cLe.preferredHeight = 45;

            // 取消按钮
            var cancelObj = new GameObject("CancelButton");
            cancelObj.transform.SetParent(btnRow.transform, false);
            cancelObj.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 1f);
            var xTmp = CreateTextObject(cancelObj.transform, "离开", 22, Color.white);
            var xRt = xTmp.GetComponent<RectTransform>();
            xRt.anchorMin = Vector2.zero; xRt.anchorMax = Vector2.one;
            xRt.offsetMin = Vector2.zero; xRt.offsetMax = Vector2.zero;
            _cancelButton = cancelObj.AddComponent<Button>();
            var xLe = cancelObj.AddComponent<LayoutElement>();
            xLe.preferredHeight = 45;

            _cancelButton.onClick.AddListener(() => { Destroy(gameObject); });
        }

        void BuildGameContent()
        {
            switch (_gameType)
            {
                case EventEffectType.MiniSlot:
                    BuildSlot();
                    break;
                case EventEffectType.MiniDice:
                    BuildDice();
                    break;
                case EventEffectType.MiniPinball:
                    BuildPinball();
                    break;
                case EventEffectType.MiniRingToss:
                    BuildRingToss();
                    break;
                case EventEffectType.MiniBalloon:
                    BuildBalloon();
                    break;
                case EventEffectType.MiniLottery:
                    BuildLottery();
                    break;
                case EventEffectType.MiniWheel:
                    BuildWheel();
                    break;
                case EventEffectType.MiniCoinFlip:
                    BuildCoinFlip();
                    break;
                case EventEffectType.MiniCardGuess:
                    BuildCardGuess();
                    break;
                case EventEffectType.MiniTreasureHunt:
                    BuildTreasureHunt();
                    break;
            }
        }

        // ========== 灵石机（老虎机）==========
        private List<TextMeshProUGUI> _slotReels = new List<TextMeshProUGUI>();
        private bool _slotSpinning;

        void BuildSlot()
        {
            var reelRow = new GameObject("ReelRow");
            reelRow.transform.SetParent(_gameArea, false);
            var rRt = reelRow.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.2f, 0.4f); rRt.anchorMax = new Vector2(0.8f, 0.7f);
            rRt.offsetMin = Vector2.zero; rRt.offsetMax = Vector2.zero;
            var hlg = reelRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            var symbols = new[] { "◆", "★", "●", "▲", "■", "✦" };
            for (int i = 0; i < 3; i++)
            {
                var reel = new GameObject($"Reel{i}");
                reel.transform.SetParent(reelRow.transform, false);
                reel.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.2f, 1f);
                var tmp = CreateTextObject(reel.transform, symbols[i], 48, new Color(1f, 0.85f, 0f));
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().anchorMax = Vector2.one;
                tmp.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                _slotReels.Add(tmp);
            }

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "摇奖!";
            _confirmButton.onClick.AddListener(() =>
            {
                if (_slotSpinning) return;
                StartCoroutine(SpinSlotRoutine());
            });
        }

        IEnumerator SpinSlotRoutine()
        {
            _slotSpinning = true;
            _confirmButton.interactable = false;
            _resultText.text = "转转转...";

            var symbols = new[] { "◆", "★", "●", "▲", "■", "✦" };
            float elapsed = 0;
            float duration = 2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                foreach (var reel in _slotReels)
                    reel.text = symbols[UnityEngine.Random.Range(0, symbols.Length)];
                yield return new WaitForSeconds(0.08f);
            }

            // 最终结果
            int roll = UnityEngine.Random.Range(0, 100);
            string result;
            int goldReward = 0;

            if (roll < 30) { result = "空奖!"; for (int i = 0; i < 3; i++) _slotReels[i].text = "✕"; }
            else if (roll < 50) { goldReward = _cost; result = $"回本 +{goldReward}灵石"; for (int i = 0; i < 3; i++) _slotReels[i].text = "●"; }
            else if (roll < 70) { goldReward = _cost * 2; result = $"小奖 +{goldReward}灵石"; for (int i = 0; i < 3; i++) _slotReels[i].text = "▲"; }
            else if (roll < 85) { goldReward = _cost * 3; result = $"中奖 +{goldReward}灵石"; for (int i = 0; i < 3; i++) _slotReels[i].text = "★"; }
            else if (roll < 92) { result = "凡品灵材!"; _slotReels[0].text = "◆"; _slotReels[1].text = "◆"; _slotReels[2].text = "●"; }
            else if (roll < 96) { result = "灵品灵材!"; _slotReels[0].text = "★"; _slotReels[1].text = "★"; _slotReels[2].text = "◆"; }
            else if (roll < 99) { result = "获得遗物!"; for (int i = 0; i < 3; i++) _slotReels[i].text = "✦"; }
            else { goldReward = _cost * 10; result = $"大奖! +{goldReward}灵石!!"; for (int i = 0; i < 3; i++) _slotReels[i].text = "✦"; }

            _resultText.text = result;
            _slotSpinning = false;
            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(goldReward, roll));
        }

        // ========== 掷骰问运 ==========
        private TextMeshProUGUI _diceText;

        void BuildDice()
        {
            _diceText = CreateTextObject(_gameArea, "?", 80, new Color(1f, 0.85f, 0f));
            var rt = _diceText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.3f, 0.3f); rt.anchorMax = new Vector2(0.7f, 0.8f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _diceText.alignment = TextAlignmentOptions.Center;

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "掷骰!";
            _confirmButton.onClick.AddListener(() => { StartCoroutine(DiceRoutine()); });
        }

        IEnumerator DiceRoutine()
        {
            _confirmButton.interactable = false;
            _resultText.text = "掷骰中...";
            for (int i = 0; i < 15; i++)
            {
                _diceText.text = "" + UnityEngine.Random.Range(1, 7);
                yield return new WaitForSeconds(0.1f);
            }

            int dice = UnityEngine.Random.Range(1, 7);
            _diceText.text = "" + dice;

            string result;
            int goldReward = 0;
            if (dice <= 2) result = $"{dice}点 空奖!";
            else if (dice <= 4) { goldReward = _cost; result = $"{dice}点 回本 +{goldReward}灵石"; }
            else if (dice == 5) { goldReward = _cost * 2; result = $"5点 双倍 +{goldReward}灵石"; }
            else { goldReward = _cost * 5; result = $"6点 五倍 +{goldReward}灵石!!"; }

            _resultText.text = result;
            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(goldReward, dice));
        }

        // ========== 灵珠弹射 ==========
        private List<RectTransform> _pinballZones = new List<RectTransform>();

        void BuildPinball()
        {
            var zoneNames = new[] { "空区", "灵石区", "凡品", "灵品", "药水区", "卡牌区" };
            var zoneColors = new[] {
                new Color(0.2f, 0.2f, 0.2f, 1f),
                new Color(0.8f, 0.7f, 0.2f, 1f),
                new Color(0.2f, 0.6f, 0.3f, 1f),
                new Color(0.2f, 0.4f, 0.8f, 1f),
                new Color(0.6f, 0.3f, 0.6f, 1f),
                new Color(0.8f, 0.5f, 0.2f, 1f)
            };

            for (int i = 0; i < 6; i++)
            {
                var zone = new GameObject($"Zone{i}");
                zone.transform.SetParent(_gameArea, false);
                var zRt = zone.AddComponent<RectTransform>();
                float w = 1f / 6f;
                zRt.anchorMin = new Vector2(i * w, 0.15f);
                zRt.anchorMax = new Vector2((i + 1) * w, 0.8f);
                zRt.offsetMin = Vector2.zero; zRt.offsetMax = Vector2.zero;
                zone.AddComponent<Image>().color = zoneColors[i];
                var tmp = CreateTextObject(zone.transform, zoneNames[i], 14, Color.white);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().anchorMax = Vector2.one;
                tmp.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                _pinballZones.Add(zRt);
            }

            // 弹珠指示
            var ball = CreateTextObject(_gameArea, "●", 30, Color.red);
            ball.alignment = TextAlignmentOptions.Center;
            var ballRt = ball.GetComponent<RectTransform>();
            ballRt.anchorMin = new Vector2(0.4f, 0.85f);
            ballRt.anchorMax = new Vector2(0.6f, 0.95f);
            ballRt.offsetMin = Vector2.zero; ballRt.offsetMax = Vector2.zero;

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "弹射!";
            _confirmButton.onClick.AddListener(() => { StartCoroutine(PinballRoutine(ballRt)); });
        }

        IEnumerator PinballRoutine(RectTransform ball)
        {
            _confirmButton.interactable = false;
            _resultText.text = "弹珠下落中...";

            float elapsed = 0;
            float duration = 1.5f;
            float startX = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float x = startX + Mathf.Sin(elapsed * 10) * 0.3f * (1 - t);
                ball.anchorMin = new Vector2(x - 0.05f, 0.95f - t * 0.8f);
                ball.anchorMax = new Vector2(x + 0.05f, 0.95f - t * 0.8f + 0.1f);
                yield return null;
            }

            // 最终位置
            int zone = UnityEngine.Random.Range(0, 6);
            float zoneCenter = (zone + 0.5f) / 6f;
            ball.anchorMin = new Vector2(zoneCenter - 0.05f, 0.15f);
            ball.anchorMax = new Vector2(zoneCenter + 0.05f, 0.25f);

            var zoneNames = new[] { "空区", "灵石区", "凡品灵材", "灵品灵材", "药水区", "卡牌区" };
            _resultText.text = $"弹珠落入: {zoneNames[zone]}";

            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(0, zone + 100)); // 100-105 = zone index
        }

        // ========== 套灵兽 ==========
        private List<RectTransform> _ringTargets = new List<RectTransform>();

        void BuildRingToss()
        {
            var animalNames = new[] { "灵兔", "灵狐", "灵鹿", "灵龙" };
            var animalColors = new[] {
                new Color(0.6f, 0.8f, 0.6f, 1f),
                new Color(0.8f, 0.6f, 0.8f, 1f),
                new Color(0.6f, 0.6f, 0.8f, 1f),
                new Color(0.8f, 0.6f, 0.2f, 1f)
            };

            for (int i = 0; i < 4; i++)
            {
                var cage = new GameObject($"Cage{i}");
                cage.transform.SetParent(_gameArea, false);
                var cRt = cage.AddComponent<RectTransform>();
                float w = 1f / 4f;
                cRt.anchorMin = new Vector2(i * w + 0.05f, 0.2f);
                cRt.anchorMax = new Vector2((i + 1) * w - 0.05f, 0.8f);
                cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
                cage.AddComponent<Image>().color = animalColors[i];
                var tmp = CreateTextObject(cage.transform, animalNames[i], 18, Color.white);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().anchorMax = Vector2.one;
                tmp.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                _ringTargets.Add(cRt);
            }

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "投圈!";
            _confirmButton.onClick.AddListener(() => { StartCoroutine(RingTossRoutine()); });
        }

        IEnumerator RingTossRoutine()
        {
            _confirmButton.interactable = false;
            _resultText.text = "投圈中...";

            yield return new WaitForSeconds(1f);

            int hit = UnityEngine.Random.Range(0, 100);
            int targetIdx;
            string result;

            if (hit < 35) { targetIdx = -1; result = "套空!"; }
            else if (hit < 60) { targetIdx = 0; result = "套中灵兔 +灵石"; }
            else if (hit < 80) { targetIdx = 1; result = "套中灵狐 获得凡品灵材"; }
            else if (hit < 92) { targetIdx = 2; result = "套中灵鹿 获得灵品灵材"; }
            else if (hit < 98) { targetIdx = 2; result = "套中灵鹿 获得药水"; }
            else { targetIdx = 3; result = "套中灵龙! 获得遗物!"; }

            if (targetIdx >= 0)
            {
                var highlight = _ringTargets[targetIdx];
                highlight.GetComponent<Image>().color = new Color(1f, 1f, 0.3f, 1f);
            }

            _resultText.text = result;
            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(0, hit));
        }

        // ========== 灵气球 ==========
        private List<RectTransform> _balloons = new List<RectTransform>();

        void BuildBalloon()
        {
            var balloonColors = new[] {
                ("蓝", new Color(0.2f, 0.4f, 0.8f, 1f)),
                ("绿", new Color(0.2f, 0.6f, 0.3f, 1f)),
                ("黄", new Color(0.8f, 0.7f, 0.2f, 1f)),
                ("紫", new Color(0.5f, 0.2f, 0.6f, 1f)),
                ("金", new Color(0.9f, 0.8f, 0.2f, 1f))
            };

            for (int i = 0; i < 5; i++)
            {
                var balloon = new GameObject($"Balloon{i}");
                balloon.transform.SetParent(_gameArea, false);
                var bRt = balloon.AddComponent<RectTransform>();
                float w = 1f / 5f;
                bRt.anchorMin = new Vector2(i * w + 0.03f, 0.2f);
                bRt.anchorMax = new Vector2((i + 1) * w - 0.03f, 0.8f);
                bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;
                balloon.AddComponent<Image>().color = balloonColors[i].Item2;
                var tmp = CreateTextObject(balloon.transform, balloonColors[i].Item1, 16, Color.white);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().anchorMax = Vector2.one;
                tmp.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                _balloons.Add(bRt);
            }

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "射击!";
            _confirmButton.onClick.AddListener(() => { StartCoroutine(BalloonRoutine()); });
        }

        IEnumerator BalloonRoutine()
        {
            _confirmButton.interactable = false;
            _resultText.text = "瞄准中...";

            yield return new WaitForSeconds(1f);

            int shot = UnityEngine.Random.Range(0, 100);
            int balloonIdx;
            string result;

            if (shot < 30) { balloonIdx = -1; result = "射偏!"; }
            else if (shot < 55) { balloonIdx = 0; result = "击中蓝气球 +灵石"; }
            else if (shot < 75) { balloonIdx = 1; result = "击中绿气球 获得凡品灵材"; }
            else if (shot < 88) { balloonIdx = 2; result = "击中黄气球 +双倍灵石"; }
            else if (shot < 96) { balloonIdx = 3; result = "击中紫气球 获得灵品灵材"; }
            else { balloonIdx = 4; result = "击中金气球! +五倍灵石!!"; }

            if (balloonIdx >= 0)
            {
                var hit = _balloons[balloonIdx];
                hit.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                var tmp = hit.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp) tmp.text = "✕";
            }

            _resultText.text = result;
            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(0, shot));
        }

        // ========== 仙缘抽签 ==========
        private TextMeshProUGUI _lotteryStick;

        void BuildLottery()
        {
            _lotteryStick = CreateTextObject(_gameArea, "签筒", 40, new Color(0.6f, 0.3f, 0.1f, 1f));
            var rt = _lotteryStick.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.35f, 0.2f); rt.anchorMax = new Vector2(0.65f, 0.8f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _lotteryStick.alignment = TextAlignmentOptions.Center;

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "抽签!";
            _confirmButton.onClick.AddListener(() => { StartCoroutine(LotteryRoutine()); });
        }

        IEnumerator LotteryRoutine()
        {
            _confirmButton.interactable = false;
            _resultText.text = "摇晃签筒...";

            for (int i = 0; i < 10; i++)
            {
                _lotteryStick.text = "◈";
                yield return new WaitForSeconds(0.08f);
                _lotteryStick.text = "◇";
                yield return new WaitForSeconds(0.08f);
            }

            int stick = UnityEngine.Random.Range(0, 100);
            string result;
            int goldReward = 0;

            if (stick < 15) { _lotteryStick.text = "下签"; _lotteryStick.color = new Color(0.5f, 0.5f, 0.5f); result = "下签 无奖励"; }
            else if (stick < 40) { goldReward = _cost; _lotteryStick.text = "中签"; _lotteryStick.color = new Color(0.6f, 0.4f, 0.2f); result = $"中签 +{goldReward}灵石"; }
            else if (stick < 65) { _lotteryStick.text = "中签"; _lotteryStick.color = new Color(0.6f, 0.4f, 0.2f); result = "中签 获得凡品灵材"; }
            else if (stick < 82) { _lotteryStick.text = "上签"; _lotteryStick.color = new Color(0.2f, 0.7f, 0.3f); result = "上签 获得灵品灵材"; }
            else if (stick < 92) { _lotteryStick.text = "上签"; _lotteryStick.color = new Color(0.2f, 0.7f, 0.3f); result = "上签 获得卡牌"; }
            else if (stick < 98) { _lotteryStick.text = "上上签"; _lotteryStick.color = new Color(0.9f, 0.2f, 0.3f); result = "上上签 获得药水"; }
            else { _lotteryStick.text = "上上签"; _lotteryStick.color = new Color(1f, 0.85f, 0f); result = "上上签! 获得遗物!!"; }

            _resultText.text = result;
            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(goldReward, stick));
        }

        // ========== 命运转盘 ==========
        private RectTransform _wheelPointer;

        void BuildWheel()
        {
            var wheelZones = new[] { "空区", "灵石", "凡品", "双倍", "灵品", "卡牌", "药水", "遗物" };
            var zoneColors = new[] {
                new Color(0.3f, 0.3f, 0.3f, 1f), new Color(0.8f, 0.7f, 0.2f, 1f),
                new Color(0.2f, 0.6f, 0.3f, 1f), new Color(0.8f, 0.6f, 0.2f, 1f),
                new Color(0.2f, 0.4f, 0.8f, 1f), new Color(0.8f, 0.5f, 0.2f, 1f),
                new Color(0.6f, 0.3f, 0.6f, 1f), new Color(0.9f, 0.8f, 0.2f, 1f)
            };

            for (int i = 0; i < 8; i++)
            {
                var zone = new GameObject($"WheelZone{i}");
                zone.transform.SetParent(_gameArea, false);
                var zRt = zone.AddComponent<RectTransform>();
                float w = 1f / 8f;
                zRt.anchorMin = new Vector2(i * w, 0.35f);
                zRt.anchorMax = new Vector2((i + 1) * w, 0.75f);
                zRt.offsetMin = Vector2.zero; zRt.offsetMax = Vector2.zero;
                zone.AddComponent<Image>().color = zoneColors[i];
                var tmp = CreateTextObject(zone.transform, wheelZones[i], 12, Color.white);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().anchorMax = Vector2.one;
                tmp.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            }

            // 指针
            var ptr = CreateTextObject(_gameArea, "▼", 30, Color.red);
            var ptrRt = ptr.GetComponent<RectTransform>();
            ptrRt.anchorMin = new Vector2(0.45f, 0.75f);
            ptrRt.anchorMax = new Vector2(0.55f, 0.85f);
            ptrRt.offsetMin = Vector2.zero; ptrRt.offsetMax = Vector2.zero;
            _wheelPointer = ptrRt;

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "转动!";
            _confirmButton.onClick.AddListener(() => { StartCoroutine(WheelRoutine()); });
        }

        IEnumerator WheelRoutine()
        {
            _confirmButton.interactable = false;
            _resultText.text = "转盘旋转中...";

            float elapsed = 0;
            float duration = 2f;
            float angle = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                angle += Time.deltaTime * 720f * (1 - elapsed / duration);
                float x = 0.5f + Mathf.Sin(angle * Mathf.Deg2Rad) * 0.45f;
                _wheelPointer.anchorMin = new Vector2(x - 0.05f, 0.75f);
                _wheelPointer.anchorMax = new Vector2(x + 0.05f, 0.85f);
                yield return null;
            }

            int zone = UnityEngine.Random.Range(0, 8);
            float zoneCenter = (zone + 0.5f) / 8f;
            _wheelPointer.anchorMin = new Vector2(zoneCenter - 0.05f, 0.75f);
            _wheelPointer.anchorMax = new Vector2(zoneCenter + 0.05f, 0.85f);

            var zoneNames = new[] { "空区", "灵石区", "凡品灵材", "双倍灵石", "灵品灵材", "卡牌区", "药水区", "遗物区" };
            _resultText.text = $"转盘停在: {zoneNames[zone]}";

            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(0, zone + 200)); // 200-207 = wheel zone
        }

        // ========== 灵币翻面 ==========
        private TextMeshProUGUI _coinText;

        void BuildCoinFlip()
        {
            _coinText = CreateTextObject(_gameArea, "◈", 60, new Color(1f, 0.85f, 0f));
            var rt = _coinText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.35f, 0.2f); rt.anchorMax = new Vector2(0.65f, 0.8f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _coinText.alignment = TextAlignmentOptions.Center;

            // 两个选择按钮
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "猜正面";
            _confirmButton.onClick.AddListener(() => { StartCoroutine(CoinFlipRoutine(true)); });

            var cancelTmp = _cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            cancelTmp.text = "猜反面";
            _cancelButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.AddListener(() => { StartCoroutine(CoinFlipRoutine(false)); });
        }

        IEnumerator CoinFlipRoutine(bool guessHeads)
        {
            _confirmButton.interactable = false;
            _cancelButton.interactable = false;
            _resultText.text = "灵币翻飞中...";

            for (int i = 0; i < 12; i++)
            {
                _coinText.text = i % 2 == 0 ? "正" : "反";
                yield return new WaitForSeconds(0.1f);
            }

            bool heads = UnityEngine.Random.value < 0.5f;
            _coinText.text = heads ? "正" : "反";
            _coinText.color = heads ? new Color(1f, 0.85f, 0f) : new Color(0.7f, 0.7f, 0.7f);

            bool win = (heads && guessHeads) || (!heads && !guessHeads);
            int goldReward = 0;
            string result;
            if (win) { goldReward = _cost * 2; result = $"猜对! +{goldReward}灵石"; }
            else { result = $"猜错! -{_cost}灵石"; }

            _resultText.text = result;
            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(goldReward, win ? 1 : 0));
        }

        // ========== 猜牌大小 ==========
        private TextMeshProUGUI _guessCardText;
        private int _guessRound = 0;
        private int _guessReward = 1;

        void BuildCardGuess()
        {
            _guessCardText = CreateTextObject(_gameArea, "?", 60, new Color(0.2f, 0.6f, 0.8f, 1f));
            var rt = _guessCardText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.35f, 0.3f); rt.anchorMax = new Vector2(0.65f, 0.8f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _guessCardText.alignment = TextAlignmentOptions.Center;

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "猜大";
            _confirmButton.onClick.AddListener(() => { StartCoroutine(CardGuessRoutine(true)); });

            var cancelTmp = _cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            cancelTmp.text = "猜小";
            _cancelButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.AddListener(() => { StartCoroutine(CardGuessRoutine(false)); });

            _resultText.text = "连续猜对奖励翻倍！猜错全无！\n当前奖励: ×1";
        }

        IEnumerator CardGuessRoutine(bool guessBig)
        {
            _confirmButton.interactable = false;
            _cancelButton.interactable = false;
            _guessRound++;

            // 翻牌动画
            for (int i = 0; i < 8; i++)
            {
                _guessCardText.text = i % 2 == 0 ? "大" : "小";
                yield return new WaitForSeconds(0.08f);
            }

            bool isBig = UnityEngine.Random.value < 0.5f;
            _guessCardText.text = isBig ? "大" : "小";
            _guessCardText.color = isBig ? new Color(0.8f, 0.3f, 0.2f) : new Color(0.2f, 0.4f, 0.8f);

            bool correct = (isBig && guessBig) || (!isBig && !guessBig);

            if (correct)
            {
                _guessReward *= 2;
                _resultText.text = $"猜对! 当前奖励: ×{_guessReward}\n继续猜或领取";

                if (_guessRound >= 5)
                {
                    // 5轮全对
                    int gold = _cost * _guessReward;
                    _resultText.text = $"5轮全对! +{gold}灵石!!";
                    _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
                    _confirmButton.onClick.RemoveAllListeners();
                    _confirmButton.onClick.AddListener(() => ApplyResult(gold, 5));
                    yield break;
                }

                _confirmButton.interactable = true;
                _cancelButton.interactable = true;
                _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "猜大";
                _cancelButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";

                // 重新绑定：猜大按钮还是猜大，取消按钮变成领取
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(() => { StartCoroutine(CardGuessRoutine(true)); });
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(() => ApplyResult(_cost * _guessReward, _guessRound));
            }
            else
            {
                _resultText.text = $"猜错! 全部归零!\n第{_guessRound}轮倒下";
                _confirmButton.interactable = true;
                _cancelButton.interactable = false;
                _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "离开";
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(() => ApplyResult(0, 0));
            }
        }

        // ========== 寻宝迷踪 ==========
        private List<RectTransform> _chestButtons = new List<RectTransform>();

        void BuildTreasureHunt()
        {
            for (int i = 0; i < 3; i++)
            {
                var chest = new GameObject($"Chest{i}");
                chest.transform.SetParent(_gameArea, false);
                var cRt = chest.AddComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0.15f + i * 0.28f, 0.3f);
                cRt.anchorMax = new Vector2(0.15f + i * 0.28f + 0.2f, 0.8f);
                cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
                chest.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.1f, 1f);
                var tmp = CreateTextObject(chest.transform, "?", 40, new Color(0.9f, 0.8f, 0.3f));
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().anchorMax = Vector2.one;
                tmp.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                tmp.GetComponent<RectTransform>().offsetMax = Vector2.zero;

                var btn = chest.AddComponent<Button>();
                var idx = i;
                btn.onClick.AddListener(() => { StartCoroutine(TreasureHuntRoutine(idx)); });
                _chestButtons.Add(cRt);
            }

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "不选，离开";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(0, -1));
        }

        IEnumerator TreasureHuntRoutine(int chestIdx)
        {
            // 禁用所有宝箱按钮
            foreach (var rt in _chestButtons)
                rt.GetComponent<Button>().interactable = false;

            // 揭晓动画
            var selected = _chestButtons[chestIdx];
            var tmp = selected.GetComponentInChildren<TextMeshProUGUI>();

            int result = UnityEngine.Random.Range(0, 3);
            string resultText;
            int goldReward = 0;

            if (result == 0)
            {
                // 陷阱
                tmp.text = "陷阱!";
                selected.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 1f);
                resultText = $"陷阱箱! 受伤!";
            }
            else if (result == 1)
            {
                // 普通
                goldReward = _cost * 2;
                tmp.text = "灵石!";
                selected.GetComponent<Image>().color = new Color(0.8f, 0.7f, 0.2f, 1f);
                resultText = $"普通宝箱 +{goldReward}灵石";
            }
            else
            {
                // 稀有
                tmp.text = "稀有!";
                selected.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.8f, 1f);
                resultText = "稀有宝箱!";
            }

            // 显示其他宝箱内容
            for (int i = 0; i < 3; i++)
            {
                if (i == chestIdx) continue;
                var other = _chestButtons[i];
                var otherTmp = other.GetComponentInChildren<TextMeshProUGUI>();
                var otherResult = UnityEngine.Random.Range(0, 3);
                if (otherResult == 0) { otherTmp.text = "陷阱"; other.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 0.5f); }
                else if (otherResult == 1) { otherTmp.text = "灵石"; other.GetComponent<Image>().color = new Color(0.8f, 0.7f, 0.2f, 0.5f); }
                else { otherTmp.text = "稀有"; other.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.8f, 0.5f); }
            }

            _resultText.text = resultText;

            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "领取";
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => ApplyResult(goldReward, result));
            yield break;
        }

        // ========== 通用工具方法 ==========
        void ApplyResult(int goldOverride, int rollCode)
        {
            // 先扣除灵石
            var arch = CardGameArchitecture.Interface;
            var battleModel = arch.GetModel<IBattleModel>();
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) { Destroy(gameObject); return; }

            if (battleModel.CurrentGold.Value >= _cost)
            {
                battleModel.CurrentGold.Value -= _cost;
                gm.PersistentGameplayData.CurrentGold = battleModel.CurrentGold.Value;
            }

            // 根据rollCode应用对应效果
            // rollCode含义各小游戏不同，这里统一调用EventSystem
            arch.GetSystem<IEventSystem>().ExecuteChoice(_currentEvent, _choiceIndex);

            // 如果有灵石覆盖（小游戏自己算的灵石数），直接加
            if (goldOverride > 0)
            {
                battleModel.CurrentGold.Value += goldOverride;
                gm.PersistentGameplayData.CurrentGold = battleModel.CurrentGold.Value;
            }

            Destroy(gameObject);
        }

        string GetGameTitle(EventEffectType type)
        {
            return type switch
            {
                EventEffectType.MiniSlot => "灵石机",
                EventEffectType.MiniDice => "掷骰问运",
                EventEffectType.MiniPinball => "灵珠弹射",
                EventEffectType.MiniRingToss => "套灵兽",
                EventEffectType.MiniBalloon => "灵气球",
                EventEffectType.MiniLottery => "仙缘抽签",
                EventEffectType.MiniWheel => "命运转盘",
                EventEffectType.MiniCoinFlip => "灵币翻面",
                EventEffectType.MiniCardGuess => "猜牌大小",
                EventEffectType.MiniTreasureHunt => "寻宝迷踪",
                _ => "小游戏"
            };
        }

        // ========== UI创建工具 ==========
        Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        void SetFullStretch(Image img)
        {
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        TextMeshProUGUI CreateTextObject(Transform parent, string text, float size, Color color)
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

        GameObject CreateText(string name, Transform parent, string text, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go;
        }
    }
}
