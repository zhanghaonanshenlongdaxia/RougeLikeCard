using QFramework;

namespace CardGame
{
    public interface IEvacuateSystem : ISystem
    {
        void OnCheckpointCleared();
        void Evacuate();
        void ContinueAdventure();
        void OnDeath();
    }
}
