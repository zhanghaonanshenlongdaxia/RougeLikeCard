using System;
using System.Collections.Generic;
using CardGame;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.NueExtentions;
using NueGames.NueDeck.Scripts.Utils;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using NueUIManager = NueGames.NueDeck.Scripts.Managers.UIManager;

namespace NueGames.NueDeck.Scripts.UI.Reward
{
    public class RewardCanvas : CanvasBase, IController
    {
        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        [Header("Continue Button")]
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject continueButtonObject;
        [Header("References")]
        [SerializeField] private RewardContainerData rewardContainerData;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private RewardContainer rewardContainerPrefab;
        [SerializeField] private Transform rewardPanelRoot;
        [Header("Choice")]
        [SerializeField] private Transform choice2DCardSpawnRoot;
        [SerializeField] private ChoiceCard choiceCardUIPrefab;
        [SerializeField] private ChoicePanel choicePanel;
        
        private readonly List<RewardContainer> _currentRewardsList = new List<RewardContainer>();
        private readonly List<ChoiceCard> _spawnedChoiceList = new List<ChoiceCard>();
        private readonly List<CardData> _cardRewardList = new List<CardData>();

        public ChoicePanel ChoicePanel => choicePanel;
        
        private void Awake()
        {
            AutoBindReferences();
        }
        
        private void AutoBindReferences()
        {
            // 通过路径查找choice2DCardSpawnRoot
            if (choice2DCardSpawnRoot == null)
                choice2DCardSpawnRoot = transform.Find("ChoicePanel/Midground/RewardRoot/Viewport/ChoiceCardUIRoot");
            if (choice2DCardSpawnRoot == null)
                choice2DCardSpawnRoot = transform.Find("ChoicePanel/Midground/Foreground/ChoiceCardUIRoot");
            if (choice2DCardSpawnRoot == null)
                choice2DCardSpawnRoot = transform.Find("ChoicePanel/ChoiceCardUIRoot");
            
            // rewardRoot
            if (rewardRoot == null)
                rewardRoot = transform.Find("CombatRewardPanel/Midground/RewardRoot/Viewport/Content");
            
            // choicePanel
            if (choicePanel == null)
            {
                var cp = transform.Find("ChoicePanel");
                if (cp != null) choicePanel = cp.GetComponent<ChoicePanel>();
            }
        }
        
        #region Public Methods

        public void PrepareCanvas()
        {
            // 清理上一场战斗的残留奖励
            ResetRewards();
            ResetChoice();
            
            rewardPanelRoot.gameObject.SetActive(true);
            
            // 显示继续按钮
            if (continueButtonObject) continueButtonObject.SetActive(true);
            if (continueButton)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinue);
            }
        }
        public void BuildReward(RewardType rewardType)
        {
            var rewardClone = Instantiate(rewardContainerPrefab, rewardRoot);
            _currentRewardsList.Add(rewardClone);
            
            switch (rewardType)
            {
                case RewardType.Gold:
                    var rewardGold = rewardContainerData.GetRandomGoldReward(out var goldRewardData);
                    rewardClone.BuildReward(GetRewardIcon(RewardType.Gold), goldRewardData.RewardDescription);
                    rewardClone.RewardButton.onClick.AddListener(()=>GetGoldReward(rewardClone,rewardGold));
                    break;
                case RewardType.Card:
                    // 从 ResourceCache 的 158 张卡牌库中随机抽 3 张
                    var allCards = CardGame.ResourceCache.GetCardsFromAllList();
                    _cardRewardList.Clear();
                    if (allCards.Count > 0)
                    {
                        // 随机抽3张不重复
                        var pool = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>(allCards);
                        int pickCount = Mathf.Min(3, pool.Count);
                        for (int i = 0; i < pickCount; i++)
                        {
                            var card = pool[UnityEngine.Random.Range(0, pool.Count)];
                            _cardRewardList.Add(card);
                            pool.Remove(card);
                        }
                    }
                    var cardRewardData = rewardContainerData.CardRewardDataList[0];
                    rewardClone.BuildReward(GetRewardIcon(RewardType.Card), cardRewardData.RewardDescription);
                    rewardClone.RewardButton.onClick.AddListener(()=>GetCardReward(rewardClone, _cardRewardList.Count));
                    break;
                case RewardType.Relic:
                    break;
                case RewardType.Material:
                    // 材料奖励：从 CombatManager.LastDroppedMaterials 获取
                    var droppedMats = NueGames.NueDeck.Scripts.Managers.CombatManager.Instance.LastDroppedMaterials;
                    if (droppedMats != null && droppedMats.Count > 0)
                    {
                        // 按材料ID分组显示
                        var grouped = new Dictionary<string, (CardGame.MaterialData mat, int count)>();
                        foreach (var mat in droppedMats)
                        {
                            if (mat == null) continue;
                            if (!grouped.ContainsKey(mat.name))
                                grouped[mat.name] = (mat, 1);
                            else
                            {
                                var prev = grouped[mat.name];
                                grouped[mat.name] = (prev.mat, prev.count + 1);
                            }
                        }
                        foreach (var kvp in grouped)
                        {
                            var matClone = Instantiate(rewardContainerPrefab, rewardRoot);
                            _currentRewardsList.Add(matClone);
                            var displayName = $"{kvp.Value.mat.ItemName} ×{kvp.Value.count}";
                            matClone.BuildReward(kvp.Value.mat.icon != null ? kvp.Value.mat.icon : GetRewardIcon(RewardType.Material), displayName);
                            // 材料已经在DropLoot中添加到背包，这里只是展示
                            var capturedClone = matClone;
                            matClone.RewardButton.onClick.AddListener(() =>
                            {
                                _currentRewardsList.Remove(capturedClone);
                                Destroy(capturedClone.gameObject);
                            });
                        }
                        // 销毁最初创建的空 rewardClone
                        _currentRewardsList.Remove(rewardClone);
                        Destroy(rewardClone.gameObject);
                    }
                    else
                    {
                        _currentRewardsList.Remove(rewardClone);
                        Destroy(rewardClone.gameObject);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, null);
            }
        }
        
        public override void ResetCanvas()
        {
            ResetRewards();

            ResetChoice();
        }

        private void ResetRewards()
        {
            foreach (var rewardContainer in _currentRewardsList)
                Destroy(rewardContainer.gameObject);

            _currentRewardsList?.Clear();
        }

        private void ResetChoice()
        {
            foreach (var choice in _spawnedChoiceList)
            {
                Destroy(choice.gameObject);
            }

            _spawnedChoiceList?.Clear();
            ChoicePanel.DisablePanel();
        }

        #endregion
        
        #region Private Methods
        private void GetGoldReward(RewardContainer rewardContainer,int amount)
        {
            var battleModel = this.GetModel<IBattleModel>();
            battleModel.CurrentGold.Value += amount;
            GameManager.PersistentGameplayData.CurrentGold = battleModel.CurrentGold.Value;
            _currentRewardsList.Remove(rewardContainer);
            UIManager.InformationCanvas.SetGoldText(battleModel.CurrentGold.Value);
            Destroy(rewardContainer.gameObject);
        }

        private void GetCardReward(RewardContainer rewardContainer,int amount = 3)
        {
            ChoicePanel.gameObject.SetActive(true);
            
            for (int i = 0; i < amount; i++)
            {
                Transform spawnTransform = choice2DCardSpawnRoot;
              
                var choice = Instantiate(choiceCardUIPrefab, spawnTransform);
                
                var reward = _cardRewardList.RandomItem();
                choice.BuildReward(reward);
                choice.OnCardChose += ResetChoice;
                
                _cardRewardList.Remove(reward);
                _spawnedChoiceList.Add(choice);
                _currentRewardsList.Remove(rewardContainer);
                
            }
            
            Destroy(rewardContainer.gameObject);
        }
        #endregion

        private void OnContinue()
        {
            // 隐藏继续按钮
            if (continueButtonObject) continueButtonObject.SetActive(false);

            // 检查是否是Boss节点 → 触发撤离选择
            var arch = CardGameArchitecture.Interface;
            var battleModel = arch.GetModel<IBattleModel>();
            if (battleModel.IsFinalEncounter)
            {
                // Boss胜利 → 弹出撤离选择面板
                ShowEvacuateChoice();
                return;
            }

            // 返回地图场景
            var sceneChanger = FindObjectOfType<SceneChanger>();
            if (sceneChanger) sceneChanger.OpenMapScene();
            else Debug.LogError("[RewardCanvas] SceneChanger not found!");
        }

        /// <summary>
        /// 弹出撤离选择面板：撤离(回基地) 或 继续冒险
        /// </summary>
        private void ShowEvacuateChoice()
        {
            var arch = CardGameArchitecture.Interface;
            var panelObj = new GameObject("EvacuateChoicePanel");
            var panel = panelObj.AddComponent<CardGame.UI.EvacuateChoicePanel>();
            panel.Init(
                onEvacuate: () =>
                {
                    // 撤离：材料转移到乾坤袋，回基地
                    arch.GetSystem<IEvacuateSystem>().Evacuate();
                    arch.GetModel<IBattleModel>().IsFinalEncounter = false;
                    GameManager.PersistentGameplayData.IsFinalEncounter = false;

                    // 存档
                    SaveSystem.Save();

                    // 返回基地
                    var sceneChanger = FindObjectOfType<SceneChanger>();
                    if (sceneChanger) sceneChanger.OpenBaseScene();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene(2);
                },
                onContinue: () =>
                {
                    // 继续冒险
                    arch.GetSystem<IEvacuateSystem>().ContinueAdventure();
                    arch.GetModel<IBattleModel>().IsFinalEncounter = false;
                    GameManager.PersistentGameplayData.IsFinalEncounter = false;

                    SaveSystem.Save();

                    var sceneChanger = FindObjectOfType<SceneChanger>();
                    if (sceneChanger) sceneChanger.OpenMapScene();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene(3);
                }
            );
        }

        /// <summary>按奖励类型获取对应图标</summary>
        private Sprite GetRewardIcon(RewardType type)
        {
            string iconPath = type switch
            {
                RewardType.Gold => "Assets/NueGames/NueDeck/Sprites/CardIcons/IncreaseMaxHealth.png",
                RewardType.Card => "Assets/NueGames/NueDeck/Sprites/CardIcons/Draw.png",
                RewardType.Material => "Assets/NueGames/NueDeck/Sprites/CardIcons/Heal.png",
                _ => null
            };
            if (string.IsNullOrEmpty(iconPath)) return null;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
#else
            return null;
#endif
        }

    }
}