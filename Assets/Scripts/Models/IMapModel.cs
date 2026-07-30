using QFramework;

namespace CardGame
{
    public interface IMapModel : IModel
    {
        GameMap CurrentMap { get; set; }
    }
}
