using QFramework;

namespace CardGame
{
    public interface ILoadoutSystem : ISystem
    {
        bool SelectCard(string cardId, int shenShiCost);
        bool DeselectCard(string cardId, int shenShiCost);
        bool ValidateLoadout();
        void StartAdventure();
    }
}
