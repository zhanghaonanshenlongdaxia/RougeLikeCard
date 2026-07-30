using System.Collections.Generic;
using CardGame;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.UI;
using QFramework;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Managers
{
    public class MapManager : MonoBehaviour, IController
    {
        [SerializeField] private List<EncounterButton> encounterButtonList;

        public List<EncounterButton> EncounterButtonList => encounterButtonList;
        
        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;
        
        private GameManager GameManager => GameManager.Instance;
        
        private void Start()
        {
            PrepareEncounters();
        }
        
        private void PrepareEncounters()
        {
            var currentEncounterId = this.GetModel<IBattleModel>().CurrentEncounterId;
            for (int i = 0; i < EncounterButtonList.Count; i++)
            {
                var btn = EncounterButtonList[i];
                if (currentEncounterId == i)
                    btn.SetStatus(EncounterButtonStatus.Active);
                else if (currentEncounterId > i)
                    btn.SetStatus(EncounterButtonStatus.Completed);
                else
                    btn.SetStatus(EncounterButtonStatus.Passive);
            }
        }
    }
}
