using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    public interface IEventSystem : ISystem
    {
        EventData GetRandomEvent();
        void ExecuteChoice(EventData eventData, int choiceIndex);
    }
}
